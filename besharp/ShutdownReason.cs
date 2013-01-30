// ----------------------------------------------------------------------------------------------------
// <copyright file="ShutdownReason.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    public enum ShutdownReason
    {
        /// <summary>
        ///   Shouldn't shut down.
        /// </summary>
        None,

        /// <summary>
        ///   No response from the server was received after several attempts 
        ///   on contacting it.
        /// </summary>
        NoResponseFromServer,

        /// <summary>
        ///   The <see cref="RConClient.Close" /> method was called.
        /// </summary>
        UserRequested,

        /// <summary>
        ///   The testing server requested shutdown.
        /// </summary>
        ServerRequested,

        /// <summary>
        ///   An unhandled exception was detected on one of the running threads.
        /// </summary>
        FatalException
    }
}
