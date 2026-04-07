using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;
using StarshipRegistry.Configuration;
using StarshipRegistry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarshipRegistry.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly string _swapiBaseUrl;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IOptions<SwapiSettings> swapiSettings)
            : base(options)
        {
            _swapiBaseUrl = swapiSettings.Value.BaseUrl.TrimEnd('/');
        }

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

        // SWAPI Seeder
        public async Task SeedDataAsync()
        {
            if (await Films.AnyAsync()) return;

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{_swapiBaseUrl}/");

            try
            {
                var response = await httpClient.GetAsync("films/");
                if (!response.IsSuccessStatusCode) return;

                var jsonString = await response.Content.ReadAsStringAsync();

                var swapiData = JsonSerializer.Deserialize<SwapiFilmResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (swapiData?.Results == null) return;

                foreach (var swapiFilm in swapiData.Results)
                {
                    var film = new Film
                    {
                        Url = swapiFilm.Url,
                        Title = swapiFilm.Title,
                        EpisodeId = swapiFilm.Episode_Id,
                        OpeningCrawl = swapiFilm.Opening_Crawl,
                        Director = swapiFilm.Director,
                        Producer = swapiFilm.Producer,
                        ReleaseDate = swapiFilm.Release_Date,
                        Characters = swapiFilm.Characters,
                        Planets = swapiFilm.Planets,
                        Starships = swapiFilm.Starships,
                        Vehicles = swapiFilm.Vehicles,
                        Species = swapiFilm.Species
                    };

                    await Films.AddAsync(film);
                }

                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seeding failed: {ex.Message}");
            }
        }
    }

    // Swapi DTOs with default instantiations to prevent nullable reference warnings
    public class SwapiFilmResponse
    {
        public List<SwapiFilmItem> Results { get; set; } = new();
    }

    public class SwapiFilmItem
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Episode_Id { get; set; }
        public string Opening_Crawl { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;
        public string Producer { get; set; } = string.Empty;
        public string Release_Date { get; set; } = string.Empty;
        public List<string> Characters { get; set; } = new();
        public List<string> Planets { get; set; } = new();
        public List<string> Starships { get; set; } = new();
        public List<string> Vehicles { get; set; } = new();
        public List<string> Species { get; set; } = new();
    }
}
