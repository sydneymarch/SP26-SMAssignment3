using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SP26_SMAssignment3.Models;

namespace SP26_SMAssignment3.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<SP26_SMAssignment3.Models.Actor> Actor { get; set; } = default!;
        public DbSet<SP26_SMAssignment3.Models.Movie> Movie { get; set; } = default!;
        public DbSet<SP26_SMAssignment3.Models.MovieActor> MovieActor { get; set; } = default!;
    }
}
