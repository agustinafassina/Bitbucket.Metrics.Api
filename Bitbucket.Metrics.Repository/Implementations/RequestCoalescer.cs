using System.Collections.Concurrent;

namespace Bitbucket.Metrics.Repository.Implementations
{
    public sealed class RequestCoalescer
    {
        private readonly ConcurrentDictionary<string, Lazy<Task>> _inFlight = new();

        public int InFlightCount => _inFlight.Count;
        public Task<T> RunAsync<T>(string key, Func<Task<T>> factory, CancellationToken cancellationToken = default)
        {
            Lazy<Task> lazy = _inFlight.GetOrAdd(
                key,
                k => new Lazy<Task>(() => ExecuteAsync(k, factory), LazyThreadSafetyMode.ExecutionAndPublication));

            return ((Task<T>)lazy.Value).WaitAsync(cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> factory)
        {
            try
            {
                return await factory().ConfigureAwait(false);
            }
            finally
            {
                // Only the Lazy that won GetOrAdd ever executes, so removing by key removes this operation.
                _inFlight.TryRemove(key, out _);
            }
        }
    }
}