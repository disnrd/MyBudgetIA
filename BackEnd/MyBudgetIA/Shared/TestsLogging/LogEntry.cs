using Microsoft.Extensions.Logging;

namespace Shared.TestsLogging
{

    /// <summary>
    /// Represents a captured log entry emitted through <see cref="ILogger"/>.
    /// </summary>
    /// <param name="Level">Log level.</param>
    /// <param name="EventId">Event id.</param>
    /// <param name="Exception">Optional exception.</param>
    /// <param name="Message">Formatted message (for debugging convenience).</param>
    /// <param name="State">
    /// Structured state as key/value pairs. If the underlying logger uses message templates,
    /// the template is usually available under the key <c>{OriginalFormat}</c>.
    /// </param>
    public sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        Exception? Exception,
        string? Message,
        IReadOnlyList<KeyValuePair<string, object?>> State);

    /// <summary>
    /// In-memory test logger that captures all log entries for assertions.
    /// </summary>
    /// <typeparam name="T">The category type.</typeparam>
    /// <remarks>
    /// This is intended for unit tests. It stores log entries in memory and always reports itself as enabled.
    /// </remarks>
    public sealed class TestLogger<T> : ILogger<T>
    {
        /// <summary>
        /// Gets the captured log entries.
        /// </summary>
        public List<LogEntry> Entries { get; } = [];

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var stateList =
                state as IReadOnlyList<KeyValuePair<string, object?>>
                ?? [];

            Entries.Add(new LogEntry(
                Level: logLevel,
                EventId: eventId,
                Exception: exception,
                Message: formatter(state, exception),
                State: stateList));
        }

        /// <summary>
        /// Gets all entries matching the specified event id.
        /// </summary>
        public IEnumerable<LogEntry> ByEventId(int eventId)
            => Entries.Where(e => e.EventId.Id == eventId);

        /// <summary>
        /// Returns <see langword="true"/> if an entry exists for the specified event id.
        /// </summary>
        public bool ContainsEventId(int eventId)
            => Entries.Any(e => e.EventId.Id == eventId);

        /// <summary>
        /// Gets the first value from a log entry state by key, if present.
        /// </summary>
        public static object? GetStateValue(LogEntry entry, string key)
            => entry.State.FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.Ordinal)).Value;

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
