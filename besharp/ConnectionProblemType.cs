// ----------------------------------------------------------------------------------------------------
// <copyright file="ConnectionProblemType.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    public enum ConnectionProblemType
    {
        /// <summary>
        /// The datagram was corrupted.
        /// </summary>
        Corrupted,

        /// <summary>
        /// The datagram was of an incorrect size.
        /// </summary>
        InvalidLength
    }
}
