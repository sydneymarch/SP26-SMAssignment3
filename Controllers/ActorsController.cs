using System.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP26_SMAssignment3.Data;
using SP26_SMAssignment3.Models;
using VaderSharp2;

namespace SP26_SMAssignment3.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int MaxInputLength = 512;

        public ActorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actor.ToListAsync());
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var actor = await _context.Actor.FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null) return NotFound();

            var (posts, overallScore) = await GetActorSentimentAsync(actor.Name ?? "");

            var viewModel = new ActorDetailsViewModel
            {
                Actor = actor,
                RedditPosts = posts,
                OverallScore = overallScore
            };

            return View(viewModel);
        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Actor actor)
        {
            if (actor.PhotoFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(actor.PhotoFile.FileName);
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/actors", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await actor.PhotoFile.CopyToAsync(stream);
                }
                actor.Photo = "/images/actors/" + fileName;
            }
            _context.Add(actor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Actors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null) return NotFound();
            return View(actor);
        }

        // POST: Actors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Actor actor)
        {
            if (id != actor.Id) return NotFound();

            if (actor.PhotoFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(actor.PhotoFile.FileName);
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/actors", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await actor.PhotoFile.CopyToAsync(stream);
                }
                actor.Photo = "/images/actors/" + fileName;
            }

            try
            {
                _context.Update(actor);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActorExists(actor.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var actor = await _context.Actor.FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null) return NotFound();

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return _context.Actor.Any(e => e.Id == id);
        }

        // ── Sentiment helpers ──────────────────────────────────────────────

        private static async Task<(List<RedditSentimentItem> posts, double overallScore)> GetActorSentimentAsync(string actorName)
        {
            var posts = new List<RedditSentimentItem>();
            double totalScore = 0;
            int validResponses = 0;

            List<string> textToExamine = await SearchRedditAsync(actorName);
            var analyzer = new SentimentIntensityAnalyzer();

            foreach (var post in textToExamine)
            {
                var results = analyzer.PolarityScores(post);
                double compound = results.Compound;
                string label = compound >= 0 ? "POSITIVE" : "NEGATIVE";

                posts.Add(new RedditSentimentItem
                {
                    Comment = post,
                    Score = Math.Round(Math.Abs(compound) * 100, 2),
                    Label = label
                });

                totalScore += compound;
                validResponses++;
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
}
