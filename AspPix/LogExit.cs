using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AspPix
{
    public class LogExit
    {
      
        public static void KillSelf()
        {
            Process.GetCurrentProcess().Kill();
        }
        public static void OnErrorExit(string name, ILogger logger, Action func)
        {
            OnErrorExit<object>(name, logger, () => { func(); return null; });
        }

        public static async Task OnErrorExitAsync(string name, ILogger logger, Func<Task> func)
        {
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //按实现这个方法直接杀死自身进程不会返回
                LogExit.Exit(logger, $"{name} {e}");

                throw;
            }
        }

        public static TR OnErrorExit<TR>(string name, ILogger logger, Func<TR> func)
        {
            try
            {

                return func();

            }
            catch(Exception e)
            {
                //按实现这个方法直接杀死自身进程不会返回
                LogExit.Exit(logger, $"{name} {e}");

                throw;
            }
        }

        public static void Exit(ILogger logger, object e)
        {
            string s = e.ToString();

            Console.WriteLine(s);

            logger.LogError(s);

            KillSelf();
        }

    }
}