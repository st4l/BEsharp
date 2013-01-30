// ----------------------------------------------------------------------------------------------------
// <copyright file="PacketProblemType.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    public enum PacketProblemType
    {
        /// <summary>
        /// The packet was corrupted.
        /// </summary>
        Corrupted,

        /// <summary>
        /// The packet was of an incorrect size.
        /// </summary>
        InvalidLength
    }
}
