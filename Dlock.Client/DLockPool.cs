using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DLock.Client
{
    class DLockPool
    {
        static object _LockObj = new object();

        static Dictionary<IPEndPoint, DLockProvider> _DLockProviderPool = new Dictionary<IPEndPoint, DLockProvider>();

        static DLockProvider GetDLockProvider(IPEndPoint ipEndPoint)
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
