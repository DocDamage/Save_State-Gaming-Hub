// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.IO.Compression;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for save state creation, loading, and branching operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class SaveStateBranchingBenchmarks
{
    private byte[] _saveData = null!;
    private SaveStateNode _rootNode = null!;
    private List<SaveStateNode> _branchNodes = null!;
    private string _tempPath = null!;

    [Params(1024 * 1024, 10 * 1024 * 1024)] // 1MB, 10MB
    public int SaveSize { get; set; }

    [Params(10, 100)]
    public int BranchCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _saveData = new byte[SaveSize];
        Random.Shared.NextBytes(_saveData);
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);

        // Create a tree structure
        _rootNode = new SaveStateNode
        {
            Id = Guid.NewGuid(),
            Name = "Root",
            Data = _saveData,
            CreatedAt = DateTime.UtcNow,
            Children = new List<SaveStateNode>()
        };

        _branchNodes = new List<SaveStateNode>();
        CreateBranchStructure(_rootNode, BranchCount, 3); // 3 levels deep
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    [Benchmark]
    public SaveStateNode CreateSaveState()
    {
        return new SaveStateNode
        {
            Id = Guid.NewGuid(),
            Name = $"Save {Guid.NewGuid()}",
            Data = _saveData.ToArray(),
            CreatedAt = DateTime.UtcNow,
            Children = new List<SaveStateNode>()
        };
    }

    [Benchmark]
    public SaveStateNode CreateBranch()
    {
        var branch = new SaveStateNode
        {
            Id = Guid.NewGuid(),
            Name = "Branch",
            Data = _saveData.ToArray(),
            CreatedAt = DateTime.UtcNow,
            ParentId = _rootNode.Id,
            Children = new List<SaveStateNode>()
        };
        _rootNode.Children.Add(branch);
        return branch;
    }

    [Benchmark]
    public List<SaveStateNode> TraverseTree_DFS()
    {
        var result = new List<SaveStateNode>();
        TraverseDFS(_rootNode, result);
        return result;
    }

    [Benchmark]
    public List<SaveStateNode> TraverseTree_BFS()
    {
        var result = new List<SaveStateNode>();
        var queue = new Queue<SaveStateNode>();
        queue.Enqueue(_rootNode);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            result.Add(node);

            foreach (var child in node.Children)
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    [Benchmark]
    public SaveStateNode? FindNodeById()
    {
        var targetId = _branchNodes[_branchNodes.Count / 2].Id;
        return FindNode(_rootNode, targetId);
    }

    [Benchmark]
    public int CalculateTreeDepth()
    {
        return GetDepth(_rootNode);
    }

    [Benchmark]
    public List<SaveStateNode> GetBranchPath()
    {
        var leaf = _branchNodes[_branchNodes.Count - 1];
        var path = new List<SaveStateNode>();
        var current = leaf;

        while (current != null)
        {
            path.Add(current);
            current = current.ParentId.HasValue
                ? FindNode(_rootNode, current.ParentId.Value)
                : null;
        }

        path.Reverse();
        return path;
    }

    [Benchmark]
    public async Task SaveToDisk()
    {
        var filePath = Path.Combine(_tempPath, $"{Guid.NewGuid()}.sav");
        await File.WriteAllBytesAsync(filePath, _saveData);
    }

    [Benchmark]
    public byte[] CompressSaveState()
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(_saveData, 0, _saveData.Length);
        }
        return output.ToArray();
    }

    [Benchmark]
    public async Task SaveCompressedToDisk()
    {
        var compressed = CompressSaveState();
        var filePath = Path.Combine(_tempPath, $"{Guid.NewGuid()}.sav.gz");
        await File.WriteAllBytesAsync(filePath, compressed);
    }

    [Benchmark]
    public int CountTotalNodes()
    {
        return CountNodes(_rootNode);
    }

    [Benchmark]
    public long CalculateTotalSize()
    {
        var nodes = new List<SaveStateNode>();
        TraverseDFS(_rootNode, nodes);
        return nodes.Sum(n => (long)n.Data.Length);
    }

    private void CreateBranchStructure(SaveStateNode parent, int count, int depth)
    {
        if (depth <= 0) return;

        int branchesPerNode = Math.Max(1, count / depth);
        for (int i = 0; i < branchesPerNode && _branchNodes.Count < count; i++)
        {
            var child = new SaveStateNode
            {
                Id = Guid.NewGuid(),
                Name = $"Branch {depth}-{i}",
                Data = _saveData.Take(1024).ToArray(), // Smaller data for branches
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                ParentId = parent.Id,
                Children = new List<SaveStateNode>()
            };

            parent.Children.Add(child);
            _branchNodes.Add(child);

            CreateBranchStructure(child, count, depth - 1);
        }
    }

    private static void TraverseDFS(SaveStateNode node, List<SaveStateNode> result)
    {
        result.Add(node);
        foreach (var child in node.Children)
        {
            TraverseDFS(child, result);
        }
    }

    private static SaveStateNode? FindNode(SaveStateNode root, Guid id)
    {
        if (root.Id == id) return root;

        foreach (var child in root.Children)
        {
            var found = FindNode(child, id);
            if (found != null) return found;
        }

        return null;
    }

    private static int GetDepth(SaveStateNode node)
    {
        if (!node.Children.Any()) return 1;
        return 1 + node.Children.Max(GetDepth);
    }

    private static int CountNodes(SaveStateNode node)
    {
        return 1 + node.Children.Sum(CountNodes);
    }
}

/// <summary>
/// Represents a node in the save state tree structure.
/// </summary>
public class SaveStateNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
    public Guid? ParentId { get; set; }
    public List<SaveStateNode> Children { get; set; } = new();
    public string? Description { get; set; }
    public string? ThumbnailPath { get; set; }
}
