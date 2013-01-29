// ----------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    public static class Constants
    {


        #region Constants

        public const int DatagramTypeIndex = 7;

        public const int LoginReturnCodeIndex = 8;

        public const int ConsoleMessageSequenceNumberIndex = 8;

        public const int ConsoleMessageBodyStartIndex = 9;

        public const int CommandResponseSequenceNumberIndex = 8;
        
        public const int CommandResponseMultipartFlag = 9;

        public const int CommandResponseMultipartTotalPartsIndex = 10;
        public const int CommandResponseMultipartPartNumberIndex = 11;

        
        #endregion


    }
}
