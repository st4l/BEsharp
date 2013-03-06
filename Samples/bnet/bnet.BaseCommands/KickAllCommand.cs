// ----------------------------------------------------------------------------------------------------
// <copyright file="KickAllCommand.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.BaseCommands
{
    using System.Globalization;
    using BattleNET;
    using BNet.IoC;


    public class KickAllCommand : RConCommandBase
    {
        public KickAllCommand()
        {
            this.KickReason = "SERVER IS RESTARTING";
        }


        public string KickReason { get; set; }

        public override string RConCommandText
        {
            get { return "Kick "; }
        }


        public override void Execute(BattlEyeClient beClient, int timeoutSecs = 10)
        {
            for (int i = 0; i <= 100; i++)
            {
                string cmd = string.Format(
                    CultureInfo.InvariantCulture, "Kick {0} {1}", i, this.KickReason);
                this.Log.InfoFormat("Sending command: '{0}'", cmd);
                this.SendCommandPacket(beClient, cmd);
            }

            while (beClient.CommandQueue > 0)
            {
                /* wait until server received all packets */
            }
        }
    }
}
