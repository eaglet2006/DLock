using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DLock.Server;

namespace DLockService
{
    class DServer
    {
        static DLockServer _dlockServer;

        internal static void Run(int port)
        {
            _dlockServer = new DLockServer(port);

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }
    }
}
