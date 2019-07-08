using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OKEGui.Utils
{
    /// <summary>
    /// Implementation of CRC-32.
    /// This class supports several convenient static methods returning the CRC as UInt32.
    /// </summary>
    public class CRC32 : HashAlgorithm
    {
        private uint _currentCrc;

        /// <summary>
        /// Initializes a new instance of the <see cref="Crc32Algorithm"/> class.
        /// </summary>
        public CRC32()
        {
#if !NETCORE
            HashSizeValue = 32;
#endif
        }

        /// <summary>
        /// Computes CRC-32 from multiple buffers.
        /// Call this method multiple times to chain multiple buffers.
        /// </summary>
        /// <param name="initial">
        /// Initial CRC value for the algorithm. It is zero for the first buffer.
        /// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
        /// </param>
        /// <param name="input">Input buffer with data to be checksummed.</param>
        /// <param name="offset">Offset of the input data within the buffer.</param>
        /// <param name="length">Length of the input data in the buffer.</param>
        /// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
        public static uint Append(uint initial, byte[] input, int offset, int length)
        {
            if (input == null)
                throw new ArgumentNullException();
            if (offset < 0 || length < 0 || offset + length > input.Length)
                throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
            return AppendInternal(initial, input, offset, length);
        }

        /// <summary>
        /// Computes CRC-3C from multiple buffers.
        /// Call this method multiple times to chain multiple buffers.
        /// </summary>
        /// <param name="initial">
        /// Initial CRC value for the algorithm. It is zero for the first buffer.
        /// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
        /// </param>
        /// <param name="input">Input buffer containing data to be checksummed.</param>
        /// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
        public static uint Append(uint initial, byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException();
            return AppendInternal(initial, input, 0, input.Length);
        }

        /// <summary>
        /// Computes CRC-32 from input buffer.
        /// </summary>
        /// <param name="input">Input buffer with data to be checksummed.</param>
        /// <param name="offset">Offset of the input data within the buffer.</param>
        /// <param name="length">Length of the input data in the buffer.</param>
        /// <returns>CRC-32 of the data in the buffer.</returns>
        public static uint Compute(byte[] input, int offset, int length)
        {
            return Append(0, input, offset, length);
        }

        /// <summary>
        /// Computes CRC-32 from input buffer.
        /// </summary>
        /// <param name="input">Input buffer containing data to be checksummed.</param>
        /// <returns>CRC-32 of the buffer.</returns>
        public static uint Compute(byte[] input)
        {
            return Append(0, input);
        }

        /// <summary>
        /// Resets internal state of the algorithm. Used internally.
        /// </summary>
        public override void Initialize()
        {
            _currentCrc = 0;
        }

        /// <summary>
        /// Appends CRC-32 from given buffer
        /// </summary>
        protected override void HashCore(byte[] input, int offset, int length)
        {
            _currentCrc = AppendInternal(_currentCrc, input, offset, length);
        }

        /// <summary>
        /// Computes CRC-32 from <see cref="HashCore"/>
        /// </summary>
        protected override byte[] HashFinal()
        {
            // Crc32 by dariogriffo uses big endian, so, we need to be compatible and return big endian too
            return new[] { (byte)(_currentCrc >> 24), (byte)(_currentCrc >> 16), (byte)(_currentCrc >> 8), (byte)_currentCrc };
        }

        private static readonly SafeProxy Proxy = new SafeProxy();

        private static uint AppendInternal(uint initial, byte[] input, int offset, int length)
        {
            return length > 0 ? SafeProxy.Append(initial, input, offset, length) : initial;
        }

        /// <summary>
        /// Calculate file's CRC32 value
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<uint> FileCRC(string filePath)
        {
            if (!File.Exists(filePath)) return 0;
            var hash = new CRC32();
            const int capacity = 1024 * 1024;
            var buffer = new byte[capacity];
            using (var file = File.OpenRead(filePath))
            {
                int cbSize;
                do
                {
                    cbSize = await file.ReadAsync(buffer, 0, capacity).ConfigureAwait(false);
                    if (cbSize > 0) hash.HashCore(buffer, 0, cbSize);
                } while (cbSize > 0);
                return hash._currentCrc;
            }
        }

        /// <summary>
        /// Calculate file's CRC32 string value
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string ComputeChecksumString(string filePath)
        {
            uint numberInUint = FileCRC(filePath).Result;
            return numberInUint.ToString("X8");
        }
    }
}