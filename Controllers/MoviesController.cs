using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP26_SMAssignment3.Data;
using SP26_SMAssignment3.Models;

namespace SP26_SMAssignment3.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private const int MaxInputLength = 512;

        public MoviesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            var (posts, overallScore) = await GetMovieSentimentAsync(movie.Title ?? "");

            var viewModel = new MovieDetailsViewModel
            {
                Movie = movie,
                RedditPosts = posts,
                OverallScore = overallScore
            };

            return View(viewModel);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie)
        {
            if (movie.PosterImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(movie.PosterImageFile.FileName);
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/movies", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.PosterImageFile.CopyToAsync(stream);
                }
                movie.PosterImage = "/images/movies/" + fileName;
            }

            _context.Add(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null) return NotFound();
            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie)
        {
            if (id != movie.Id) return NotFound();

            if (movie.PosterImageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(movie.PosterImageFile.FileName);
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/movies", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.PosterImageFile.CopyToAsync(stream);
                }
                movie.PosterImage = "/images/movies/" + fileName;
            }

            try
            {
                _context.Update(movie);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovieExists(movie.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }

        // ── Sentiment helpers ──────────────────────────────────────────────

        private async Task<(List<RedditSentimentItem> posts, double overallScore)> GetMovieSentimentAsync(string title)
        {
            var apiKey = _configuration["HuggingFaceApiKey"] ?? "";
            var url = "https://router.huggingface.co/hf-inference/models/distilbert/distilbert-base-uncased-finetuned-sst-2-english";
            var posts = new List<RedditSentimentItem>();
            double totalScore = 0;
            int validResponses = 0;

            List<string> textToExamine = await SearchRedditAsync(title);

            using var httpClient = new HttpClient();
            foreach (var post in textToExamine)
            {
                var data = new { inputs = new[] { post } };
                var json = JsonSerializer.Serialize(data);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Headers = { { "Authorization", $"Bearer {apiKey}" } },
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                try
                {
                    var response = await httpClient.SendAsync(request);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var sentimentResults = JsonSerializer.Deserialize<List<List<SentimentResponse>>>(responseString);

                    if (sentimentResults != null && sentimentResults.Count > 0)
                    {
                        var result = sentimentResults[0][0];
                        double confidence = result.Score;
                        if (result.Label == "NEGATIVE") confidence *= -1;

                        posts.Add(new RedditSentimentItem
                        {
                            Comment = post,
                            Score = Math.Round(result.Score * 100, 2),
                            Label = result.Label
                        });

                        totalScore += confidence;
                        validResponses++;
                    }
                }
                catch { }
            }

            double overallScore = validResponses > 0 ? Math.Round(totalScore / validResponses, 2) : 0;
            return (posts, overallScore);
        }

        private static async Task<List<string>> SearchRedditAsync(string searchQuery)
        {
            var returnList = new List<string>();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            try
            {
                string json = await client.GetStringAsync(
                    "https://api.pullpush.io/reddit/search/comment/?size=25&q=" + HttpUtility.UrlEncode(searchQuery));
                JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("data", out JsonElement dataArray))
                {
                    foreach (JsonElement comment in dataArray.EnumerateArray())
                    {
                        if (comment.TryGetProperty("body", out JsonElement bodyElement))
                        {
                            string? textToAdd = bodyElement.GetString();
                            if (!string.IsNullOrEmpty(textToAdd))
                            {
                                returnList.Add(TruncateToMaxLength(textToAdd, MaxInputLength));
                            }
                        }
                    }
                }
            }
            catch { }

            return returnList;
        }

        private static string TruncateToMaxLength(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength);
        }
    }

    public class SentimentResponse
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}
