using System;
using System.Security.Cryptography;

namespace LockerEtl.Processing
{
    /// <summary>Generates numeric pickup PINs using a cryptographically secure random source.</summary>
    public interface IPinGenerator
    {
        string Generate();
    }

    public class PinGenerator : IPinGenerator
    {
        private readonly int _length;

        public PinGenerator(int length = 6)
        {
            if (length is < 4 or > 12)
                throw new ArgumentOutOfRangeException(nameof(length), "PIN length must be between 4 and 12.");
            _length = length;
        }

        public string Generate()
        {
            // GetInt32 is uniform; no modulo bias, unlike Random.Next() % 10.
            return string.Create(_length, 0, (span, _) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            });
        }
    }
}