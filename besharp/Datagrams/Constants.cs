// ----------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    public static class Constants
    {
        public const int DatagramTypeIndex = 7;

        public const int LoginReturnCodeIndex = 8;

        public const int ConsoleMessageSequenceNumberIndex = 8;

        public const int ConsoleMessageBodyStartIndex = 9;

        public const int CommandResponseSequenceNumberIndex = 8;

        public const int CommandResponseMultipartMarkerIndex = 9;

        public const int CommandResponseMultipartTotalPartsIndex = 10;

        public const int CommandResponseMultipartPartNumberIndex = 11;
    }
}
