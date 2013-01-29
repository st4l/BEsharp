using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;

namespace BESharp.Tests
{
    public class MyDebugAppender : DebugAppender
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            Debug.Write(RenderLoggingEvent(loggingEvent));

            if (!this.ImmediateFlush)
            {
                return;
            }

            Debug.Flush();
            
        }
    }
}
