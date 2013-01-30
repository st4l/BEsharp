// ----------------------------------------------------------------------------------------------------
// <copyright file="OutboundDatagramBase.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;


    public abstract class OutboundDatagramBase : DatagramBase, IOutboundDatagram
    {
        /// <summary>
        ///   The date and time this message was sent. Set automatically by 
        ///   <see cref="RConClient" />.
        /// </summary>
        public DateTime SentTime { get; set; }

        public abstract bool ExpectsResponse { get; }


        public byte[] Build()
        {
            var payload = this.BuildPayload();

            byte[] checksum;
            using (var crc = new Crc32(Crc32.DefaultPolynomialReversed, Crc32.DefaultSeed))
            {
                checksum = crc.ComputeHash(payload);
                Array.Reverse(checksum);
            }

            var payloadLen = Buffer.ByteLength(payload);
            var result = new byte[6 + payloadLen];
            Buffer.SetByte(result, 0, 0x42); // "B"
            Buffer.SetByte(result, 1, 0x45); // "E"
            Buffer.BlockCopy(checksum, 0, result, 2, 4);
            Buffer.BlockCopy(payload, 0, result, 6, payloadLen);

            return result;
        }


        protected abstract byte[] BuildPayload();
    }
}
