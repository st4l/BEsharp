using System;

namespace BESharp
{
    public class PacketProblemEventArgs : EventArgs
    {
        public PacketProblemType PacketProblemType { get; private set; }


        public PacketProblemEventArgs(PacketProblemType packetProblemType)
        {
            this.PacketProblemType = packetProblemType;
        }
    }
}