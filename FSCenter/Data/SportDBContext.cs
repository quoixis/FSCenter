using FSCenter.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace FSCenter.Data
{
    public class SportDBContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Trainer> Trainers { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Club> Clubs { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Membership> Memberships { get; set; } = null!;
        public DbSet<Visit> Visits { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        public SportDBContext(DbContextOptions<SportDBContext> options) : base(options) { }
        public SportDBContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string projectDir = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
                string folder = Path.Combine(projectDir, "Data");
                Directory.CreateDirectory(folder);

                string dbPath = Path.Combine(folder, "main.db");

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Room>().HasIndex(r => r.RoomNumber).IsUnique();
        }
    }
}
