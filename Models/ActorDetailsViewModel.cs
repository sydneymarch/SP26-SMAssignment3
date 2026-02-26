namespace SP26_SMAssignment3.Models
{
    public class ActorDetailsViewModel
    {
        public Actor Actor { get; set; } = new();
        public List<RedditSentimentItem> RedditPosts { get; set; } = new();
        public double OverallScore { get; set; }
        public string OverallSentimentLabel => OverallScore >= 0 ? "POSITIVE" : "NEGATIVE";
    }
}
