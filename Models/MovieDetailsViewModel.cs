namespace SP26_SMAssignment3.Models
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; } = new();
        public List<RedditSentimentItem> RedditPosts { get; set; } = new();
        public double OverallScore { get; set; }
        public string OverallSentimentLabel => OverallScore >= 0 ? "POSITIVE" : "NEGATIVE";
        public List<Actor> Cast { get; set; } = new();
    }
}
