using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DLock.Client
{
    public class DLockPool
    {
        static object _LockObj = new object();

        static Dictionary<IPEndPoint, DLockProvider> _DLockProviderPool = new Dictionary<IPEndPoint, DLockProvider>();

        public static DLockProvider GetDLockProvider(string ipString)
        {
            return GetDLockProvider(ipString, Constants.DefaultPort);
        }

        public static DLockProvider GetDLockProvider(string ipString, int port)
        {
            return GetDLockProvider(new IPEndPoint(IPAddress.Parse(ipString), port));
        }


        public static DLockProvider GetDLockProvider(IPEndPoint ipEndPoint)
        {
            lock (_LockObj)
            {
                DLockProvider result;

                if (!_DLockProviderPool.TryGetValue(ipEndPoint, out result))
                {
                    result = new DLockProvider(ipEndPoint);
                }

                return result;
            }
        }
    }
}
