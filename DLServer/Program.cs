using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLockServer
{
    class Program
    {
        static void Main(string[] args)
        {
            DLServer.DServer.Run(Constants.DefaultPort);
        }
    }
}
