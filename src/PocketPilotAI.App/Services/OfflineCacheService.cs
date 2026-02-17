namespace PocketPilotAI.App.Services;

public class OfflineCacheService
{
  private readonly Dictionary<string, object> cache = new(StringComparer.OrdinalIgnoreCase);

  public void Set<T>(string key, T value)
  {
    cache[key] = value!;
  }

  public T? Get<T>(string key)
  {
    return cache.TryGetValue(key, out object? value) ? (T)value : default;
  }
}
