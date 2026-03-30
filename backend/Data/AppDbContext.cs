using Microsoft.EntityFrameworkCore;
using AudioBackend.Models;

namespace AudioBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AudioRecord> AudioRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Tablo adını netleştirelim
            modelBuilder.Entity<AudioRecord>().ToTable("AudioRecords");
        }
    }
}