namespace BNet.Tests
{
    using System;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Layout;


    public class DebugLogger
    {
        static DebugLogger()
        {
            
        }


        public static ILog GetLogger(Type t)
        {
            return LogManager.GetLogger(t);
        }
    }
}
