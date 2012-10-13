using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            enum State
            {
                Init, //Initial state
                Idle, //no token
                Token,//keep token
                RequireToken, //server require token
            }

            readonly DLockProvider _Provider;

            private System.Threading.ManualResetEvent _WaitEvent = new System.Threading.ManualResetEvent(false);

            private object _LockObj = new object();
            private object _Entry = new object();

            private object _StateLock = new object();
            private State _CurState = State.Init;

            private State CurState
            {
                get
                {
                    lock (_StateLock)
                    {
                        return _CurState;
                    }
                }

                set
                {
                    lock (_StateLock)
                    {
                        _CurState = value;
                    }
                }
            }

            private LinkedList<Mutex> _ActivedMutexes = new LinkedList<Mutex>();
            
            internal int Handle { get; private set; }
            
            internal int Count 
            {
                get
                {
                    lock (_LockObj)
                    {
                        return _ActivedMutexes.Count;
                    }
                }
            }

            internal NamedMutexMgr(DLockProvider provider)
            {
                _Provider = provider;
            }


            internal void EventReceivedHandler(DLockEvent dEvent)
            {
                if (dEvent.Event == DLockEvent.DEvent.InitApplyToken)
                {
                    Handle = dEvent.Handle;

                    lock (_LockObj)
                    {
                        foreach (Mutex mutex in _ActivedMutexes)
                        {
                            mutex.Handle = Handle;
                        }
                    }
                }

                switch (dEvent.Event)
                {
                    case DLockEvent.DEvent.ApplyToken:
                    case DLockEvent.DEvent.InitApplyToken:
                        CurState = State.Token;
                        _WaitEvent.Set();
                        break;
                    case DLockEvent.DEvent.RequireToken:
                        CurState = State.RequireToken;
                        break;
                }
            }

            private void Add(Mutex mutex)
            {
                lock (_LockObj)
                {
                    _ActivedMutexes.AddLast(mutex);
                }
            }


            private void Remove(Mutex mutex)
            {
                lock (_LockObj)
                {
                    //Some mutex may entry more than once for same thread
                    //Has to remove from last.
                    LinkedListNode<Mutex> last = _ActivedMutexes.Last;

                    while (last != null)
                    {
                        if (last.Value == mutex)
                        {
                            _ActivedMutexes.Remove(last);
                            return;
                        }

                        last = last.Previous;
                    }
                }
            }

            internal void ASend(MutexEvent dEvent)
            {
                _Provider.ASend(dEvent);
            }

            internal void ReleaseMutex(Mutex mutex)
            {
                try
                {
                    Remove(mutex);

                    if (CurState == State.RequireToken)
                    {
                        CurState = State.Idle;
                        ASend(new MutexEvent(mutex.Name, DLockEvent.DEvent.ReturnToken, Handle));
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(_Entry);
                }
            }

            internal bool WaitOne(Mutex mutex, int millisecondsTimeout)
            {
                Stopwatch entrySW = new Stopwatch();
                entrySW.Start();

                try
                {
                    Add(mutex);

                    if (!System.Threading.Monitor.TryEnter(_Entry, millisecondsTimeout))
                    {
                        //can't entry.
                        Remove(mutex);
                        return false;
                    }

                    //only one thread can't entry here at same time
                    switch (CurState)
                    {
                        case State.Token:
                        case State.RequireToken:
                            return true;
                    }

                    MutexEvent mEvent = new MutexEvent();

                    switch (CurState)
                    {
                        case State.Init:
                            mEvent.Event = DLockEvent.DEvent.InitApplyToken;
                            break;

                        case State.RequireToken:
                            mEvent.Handle = Handle;
                            mEvent.Event = DLockEvent.DEvent.ApplyToken;
                            break;

                        default:
                            mEvent = null;
                            break;
                    }

                    if (mEvent != null)
                    {
                        mEvent.Name = mutex.Name;
                    }

                    ASend(mEvent);
                    _WaitEvent.Reset();

                    entrySW.Stop();

                    int remain = millisecondsTimeout - (int)entrySW.ElapsedMilliseconds;

                    if (remain <= 0)
                    {
                        if (millisecondsTimeout != System.Threading.Timeout.Infinite)
                        {
                            ReleaseMutex(mutex);
                            return false;
                        }
                        else
                        {
                            remain = System.Threading.Timeout.Infinite;
                        }
                    }

                    if (!_WaitEvent.WaitOne(remain))
                    {
                        ReleaseMutex(mutex);
                        return false;
                    }

                    return true;
                }
                finally
                {
                    entrySW.Stop();
                }
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

        internal void ReleaseMutex(Mutex mutex)
        {
            lock (_LockObj)
            {
                NamedMutexMgr namedMutexMgr;
                if (_NamedMutexMgrDict.TryGetValue(mutex.Name, out namedMutexMgr))
                {
                    namedMutexMgr.ReleaseMutex(mutex);

                    //no thread wait for this mutex, remove it from manager. 
                    if (namedMutexMgr.Count == 0)
                    {
                        _NamedMutexMgrDict.Remove(mutex.Name);
                    }
                }
                else
                {
                    throw new DLockException(string.Format("Mutex named:{0} doesn't exist", mutex.Name), DLockException.ErrorType.Mutex);
                }
            }
        }

        internal bool WaitOne(Mutex mutex, int millisecondsTimeout)
        {
            lock (_LockObj)
            {
                NamedMutexMgr namedMutexMgr;
                if (!_NamedMutexMgrDict.TryGetValue(mutex.Name, out namedMutexMgr))
                {
                    //No thread wait for, add to manager.
                    namedMutexMgr = new NamedMutexMgr(_Provider);
                    _NamedMutexMgrDict.Add(mutex.Name, namedMutexMgr);
                }

                return namedMutexMgr.WaitOne(mutex, millisecondsTimeout);
            }
        }


    }
}
