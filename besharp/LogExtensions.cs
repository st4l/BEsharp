// ----------------------------------------------------------------------------------------------------
// <copyright file="LogExtensions.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

namespace BESharp
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using log4net.Core;

    public static class LogExtensions
    {
        [Conditional("TRACE")]
        public static void Trace(this ILoggerWrapper logger, string message)
        {
            if (logger == null || logger.Logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            
            logger.Logger.Log(
                              logger.GetType(),
                              Level.Debug, // Level.Trace
                              message,
                              null);
        }


        [Conditional("TRACE")]
        public static void TraceFormat(this ILoggerWrapper logger, string format, params object[] args)
        {
            if (logger == null || logger.Logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            logger.Logger.Log(
                              logger.GetType(),
                              Level.Debug, // Level.Trace
                              string.Format(CultureInfo.InvariantCulture, format, args),
                              null);
        }
    }
}
