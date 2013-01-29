// ----------------------------------------------------------------------------------------------------
// <copyright file="CRC32.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System.Security.Cryptography;


    internal class Crc32 : HashAlgorithm
    {
        public const uint DefaultPolynomial = 0x04C11DB7;

        public const uint DefaultPolynomialReversed = 0xEDB88320;

        public const uint DefaultSeed = 0xffffffff;


        private static uint[] defaultTable;

        private readonly uint seed;

        private readonly uint[] table;

        private uint hash;
        private static uint[] defaultTableReversed;


        public Crc32()
        {
            this.table = InitializeTable(DefaultPolynomialReversed);
            this.seed = DefaultSeed;
            this.Initialize();
        }


        public Crc32(uint polynomial, uint seed)
        {
            this.table = InitializeTable(polynomial);
            this.seed = seed;
            this.Initialize();
        }


        public override int HashSize
        {
            get { return 32; }
        }



        public override sealed void Initialize()
        {
            this.hash = this.seed;
        }


        protected override void HashCore(byte[] buffer, int start, int length)
        {
            this.hash = CalculateHash(this.table, this.hash, buffer, start, length);
        }


        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = UInt32ToBigEndianBytes(~this.hash);
            this.HashValue = hashBuffer;
            return hashBuffer;
        }


        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultTable != null)
            {
                return defaultTable;
            }

            if (polynomial == DefaultPolynomialReversed && defaultTableReversed != null)
            {
                return defaultTableReversed;
            }

            var createTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                var entry = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                    {
                        entry = (entry >> 1) ^ polynomial;
                    }
                    else
                    {
                        entry = entry >> 1;
                    }
                }

                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
            {
                defaultTable = createTable;
            }

            if (polynomial == DefaultPolynomialReversed)
            {
                defaultTableReversed = createTable;
            }

            return createTable;
        }


        private static uint CalculateHash(
            uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            uint crc = seed;
            for (int i = start; i < size; i++)
            {
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
                }
            }

            return crc;
        }


        private static byte[] UInt32ToBigEndianBytes(uint x)
        {
            return new[]
                       {
                           (byte)((x >> 24) & 0xff), (byte)((x >> 16) & 0xff), (byte)((x >> 8) & 0xff), 
                           (byte)(x & 0xff)
                       };
        }
    }
}
