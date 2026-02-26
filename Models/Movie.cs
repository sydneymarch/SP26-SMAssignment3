using System.ComponentModel.DataAnnotations.Schema;

namespace SP26_SMAssignment3.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? IMDBLink { get; set; }
        public string? Genre { get; set; }
        public int? YearOfRelease { get; set; }
        public string? PosterImage { get; set; }
        [NotMapped]
        public IFormFile? PosterImageFile { get; set; }
    }
}
