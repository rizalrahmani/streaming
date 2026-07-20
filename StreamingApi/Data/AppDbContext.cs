using Microsoft.EntityFrameworkCore;
using StreamingApi.Models;

namespace StreamingApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
      
    }

    public DbSet<Movie> Movies => Set<Movie>();
}