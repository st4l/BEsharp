// ----------------------------------------------------------------------------------------------------
// <copyright file="ResponseHandler.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using log4net;

namespace BESharp
{
    using System;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using Datagrams;


    public class ResponseHandler : IDisposable
    {
        private ManualResetEventSlim waitHandle;

        private bool disposed;


        /// <summary>
        ///     Creates a new instance of <see cref="ResponseHandler" /> for
        ///     the specified sent datagram and timeout in milliseconds.
        /// </summary>
        /// <param name="sentDatagram">The sent datagram of which a response is awaited.</param>
        public ResponseHandler(IOutboundDatagram sentDatagram)
        {
            this.SentDatagram = sentDatagram;
            this.Log = LogManager.GetLogger(this.GetType());
        }


        protected ILog Log { get; set; }


        /// <summary>
        ///     Use C# destructor syntax for finalization code. 
        /// </summary>
        /// <remarks>
        ///     This destructor will run only if the Dispose method 
        ///     does not get called. 
        ///     It gives your base class the opportunity to finalize. 
        ///     Do not provide destructors in types derived from this class.
        /// </remarks>
        ~ResponseHandler()
        {
            this.Dispose(false);
        }


        /// <summary>
        ///     The Datagram that was sent, the response of which
        ///     this handler can wait for.
        /// </summary>
        public IOutboundDatagram SentDatagram { get; private set; }

        /// <summary>
        ///     After the response is received, the received response 
        ///     message.
        /// </summary>
        public IInboundDatagram ResponseDatagram { get; internal set; }


        public bool Completed { get; set; }


        /// <summary>
        ///     Blocks the current thread until a response is received
        ///     or the timeout elapses. If a response is received, 
        ///     the <see cref="ResponseDatagram"/> property
        ///     will contain the received datagram.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <returns>
        ///     True if the response was received; otherwise, false.
        /// </returns>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        public Task<bool> WaitForResponse(int timeout)
        {
            if (this.Completed)
            {
                this.Log.TraceFormat("Handler for type {0} didn't need to wait.", this.SentDatagram.Type);
                return Task.FromResult(true);
            }

            this.waitHandle = new ManualResetEventSlim(false);
            var task = Task.Factory.StartNew(() => this.DoWait(timeout));
            task.ConfigureAwait(false);
            return task;
        }


        private bool DoWait(int timeout)
        {
            this.Log.TraceFormat("Handler for type {0} starting wait.", this.SentDatagram.Type);
            bool result = this.waitHandle.Wait(timeout);
            this.Log.TraceFormat("Handler for type {0} done waiting, result={1}.", this.SentDatagram.Type, result);
            return result;
        }



        /// <summary>
        ///     Blocks the current thread until a response is received
        ///     or 3 seconds elapse. If a response is received,
        ///     the <see cref="ResponseDatagram"/> property
        ///     will contain the received datagram.
        /// </summary>
        /// <returns>
        ///     True if the response was received; otherwise, false.
        /// </returns>
        public Task<bool> WaitForResponse()
        {
            return WaitForResponse(1000 * 3);
        }


        /// <summary>
        ///     Accepts the response datagram and signals the waiting
        ///     thread to continue.
        /// </summary>
        /// <param name="result"></param>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        internal void Complete(IInboundDatagram result)
        {
            this.ResponseDatagram = result;
            this.Completed = true;
            if (this.waitHandle != null)
            {
                this.waitHandle.Set();
            }
        }


        /// <summary>
        ///     Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        ///     Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///     True unless we're called from the finalizer,
        ///     in which case only unmanaged resources can be disposed.
        /// </param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this.waitHandle != null)
                    {
                        this.waitHandle.Dispose();
                    }
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }


    }
}
