// ----------------------------------------------------------------------------------------------------
// <copyright file="RConCommandBase.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using BattleNET;
    using log4net;



    public abstract class RConCommandBase : IRConCommand
    {
        public RConCommandMetadata Metadata { get; set; }

        public abstract string RConCommandText { get; }

        public ILog Log { get; set; }

        public CommandExecContext Context { get; set; }

        protected string RawResponse { get; set; }


        public virtual void Execute(BattlEyeClient beClient, int timeoutSecs = 10)
        {
            this.RawResponse = null;

            this.Log.DebugFormat("Sending command: '{0}'", this.Metadata.Name);
            BattlEyeCommandResult result = beClient.SendCommandPacket(
                this.RConCommandText, 
                handler: (o, args) => this.RawResponse = args.Message, 
                timeOutInSecs: timeoutSecs);
            if (result != BattlEyeCommandResult.Success)
            {
                throw new RConException("Error sending command '" + this.RConCommandText + "': " + result);
            }

            while (beClient.CommandQueue > 0)
            {
                /* wait until server acknowledged all commands */
            }
        }


        public virtual bool ExecuteSingle()
        {
            var beClient = new BattlEyeClient(this.Context.Server.LoginCredentials)
                               {
                                   ReconnectOnPacketLoss = true, 
                                   DiscardConsoleMessages = true
                               };

            var connect = beClient.Connect();
            if (connect != BattlEyeConnectionResult.Success)
            {
                beClient.Disconnect();
                throw new RConException("ERROR: Could not connect to the server: " + connect);
            }

            this.Execute(beClient);
            beClient.Disconnect();
            return true;

            // ~beClient()
        }


        protected BattlEyeCommandResult SendCommandPacket(BattlEyeClient beClient, string commandText)
        {
            var result = beClient.SendCommandPacket(commandText);
            this.Log.Info(result);

            if (result != BattlEyeCommandResult.Success)
            {
                throw new RConException(
                    "ERROR: There was an error sending command '" + commandText + "': " + result);
            }

            return result;
        }

    }
}
