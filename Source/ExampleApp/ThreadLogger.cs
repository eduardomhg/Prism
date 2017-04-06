using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrismUnityApp1
{
    public static class ThreadLogger
    {
        public static void Log(string message, string className, [CallerMemberName]string memberName = "")
        {
            Debug.WriteLine($"---- LOG [{DateTime.Now.ToLongTimeString()}][{className}.{memberName}, thread {Thread.CurrentThread.ManagedThreadId}]: {message}");
        }
    }
}
