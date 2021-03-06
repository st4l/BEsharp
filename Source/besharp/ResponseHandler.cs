﻿// ----------------------------------------------------------------------------------------------------
// <copyright file="ResponseHandler.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using Datagrams;
    using log4net;


    internal sealed class ResponseHandler : IDisposable
    {
        private ManualResetEventSlim waitHandle;

        private bool disposed;


        /// <summary>
        ///   Creates a new instance of <see cref="ResponseHandler" /> for
        ///   the specified sent datagram and timeout in milliseconds.
        /// </summary>
        /// <param name="sentDatagram"> The sent datagram of which a response is awaited. </param>
        internal ResponseHandler(IOutboundDatagram sentDatagram)
        {
            this.SentDatagram = sentDatagram;
            this.log = LogManager.GetLogger(this.GetType());
        }


        /// <summary>
        ///   Use C# destructor syntax for finalization code.
        /// </summary>
        /// <remarks>
        ///   This destructor will run only if the Dispose method 
        ///   does not get called. 
        ///   It gives your base class the opportunity to finalize. 
        ///   Do not provide destructors in types derived from this class.
        /// </remarks>
        ~ResponseHandler()
        {
            this.Dispose(false);
        }


        /// <summary>
        ///   The Datagram that was sent, the response of which
        ///   this handler can wait for.
        /// </summary>
        public IOutboundDatagram SentDatagram { get; private set; }

        /// <summary>
        ///   After the response is received, the received response 
        ///   message.
        /// </summary>
        public IInboundDatagram ResponseDatagram { get; internal set; }


        public bool Completed { get; set; }

        private readonly ILog log;


        /// <summary>
        ///   Blocks the current thread until a response is received
        ///   or the timeout elapses. If a response is received, 
        ///   the <see cref="ResponseDatagram" /> property
        ///   will contain the received datagram.
        /// </summary>
        /// <param name="timeout"> Timeout in milliseconds. </param>
        /// <returns> True if the response was received; otherwise, false. </returns>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        public Task<bool> WaitForResponse(int timeout)
        {
            if (this.Completed)
            {
                this.log.TraceFormat("Handler for type {0} didn't need to wait.", this.SentDatagram.Type);
                return Task.FromResult(true);
            }

            this.waitHandle = new ManualResetEventSlim(false);
            Task<bool> task = Task.Factory.StartNew(() => this.DoWait(timeout));
            //task.ConfigureAwait(false);
            return task;
        }


        /// <summary>
        ///   Blocks the current thread until a response is received
        ///   or 3 seconds elapse. If a response is received,
        ///   the <see cref="ResponseDatagram" /> property
        ///   will contain the received datagram.
        /// </summary>
        /// <returns> True if the response was received; otherwise, false. </returns>
        public Task<bool> WaitForResponse()
        {
            return this.WaitForResponse(1000 * 3);
        }


        /// <summary>
        ///   Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        ///   Accepts the response datagram and finishes any wait for it.
        /// </summary>
        /// <param name="result"> </param>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        internal void AcceptResponse(IInboundDatagram result)
        {
            this.ResponseDatagram = result;
            this.Completed = true;
            if (this.waitHandle != null)
            {
                this.waitHandle.Set();
            }
        }


        private bool DoWait(int timeout)
        {
            this.log.TraceFormat("Handler for type {0} starting wait.", this.SentDatagram.Type);
            bool result = this.waitHandle.Wait(timeout);
            this.log.TraceFormat("Handler for type {0} done waiting, result={1}.", this.SentDatagram.Type, result);
            return result;
        }


        /// <summary>
        ///   Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="notFromFinalizer"> True unless we're called from the finalizer, in which case only unmanaged resources can be disposed. </param>
        private void Dispose(bool notFromFinalizer)
        {
            // Check to see if Dispose has already been called. 
            if (!this.disposed)
            {
                // If notFromFinalizer, dispose all managed resources. 
                if (notFromFinalizer)
                {
                    // Dispose managed resources.
                    if (this.waitHandle != null)
                    {
                        this.waitHandle.Set();
                        this.waitHandle.Dispose();
                    }
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }
    }
}
