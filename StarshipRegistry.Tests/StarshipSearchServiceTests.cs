using StarshipRegistry.Services;
using System.Reflection;

namespace StarshipRegistry.Tests;

public class StarshipSearchServiceTests
{
    private static float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var method = typeof(StarshipSearchService)
            .GetMethod("CalculateCosineSimilarity", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (float)method.Invoke(null, new object[] { a, b })!;
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var vec = new float[] { 1f, 2f, 3f };
        var result = CosineSimilarity(vec, vec);
        Assert.Equal(1f, result, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        var a = new float[] { 1f, 0f, 0f };
        var b = new float[] { -1f, 0f, 0f };
        var result = CosineSimilarity(a, b);
        Assert.Equal(-1f, result, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var a = new float[] { 1f, 0f };
        var b = new float[] { 0f, 1f };
        var result = CosineSimilarity(a, b);
        Assert.Equal(0f, result, precision: 5);
    }

    [Fact]
    public void CosineSimilarity_DifferentLengthVectors_ThrowsArgumentException()
    {
        var a = new float[] { 1f, 2f };
        var b = new float[] { 1f, 2f, 3f };
        Assert.Throws<TargetInvocationException>(() => CosineSimilarity(a, b));
    }

    [Fact]
    public void CosineSimilarity_ZeroVector_ReturnsZero()
    {
        var a = new float[] { 0f, 0f, 0f };
        var b = new float[] { 1f, 2f, 3f };
        var result = CosineSimilarity(a, b);
        Assert.Equal(0f, result);
    }
}
