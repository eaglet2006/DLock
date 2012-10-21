using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DLock.Server;

namespace DLServer
{
    class DServer
    {
        static DLock.Server.DLockServer _dlockServer;

        internal static void Run(int port)
        {
            _dlockServer = new DLock.Server.DLockServer(port);

            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }
    }
}
