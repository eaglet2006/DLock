using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Example
{
    class Program
    {
        static void TestPerformance(string ipAddress)
        {
            DLock.Client.DLockProvider provider = DLock.Client.DLockPool.GetDLockProvider(ipAddress);
            DLock.Client.Mutex mutex_a = provider.CreateMutex("a");

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            for (int i = 0; i < 1000000000; i++)
            {
                if (mutex_a.WaitOne())
                {
                    try
                    {
                        //Console.WriteLine("Entry");
                        //System.Threading.Thread.Sleep(5000);

                    }
                    finally
                    {
                        mutex_a.ReleaseMutex();
                    }

                    if ((i % 100000) == 0)
                    {
                        System.Threading.Thread.Sleep(10);

                        sw.Stop();
                        Console.WriteLine("{0} per second", 100000 * 1000 / sw.ElapsedMilliseconds);
                        sw.Reset();
                        sw.Start();
                    }

                    //Console.WriteLine("Leave");
                }
            }

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);


        }

        static void TestDispose(string ipAddress)
        {
            DLock.Client.DLockProvider provider = DLock.Client.DLockPool.GetDLockProvider(ipAddress);
            DLock.Client.Mutex mutex_a = provider.CreateMutex("a");

            if (mutex_a.WaitOne())
            {
                try
                {
                    if (mutex_a.Suspected)
                    {
                        Console.WriteLine("Suspected");
                    }
                    //do something.
                }
                finally
                {
                    //mutex_a.ReleaseMutex();
                }
            }

            mutex_a = null;
            GC.Collect(GC.MaxGeneration);
            GC.Collect(GC.MaxGeneration);

        }

        static void MutexExample(string ipAddress)
        {
            DLock.Client.DLockProvider provider = DLock.Client.DLockPool.GetDLockProvider(ipAddress);
            DLock.Client.Mutex mutex_a = provider.CreateMutex("a");
            if (mutex_a.WaitOne())
            {
                try
                {
                    //do something.
                    if (mutex_a.Suspected)
                    {
                        Console.WriteLine("Suspected");
                    }

                    Console.WriteLine("Sleep 30 seconds");

                    System.Threading.Thread.Sleep(30 * 1000);

                }
                finally
                {
                    mutex_a.ReleaseMutex();
                }
            }
        }

        static void Main(string[] args)
        {
            //TestDispose("127.0.0.1");

            //GC.Collect(GC.MaxGeneration);
            //GC.Collect(GC.MaxGeneration);

            //System.Threading.Thread.Sleep(5000);

            while (true)
            {
                try
                {
                    MutexExample("127.0.0.1");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    System.Threading.Thread.Sleep(10 * 1000);
                }
            }

            //TestPerformance("127.0.0.1");

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }
    }
}
