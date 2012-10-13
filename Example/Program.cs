using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            DLock.Client.DLockProvider provider = DLock.Client.DLockPool.GetDLockProvider("127.0.0.1");
            DLock.Client.Mutex mutex_a = provider.CreateMutex("a");

            if (mutex_a.WaitOne())
            {
                try
                {
                }
                finally
                {
                    mutex_a.ReleaseMutex();
                }
            }
        }
    }
}
