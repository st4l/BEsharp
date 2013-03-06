// ----------------------------------------------------------------------------------------------------
// <copyright file="IRconCommand.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using BattleNET;
    using log4net;


    public interface IRConCommand
    {
        RConCommandMetadata Metadata { get; set; }

        string RConCommandText { get; }

        ILog Log { get; }

        CommandExecContext Context { get; set; }

        void Execute(BattlEyeClient beClient, int timeoutSecs = 10);

        bool ExecuteSingle();
    }
}
