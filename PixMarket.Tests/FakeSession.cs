using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace PixMarket.Tests;

public class FakeSession : ISession
{
    private readonly ConcurrentDictionary<string, byte[]> _data = new();

    public bool IsAvailable => true;

    public string Id => "fake-session";

    public IEnumerable<string> Keys => _data.Keys;

    public void Clear() => _data.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Remove(string key) => _data.TryRemove(key, out _);

    public void Set(string key, byte[] value) => _data[key] = value;

    public bool TryGetValue(string key, out byte[] value)
        => _data.TryGetValue(key, out value!);
}