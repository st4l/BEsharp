// ----------------------------------------------------------------------------------------------------
// <copyright file="AcknowledgeMessageDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    internal class AcknowledgeMessageDatagram : OutboundDatagramBase
    {
        private readonly byte sequenceNumber;


        public AcknowledgeMessageDatagram(byte sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }


        public override DatagramType Type
        {
            get { return DatagramType.Message; }
        }

        public override bool ExpectsResponse
        {
            get { return false; }
        }


        protected override byte[] BuildPayload()
        {
            // eg 42 45 7d 8f ef 73 ff 02 00   BE......
            return new[] { (byte)0xFF, (byte)0x02, this.sequenceNumber };
        }
    }
}
