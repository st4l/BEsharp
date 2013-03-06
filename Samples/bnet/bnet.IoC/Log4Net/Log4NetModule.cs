// ----------------------------------------------------------------------------------------------------
// <copyright file="Log4NetModule.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC.Log4Net
{
    using System;
    using log4net;


    public class Log4NetModule : LogModule<ILog>
    {
        protected override ILog CreateLoggerFor(Type type)
        {
            return LogManager.GetLogger(type);
        }
    }
}
