using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
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
        private List<(Starship Ship, ReadOnlyMemory<float> Vector)>? _cachedEmbeddings;

        public StarshipSearchService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            // Fix: Keep using the specialized embedding model for vector generation
            _embeddingGenerator = new OllamaApiClient(new Uri("http://localhost:11434/"), "nomic-embed-text");
        }

        public async Task BuildIndexAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var starships = await context.Starships.ToListAsync();

                _cachedEmbeddings = new List<(Starship, ReadOnlyMemory<float>)>();

                foreach (var ship in starships)
                {
                    string searchText = $"search_document: {ship.Name} {ship.Model} {ship.Manufacturer} " +
                        $"class {ship.StarshipClass} hyperdrive {ship.HyperdriveRating} " +
                        $"crew {ship.Crew} passengers {ship.Passengers} " +
                        $"length {ship.Length} speed {ship.MaxAtmospheringSpeed} " +
                        $"cargo {ship.CargoCapacity} MGLT {ship.Mglt} consumables {ship.Consumables}";

                    var generatedEmbeddings = await _embeddingGenerator.GenerateAsync(new[] { searchText });
                    _cachedEmbeddings.Add((ship, generatedEmbeddings[0].Vector));
                }
            }
        }

        public async Task<List<Starship>> SearchAsync(string query, int take = 10)
        {
            if (_cachedEmbeddings == null || !_cachedEmbeddings.Any())
                return new List<Starship>();

            string searchPrompt = $"search_query: {query}";
            var queryEmbeddings = await _embeddingGenerator.GenerateAsync(new[] { searchPrompt });
            var queryVector = queryEmbeddings[0].Vector;

            return _cachedEmbeddings
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
            if (vecA.Length != vecB.Length) return 0;

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