// ----------------------------------------------------------------------------------------------------
// <copyright file="IRconCommandT.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using BattleNET;


    public interface IRConCommand<out TResultType> : IRConCommand
        where TResultType : class
    {
        TResultType Result { get; }

        bool ExecAwaitResponse(BattlEyeClient beClient, int timeoutSecs);

        bool ExecSingleAwaitResponse();
    }
}
