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
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var starships = await context.Starships.ToListAsync();

                    // Build into a local list; assign atomically so SearchAsync never sees a partial index.
                    var newEmbeddings = new List<(Starship, ReadOnlyMemory<float>)>(starships.Count);

                    foreach (var ship in starships)
                    {
                        string searchText = $"search_document: {ship.Name} {ship.Model} {ship.Manufacturer} " +
                            $"class {ship.StarshipClass} hyperdrive {ship.HyperdriveRating} " +
                            $"crew {ship.Crew} passengers {ship.Passengers} " +
                            $"length {ship.Length} speed {ship.MaxAtmospheringSpeed} " +
                            $"cargo {ship.CargoCapacity} MGLT {ship.Mglt} consumables {ship.Consumables}";

                        var generatedEmbeddings = await _embeddingGenerator.GenerateAsync(new[] { searchText });
                        newEmbeddings.Add((ship, generatedEmbeddings[0].Vector));
                    }

                    _cachedEmbeddings = newEmbeddings;
                }
            }
            finally
            {
                _buildLock.Release();
            }
        }

        public virtual async Task<List<Starship>> SearchAsync(string query, int take = 10)
        {
            // Capture a snapshot so this read is unaffected by a concurrent BuildIndexAsync swap.
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
