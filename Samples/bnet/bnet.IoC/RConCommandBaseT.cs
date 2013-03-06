// ----------------------------------------------------------------------------------------------------
// <copyright file="RConCommandBaseT.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using System;
    using System.Threading;
    using BattleNET;


    public abstract class RConCommandBase<TResultType> : RConCommandBase, IRConCommand<TResultType>
        where TResultType : class
    {
        public TResultType Result { get; protected set; }


        public virtual bool ExecAwaitResponse(BattlEyeClient beClient, int timeoutSecs = 10)
        {
            this.Execute(beClient, timeoutSecs);

            DateTime timeout = DateTime.Now.AddSeconds(timeoutSecs);
            while (DateTime.Now < timeout && this.RawResponse == null)
            {
                Thread.Sleep(500);
            }

            if (this.RawResponse == null)
            {
                throw new RConException("ERROR: Timeout while waiting for command '" + this.RConCommandText + "' response.");
            }

            try
            {
                this.Result = this.ParseResponse(this.RawResponse);
            }
            catch (Exception e)
            {
                throw new RConException(
                    "ERROR: Could not parse response: \r\n" + this.RawResponse, e);
            }

            return this.Result != null;
        }


        public virtual bool ExecSingleAwaitResponse()
        {
            var beClient = new BattlEyeClient(this.Context.Server.LoginCredentials)
                               {
                                   ReconnectOnPacketLoss
                                       = true, 
                                   DiscardConsoleMessages
                                       = true
                               };

            BattlEyeConnectionResult connect = beClient.Connect();
            if (connect != BattlEyeConnectionResult.Success)
            {
                beClient.Disconnect();
                throw new RConException("ERROR: Could not connect to the server: " + connect);
            }

            this.Log.DebugFormat("Sending command: '{0}'", this.Metadata.Name);
            this.ExecAwaitResponse(beClient);
            beClient.Disconnect();

            return this.Result != null;

            // ~beClient()
        }


        protected abstract TResultType ParseResponse(string rawResponse);
    }
}
