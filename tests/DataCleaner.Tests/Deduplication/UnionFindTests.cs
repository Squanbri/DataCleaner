using DataCleaner.Api.Services;

namespace DataCleaner.Tests.Deduplication;

public class UnionFindTests
{
    [Fact]
    public void Union_ChainABC_FormsSingleComponent()
    {
        var uf = new UnionFind(3);
        uf.Union(0, 1); // A-B
        uf.Union(1, 2); // B-C

        Assert.Equal(uf.Find(0), uf.Find(2));

        var components = uf.GetComponents();
        Assert.Single(components);
        Assert.Equal([0, 1, 2], components[0].Order().ToArray());
    }

    [Fact]
    public void Union_DisjointPairs_KeepsTwoComponents()
    {
        var uf = new UnionFind(4);
        uf.Union(0, 1);
        uf.Union(2, 3);

        var components = uf.GetComponents()
            .Select(c => c.Order().ToArray())
            .OrderBy(c => c[0])
            .ToList();

        Assert.Equal(2, components.Count);
        Assert.Equal([0, 1], components[0]);
        Assert.Equal([2, 3], components[1]);
    }
}
