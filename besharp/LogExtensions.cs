using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Core;

namespace BESharp
{
    public static class LogExtensions
    {
        [Conditional("TRACE")]
        public static void Trace(this ILog logger, string msg)
        {
            logger.Logger.Log(
                logger.GetType(),
                Level.Debug, // Level.Trace
                msg,
                null);
        }


        [Conditional("TRACE")]
        public static void TraceFormat(this ILog logger, string fmt, params object[] args)
        {
            logger.Logger.Log(
                logger.GetType(),
                Level.Debug, // Level.Trace
                string.Format(CultureInfo.InvariantCulture, fmt, args),
                null);
        }



    }
}
