// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

using System;

namespace org.zhangwenqing.utilities
{
    public static class GuidExtensions
    {
        private const byte Variant10xxMask = 0xC0;
        private const byte Variant10xxValue = 0x80;
        private const ushort VersionMask = 0xF000;
        private const ushort Version7Value = 0x7000;

        /// <summary>Gets the value of the variant field for the <see cref="Guid" />.</summary>
        /// <remarks>
        ///     <para>This corresponds to the most significant 4 bits of the 8th byte: 00000000-0000-0000-F000-000000000000. The "don't-care" bits are not masked out.</para>
        ///     <para>See RFC 9562 for more information on how to interpret this value.</para>
        /// </remarks>
        public static int GetVariant(this Guid guid) => guid.ToByteArray()[8] >> 4;

        /// <summary>Gets the value of the version field for the <see cref="Guid" />.</summary>
        /// <remarks>
        ///     <para>This corresponds to the most significant 4 bits of the 6th byte: 00000000-0000-F000-0000-000000000000.</para>
        ///     <para>See RFC 9562 for more information on how to interpret this value.</para>
        /// </remarks>
        public static int GetVersion(this Guid guid) =>
            BitConverter.ToUInt16(guid.ToByteArray(), 6) >> 12;


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Guid CreateVersion4() => Guid.NewGuid();

        /// <summary>Creates a new <see cref="Guid" /> according to RFC 9562, following the Version 7 format.</summary>
        /// <returns>A new <see cref="Guid" /> according to RFC 9562, following the Version 7 format.</returns>
        /// <remarks>
        ///     <para>This uses <see cref="DateTimeOffset.UtcNow" /> to determine the Unix Epoch timestamp source.</para>
        ///     <para>This seeds the rand_a and rand_b sub-fields with random data.</para>
        /// </remarks>
        public static Guid CreateVersion7() =>
            CreateVersion7(DateTimeOffset.UtcNow);

        /// <summary>Creates a new <see cref="Guid" /> according to RFC 9562, following the Version 7 format.</summary>
        /// <param name="timestamp">The date time offset used to determine the Unix Epoch timestamp.</param>
        /// <returns>A new <see cref="Guid" /> according to RFC 9562, following the Version 7 format.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timestamp" /> represents an offset prior to <see cref="DateTimeOffset.UnixEpoch" />.</exception>
        /// <remarks>
        ///     <para>This seeds the rand_a and rand_b sub-fields with random data.</para>
        /// </remarks>
        public static Guid CreateVersion7(DateTimeOffset timestamp)
        {
            // NewGuid uses CoCreateGuid on Windows and Interop.GetCryptographicallySecureRandomBytes on Unix to get
            // cryptographically-secure random bytes. We could use Interop.BCrypt.BCryptGenRandom to generate the random
            // bytes on Windows, as is done in RandomNumberGenerator, but that's measurably slower than using CoCreateGuid.
            // And while CoCreateGuid only generates 122 bits of randomness, the other 6 bits being for the version / variant
            // fields, this method also needs those bits to be non-random, so we can just use NewGuid for efficiency.
            var result = Guid.NewGuid().ToByteArray().AsSpan();

            // 2^48 is roughly 8925.5 years, which from the Unix Epoch means we won't
            // overflow until around July of 10,895. So there isn't any need to handle
            // it given that DateTimeOffset.MaxValue is December 31, 9999. However, we
            // can't represent timestamps prior to the Unix Epoch since UUIDv7 explicitly
            // stores a 48-bit unsigned value, so we do need to throw if one is passed in.
            var unixTimeMs = timestamp.ToUnixTimeMilliseconds();

            // Write the Unix Epoch timestamp into the first 6 bytes of the UUID.
            BitConverter.GetBytes((int)(unixTimeMs >> 16)).CopyTo(result);
            BitConverter.GetBytes((short)unixTimeMs).CopyTo(result[4..]);

            // Write the version fields into the UUID.
            BitConverter.GetBytes((short)((BitConverter.ToInt16(result.Slice(6,2)) & ~VersionMask) | Version7Value))
                .CopyTo(result[6..]);

            // Write the variant fields into the UUID.
            result[8] = (byte)((result[8] & ~Variant10xxMask) | Variant10xxValue);

            return new Guid(result);
        }
    }
}