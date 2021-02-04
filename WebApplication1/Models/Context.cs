using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class Context : DbContext
    {
      public Context(DbContextOptions<Context> options)
            : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=exchange;User Id=postgres;Password=password;");
        }

        public DbSet<User> Users { get; set; }
        public DbSet<StandingOrder> Orders { get; set; }
    }
}
