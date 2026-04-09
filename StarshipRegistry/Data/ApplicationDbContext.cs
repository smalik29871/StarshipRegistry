using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StarshipRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarshipRegistry.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Starship> Starships { get; set; }
        public DbSet<Film> Films { get; set; }
        public DbSet<Planet> Planets { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Species> Species { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Starship>().HasKey(s => s.Url);
            modelBuilder.Entity<Character>().HasKey(c => c.Url);
            modelBuilder.Entity<Film>().HasKey(f => f.Url);
            modelBuilder.Entity<Planet>().HasKey(p => p.Url);
            modelBuilder.Entity<Vehicle>().HasKey(v => v.Url);
            modelBuilder.Entity<Species>().HasKey(s => s.Url);

            modelBuilder.Entity<Starship>().HasIndex(s => s.Name);
            modelBuilder.Entity<Starship>().HasIndex(s => s.Created);

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            modelBuilder.Entity<Film>(entity =>
            {
                entity.Property(f => f.Characters)
                    .HasConversion(
                        v => SerializeList(v),
                        v => DeserializeList(v))
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(f => f.Planets)
                    .HasConversion(
                        v => SerializeList(v),
                        v => DeserializeList(v))
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(f => f.Starships)
                    .HasConversion(
                        v => SerializeList(v),
                        v => DeserializeList(v))
                    .Metadata.SetValueComparer(stringListComparer);
            });
        }

        private static string SerializeList(List<string> list)
        {
            return JsonSerializer.Serialize(list ?? new List<string>());
        }

        private static List<string> DeserializeList(string json)
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        // Automatic UTC Timestamps
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is ITimestampedEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (ITimestampedEntity)entityEntry.Entity;

                entity.Edited = DateTime.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.Created = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public Task<int> SaveRawAsync(CancellationToken cancellationToken = default)
            => base.SaveChangesAsync(cancellationToken);
    }
}
