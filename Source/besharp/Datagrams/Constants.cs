// ----------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    internal static class Constants
    {
        /// <summary>
        /// 7
        /// </summary>
        public const int DatagramTypeIndex = 7;

        /// <summary>
        /// 8
        /// </summary>
        public const int LoginReturnCodeIndex = 8;

        /// <summary>
        /// 8
        /// </summary>
        public const int ConsoleMessageSequenceNumberIndex = 8;

        /// <summary>
        /// 9
        /// </summary>
        public const int ConsoleMessageBodyStartIndex = 9;

        /// <summary>
        /// 8
        /// </summary>
        public const int CommandResponseSequenceNumberIndex = 8;

        /// <summary>
        /// 9
        /// </summary>
        public const int CommandResponseMultipartMarkerIndex = 9;

        /// <summary>
        /// 10
        /// </summary>
        public const int CommandResponseMultipartTotalPartsIndex = 10;

        /// <summary>
        /// 11
        /// </summary>
        public const int CommandResponseMultipartPartNumberIndex = 11;

        /// <summary>
        /// 9
        /// </summary>
        public const int DatagramMinLength = 9;

    }
}
