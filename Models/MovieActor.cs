namespace SP26_SMAssignment3.Models
{
    public class MovieActor
    {
        public int Id { get; set; }
        public int? MovieId { get; set; }
        public int? ActorId { get; set; }
        public Movie? Movie { get; set; }
        public Actor? Actor { get; set; }
    }
}
