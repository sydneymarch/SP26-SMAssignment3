using System.ComponentModel.DataAnnotations.Schema;

namespace SP26_SMAssignment3.Models
{
    public class Actor
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? IMDBLink { get; set; }
        public int? Age { get; set; }
        public string? Photo { get; set; }
        [NotMapped]
        public IFormFile? PhotoFile { get; set; }
    }
}
