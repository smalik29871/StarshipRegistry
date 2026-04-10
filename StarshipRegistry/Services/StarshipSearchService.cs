using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using StarshipRegistry.Data;
using StarshipRegistry.Models;

namespace StarshipRegistry.Services
{
    public class StarshipSearchService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private volatile List<(Starship Ship, ReadOnlyMemory<float> Vector)>? _cachedEmbeddings;
        private readonly SemaphoreSlim _buildLock = new SemaphoreSlim(1, 1);

        public StarshipSearchService(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            var ollamaUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434/";
            var embeddingModel = config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
            _embeddingGenerator = new OllamaApiClient(new Uri(ollamaUrl), embeddingModel);
        }

        public virtual async Task BuildIndexAsync()
        {
            await _buildLock.WaitAsync();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var starships = await context.Starships.ToListAsync();

                var texts = starships.Select(s => BuildSearchText(s)).ToList();
                GeneratedEmbeddings<Embedding<float>> generated;
                try
                {
                    generated = await _embeddingGenerator.GenerateAsync(texts);
                }
                catch (Exception)
                {
                    return;
                }

                var newEmbeddings = new List<(Starship, ReadOnlyMemory<float>)>(starships.Count);
                for (int i = 0; i < starships.Count; i++)
                    newEmbeddings.Add((starships[i], generated[i].Vector));

                _cachedEmbeddings = newEmbeddings;
            }
            finally
            {
                _buildLock.Release();
            }
        }

        public virtual async Task AddToIndexAsync(Starship ship)
        {
            if (_cachedEmbeddings == null) return;

            await _buildLock.WaitAsync();
            try
            {
                var generated = await _embeddingGenerator.GenerateAsync(new[] { BuildSearchText(ship) });
                var current = _cachedEmbeddings;
                _cachedEmbeddings = current
                    .Where(e => e.Ship.Url != ship.Url)
                    .Append((ship, generated[0].Vector))
                    .ToList();
            }
            catch (Exception)
            {
                // Ollama unavailable — index unchanged
            }
            finally
            {
                _buildLock.Release();
            }
        }

        public virtual void RemoveFromIndex(string url)
        {
            var current = _cachedEmbeddings;
            if (current == null) return;
            _cachedEmbeddings = current.Where(e => e.Ship.Url != url).ToList();
        }

        private static string BuildSearchText(Starship ship) =>
            $"search_document: {ship.Name} {ship.Model} {ship.Manufacturer} " +
            $"class {ship.StarshipClass} hyperdrive {ship.HyperdriveRating} " +
            $"crew {ship.Crew} passengers {ship.Passengers} " +
            $"length {ship.Length} speed {ship.MaxAtmospheringSpeed} " +
            $"cargo {ship.CargoCapacity} MGLT {ship.Mglt} consumables {ship.Consumables}";

        public virtual async Task<List<Starship>> SearchAsync(string query, int take = 10)
        {
            var snapshot = _cachedEmbeddings;
            if (snapshot == null || !snapshot.Any())
                return new List<Starship>();

            string searchPrompt = $"search_query: {query}";
            var queryEmbeddings = await _embeddingGenerator.GenerateAsync(new[] { searchPrompt });
            var queryVector = queryEmbeddings[0].Vector;

            return snapshot
                .Select(item => new
                {
                    Starship = item.Ship,
                    Similarity = CalculateCosineSimilarity(queryVector, item.Vector)
                })
                .Where(r => r.Similarity >= 0.35f)
                .OrderByDescending(r => r.Similarity)
                .Take(take)
                .Select(r => r.Starship)
                .ToList();
        }

        private static float CalculateCosineSimilarity(ReadOnlyMemory<float> vecA, ReadOnlyMemory<float> vecB)
        {
            if (vecA.Length != vecB.Length)
                throw new ArgumentException($"Vector dimensions do not match: {vecA.Length} vs {vecB.Length}.");

            var arrayA = vecA.Span;
            var arrayB = vecB.Span;

            float dotProduct = 0;
            float magnitudeA = 0;
            float magnitudeB = 0;

            for (int i = 0; i < arrayA.Length; i++)
            {
                dotProduct += arrayA[i] * arrayB[i];
                magnitudeA += arrayA[i] * arrayA[i];
                magnitudeB += arrayB[i] * arrayB[i];
            }

            float denominator = (float)(Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
            return denominator == 0 ? 0 : dotProduct / denominator;
        }
    }
}
