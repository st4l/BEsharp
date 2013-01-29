namespace BESharp.Datagrams
{
    public class CommandResponseDatagram : InboundDatagramBase
    {
        public CommandResponseDatagram()
        {
        }

        public CommandResponseDatagram(byte[] buffer)
        {
            this.OriginalSequenceNumber = buffer[Constants.CommandResponseSequenceNumberIndex];
        }

        public override DatagramType Type
        {
            get { return DatagramType.Command; }
        }
        public string Body { get; protected set; }
        public byte OriginalSequenceNumber { get; protected set; }
    }
}