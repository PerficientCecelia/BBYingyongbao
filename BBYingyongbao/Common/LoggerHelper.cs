using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBYingyongbao.Common
{
    public class LoggerHelper
    {
        public static void LogInfo(Type t, string Message)
        {
            log4net.ILog log = log4net.LogManager.GetLogger("NETCoreRepository", t);
            log.Info(Message);
        }

        public static void LogInfo(Type t, string Message, Exception ex)
        {
            log4net.ILog log = log4net.LogManager.GetLogger("NETCoreRepository", t);
            log.Info(Message, ex);
        }

        public static void ErrorInfo(Type t, string Message)
        {
            log4net.ILog log = log4net.LogManager.GetLogger("NETCoreRepository", t);
            log.Error(Message);
        }

        public static void ErrorInfo(Type t, string Message, Exception ex)
        {
            log4net.ILog log = log4net.LogManager.GetLogger("NETCoreRepository", t);
            log.Error(Message, ex);
        }

    
    }
}
