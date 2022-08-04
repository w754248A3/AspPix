using System;
using System.Diagnostics;

namespace AspPix
{
    public class LogExit
    {
      
        public static void Init()
        {
            
        }

        public static void KillSelf()
        {
            Process.GetCurrentProcess().Kill();
        }

        public static TR OnErrorExit<TR>(string name, Func<TR> func)
        {
            try
            {

                return func();

            }
            catch(Exception e)
            {
                //按实现这个方法直接杀死自身进程不会返回
                LogExit.Exit($"{name} {e}");

                throw;
            }
        }

        public static void Exit(object e)
        {
            string s = e.ToString();

            Console.WriteLine(s);

            KillSelf();
        }

    }
}