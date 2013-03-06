// ----------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC.Log4Net
{
    using System;
    using System.Globalization;
    using log4net;
    using log4net.Core;


    public static class Extensions
    {
        public static void Trace(this ILog log, object message, Exception exception = null)
        {
            log.Logger.Log(log.GetType(), Level.Trace, message, exception);
        }


        public static void TraceFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(
                log.GetType(), 
                Level.Trace, 
                string.Format(CultureInfo.InvariantCulture, format, args), 
                null);
        }


        public static void Fine(this ILog log, object message, Exception exception = null)
        {
            log.Logger.Log(log.GetType(), Level.Fine, message, exception);
        }


        public static void FineFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(
                log.GetType(),
                Level.Fine,
                string.Format(CultureInfo.InvariantCulture, format, args),
                null);
        }

        public static void Verbose(this ILog log, object message, Exception exception = null)
        {
            log.Logger.Log(log.GetType(), Level.Verbose, message, exception);
        }


        public static void VerboseFormat(this ILog log, string format, params object[] args)
        {
            log.Logger.Log(
                log.GetType(),
                Level.Verbose,
                string.Format(CultureInfo.InvariantCulture, format, args),
                null);
        }


    }
}
