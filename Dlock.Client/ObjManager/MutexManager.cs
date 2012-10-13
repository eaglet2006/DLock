using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLock.Client.ObjManager
{
    class MutexManager
    {
        private object _LockObj = new object();
        Dictionary<string, NamedMutexMgr> _NamedMutexMgrDict = new Dictionary<string, NamedMutexMgr>();
        readonly DLockProvider _Provider;

        class NamedMutexMgr
        {
            readonly DLockProvider _Provider;
            private object _LockObj = new object();
            private LinkedList<Mutex> _ActivedMutexes = new LinkedList<Mutex>();
            
            internal int Handle { get; private set; }

            internal NamedMutexMgr(DLockProvider provider)
            {
                _Provider = provider;
            }

            internal void EventReceivedHandler(DLockEvent dEvent)
            {
                if (dEvent.Event == DLockEvent.DEvent.InitApplyToken)
                {
                    Handle = dEvent.Handle;
                }

                lock (_LockObj)
                {
                    foreach (Mutex mutex in _ActivedMutexes)
                    {
                        mutex.Handle = Handle;
                    }
                }
            }

            internal void ASend(DLockEvent dEvent)
            {
                _Provider.ASend(dEvent);
            }
        }

        internal MutexManager(DLockProvider provider)
        {
            _Provider = provider;
        }

        internal void EventReceivedHandler(DLockEvent dEvent)
        {
            lock (_LockObj)
            {
                NamedMutexMgr namedMutexMgr;
                if (_NamedMutexMgrDict.TryGetValue(dEvent.Name, out namedMutexMgr))
                {
                    namedMutexMgr.EventReceivedHandler(dEvent);
                }
            }
        }

    }
}
