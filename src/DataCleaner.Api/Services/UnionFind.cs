namespace DataCleaner.Api.Services;

/// <summary>
/// Disjoint-set (Union-Find) with path compression and union by rank.
/// </summary>
public sealed class UnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public UnionFind(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        _parent = new int[size];
        _rank = new int[size];
        for (var i = 0; i < size; i++)
        {
            _parent[i] = i;
        }
    }

    public int Find(int x)
    {
        if (_parent[x] != x)
        {
            _parent[x] = Find(_parent[x]);
        }

        return _parent[x];
    }

    public void Union(int a, int b)
    {
        var rootA = Find(a);
        var rootB = Find(b);
        if (rootA == rootB)
        {
            return;
        }

        if (_rank[rootA] < _rank[rootB])
        {
            _parent[rootA] = rootB;
        }
        else if (_rank[rootA] > _rank[rootB])
        {
            _parent[rootB] = rootA;
        }
        else
        {
            _parent[rootB] = rootA;
            _rank[rootA]++;
        }
    }

    public IReadOnlyList<IReadOnlyList<int>> GetComponents()
    {
        var map = new Dictionary<int, List<int>>();
        for (var i = 0; i < _parent.Length; i++)
        {
            var root = Find(i);
            if (!map.TryGetValue(root, out var list))
            {
                list = [];
                map[root] = list;
            }

            list.Add(i);
        }

        return map.Values.Select(v => (IReadOnlyList<int>)v).ToList();
    }
}
