
namespace BESharp.Tests
{
    using System;

    public class TestDatagram
    {
        public TestDatagram(byte[] payload)
        {
            this.Payload = payload;
            this.Timestamp = DateTime.Now;
        }

        public DateTime Timestamp { get; set; }

        public byte[] Payload { get; set; }
    }
}
