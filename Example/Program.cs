﻿using System;
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

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            for (int i = 0; i < 1000000; i++)
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

                    if ((i % 10000) == 0)
                    {
                        System.Threading.Thread.Sleep(1000);

                        sw.Stop();
                        sw.Reset();
                        Console.WriteLine(sw.ElapsedMilliseconds);

                    }

                    //Console.WriteLine("Leave");
                }
            }

            sw.Stop();
            
            Console.WriteLine(sw.ElapsedMilliseconds);

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }
    }
}
