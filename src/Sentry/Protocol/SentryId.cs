using System;
using Newtonsoft.Json;

namespace Sentry.Protocol
{
    /// <summary>
    /// The identifier of an event in Sentry.
    /// </summary>
    [JsonConverter(typeof(SentryIdJsonConverter))]
    public readonly struct SentryId : IEquatable<SentryId>
    {
        private readonly Guid _eventId;

        /// <summary>
        /// An empty sentry id.
        /// </summary>
        public static readonly SentryId Empty = Guid.Empty;

        /// <summary>
        /// Creates a new instance of a Sentry Id.
        /// </summary>
        public SentryId(Guid guid) => _eventId = guid;

        /// <summary>
        /// Sentry Id in the format Sentry recognizes.
        /// </summary>
        /// <remarks>
        /// Default <see cref="ToString"/> of <see cref="Guid"/> includes
        /// dashes which sentry doesn't expect when searching events.
        /// </remarks>
        /// <returns>String representation of the event id.</returns>
        public override string ToString() => _eventId.ToString("n");

        /// <inheritdoc />
        public bool Equals(SentryId other) => _eventId.Equals(other._eventId);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SentryId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _eventId.GetHashCode();

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(SentryId left, SentryId right) => left.Equals(right);

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator !=(SentryId left, SentryId right) => !(left == right);

        /// <summary>
        /// The <see cref="Guid"/> from the <see cref="SentryId"/>.
        /// </summary>
        public static implicit operator Guid(SentryId sentryId) => sentryId._eventId;

        /// <summary>
        /// A <see cref="SentryId"/> from a <see cref="Guid"/>.
        /// </summary>
        public static implicit operator SentryId(Guid guid) => new SentryId(guid);
    }

    internal class SentryIdJsonConverter : JsonConverter<SentryId>
    {
        public override void WriteJson(
            JsonWriter writer,
            SentryId value,
            JsonSerializer serializer) =>
            writer.WriteValue(value.ToString());

        public override SentryId ReadJson(
            JsonReader reader,
            Type objectType,
            SentryId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) =>
            new SentryId(Guid.Parse((string)reader.Value));
    }
}
