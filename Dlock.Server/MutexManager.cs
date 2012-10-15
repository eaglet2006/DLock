using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Net;

using NTCPMSG.Server;

namespace DLock.Server
{
    class MutexManager
    {
        class MutexHanlde
        {
            internal IPAddress IP { get; private set; }
            internal UInt16 CableId { get; private set; }
            internal int Handle { get; private set; }
            internal bool KeepToken { get; set; }
            internal DLockEvent.DEvent LastEvent { get; set; }
            internal bool ApplyingToken { get; set; }

            internal MutexHanlde(IPAddress ipAddress, UInt16 cableId, int handle, DLockEvent.DEvent lastEvent)
            {
                IP = ipAddress;
                CableId = cableId;
                Handle = handle;
                KeepToken = false;
                ApplyingToken = false;
                LastEvent = lastEvent;
            }

            internal void SetKeepToken(bool value)
            {
                KeepToken = value;

                ApplyingToken = false;
            }

            public override bool Equals(object obj)
            {
                MutexHanlde other = obj as MutexHanlde;
                if (other == null)
                {
                    return false;
                }

                return this.Handle == other.Handle;
            }

            public override int GetHashCode()
            {
                return Handle.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("IP:{0} CableId:{1} Handle:{2} KeepToken:{3}", 
                    IP, CableId, Handle, KeepToken);
            }
        }

        class MutexTokenScheduling
        {
            //Don't use Queue<T> is because we need remove item inside the queue when client exits.
            LinkedList<MutexHanlde> _Queue = new LinkedList<MutexHanlde>();
            readonly NTcpListener _Listener;
            object _LockObj = new object();

            /// <summary>
            /// Get mutex name
            /// </summary>
            internal string Name { get; private set; }

            internal MutexTokenScheduling(string name, NTcpListener listener)
            {
                Name = name;
                _Listener = listener;
            }

            internal void ApplyToken(MutexEvent dEvent, IPAddress ipAddress, UInt16 cableId)
            {
                lock (_LockObj)
                {
                    if (_Queue.FirstOrDefault(mToken => mToken.Handle == dEvent.Handle) == null)
                    {
                        //not exist, add a new MutexToken
                        _Queue.AddLast(new MutexHanlde(ipAddress, cableId, dEvent.Handle, dEvent.Event));
                    }

                    if ((!_Queue.First.Value.KeepToken && _Queue.First.Value.Handle == dEvent.Handle) ||
                        _Queue.Count == 1)
                    {
                        //only one mutex client or first mutex handle is for the appling client and doesn't keep token. 
                        //Send apply token immidiately.
                        if (_Listener.AsyncSend(_Queue.First.Value.CableId, (uint)DLockEvent.GlobalEvent.MutexEvent,
                            new MutexEvent(Name, dEvent.Event, _Queue.First.Value.Handle).GetBytes()))
                        {
                            _Queue.First.Value.SetKeepToken(true);
                        }
                    }
                    else
                    {
                        MutexHanlde mutexHanlde = _Queue.First(s => s.Handle == dEvent.Handle);

                        if (!_Queue.First.Value.KeepToken)
                        {
                            //first mutex handle does not keep the token
                            //only happen when first mutex handle send apply token event and disconnect imidiately  
                           
                            _Queue.Remove(mutexHanlde);
                            _Queue.AddFirst(mutexHanlde);

                            if (_Listener.AsyncSend(mutexHanlde.CableId, (uint)DLockEvent.GlobalEvent.MutexEvent,
                                new MutexEvent(Name, dEvent.Event, mutexHanlde.Handle).GetBytes()))
                            {
                                mutexHanlde.SetKeepToken(true);
                            }
                        }
                        else
                        {
                            if (_Queue.First.Value.Handle != dEvent.Handle)
                            {
                                //Other client is keeping token now
                                //Need require the token from that client and give this client token
                                mutexHanlde.ApplyingToken = true;
                                mutexHanlde.LastEvent = dEvent.Event;

                                if (_Listener.AsyncSend(_Queue.First.Value.CableId, (uint)DLockEvent.GlobalEvent.MutexEvent,
                                    new MutexEvent(Name, DLockEvent.DEvent.RequireToken, _Queue.First.Value.Handle).GetBytes()))
                                {
                                    //_Queue.First.Value.SetKeepToken(true);
                                }
                            }
                            else
                            {
                                if (_Listener.AsyncSend(_Queue.First.Value.CableId, (uint)DLockEvent.GlobalEvent.MutexEvent,
                                    new MutexEvent(Name, dEvent.Event, _Queue.First.Value.Handle).GetBytes()))
                                {
                                    //_Queue.First.Value.SetKeepToken(true);
                                }
                            }
                        }
                    }
                }
            }

            internal void ReturnToken(MutexEvent dEvent, IPAddress ipAddress, UInt16 cableId)
            {
                lock (_LockObj)
                {
                    MutexHanlde mutexHandle = _Queue.FirstOrDefault(s => s.Handle == dEvent.Handle);

                    if (mutexHandle != null)
                    {
                        //Move current mutex handle to the last of the queue.
                        mutexHandle.SetKeepToken(false);
                        _Queue.Remove(mutexHandle);
                        _Queue.AddLast(mutexHandle);
                    }

                    MutexHanlde applyingHandle = _Queue.FirstOrDefault(s => s.ApplyingToken);

                    if (applyingHandle != null)
                    {
                        if (applyingHandle != _Queue.First.Value)
                        {
                            _Queue.Remove(applyingHandle);
                            _Queue.AddFirst(applyingHandle);
                        }

                        if (_Listener.AsyncSend(_Queue.First.Value.CableId, (uint)DLockEvent.GlobalEvent.MutexEvent,
                             new MutexEvent(Name, applyingHandle.LastEvent, applyingHandle.Handle).GetBytes()))
                        {
                            applyingHandle.SetKeepToken(true);
                        }
                    }

                }
            }

            internal void Exit(MutexEvent dEvent, IPAddress ipAddress, UInt16 cableId)
            {

            }
        }

        object _TokenLock = new object();
        int _LastAllocatedToken = 1;
        HashSet<int> _TokenPool = new HashSet<int>();
        ConcurrentDictionary<string, MutexTokenScheduling> _NameToQueue = new ConcurrentDictionary<string, MutexTokenScheduling>();
        readonly NTcpListener _Listener;

        int AllocToken()
        {
            lock (_TokenLock)
            {
                while (_TokenPool.Contains(_LastAllocatedToken))
                {
                    if (++_LastAllocatedToken == int.MaxValue)
                    {
                        _LastAllocatedToken = 1;
                    }
                }

                _TokenPool.Add(_LastAllocatedToken);

                return _LastAllocatedToken;
            }
        }

        void ReturnToken(int token)
        {
            lock (_TokenLock)
            {
                _TokenPool.Remove(token);
            }
        }

        /// <summary>
        /// Receive message from mutex client
        /// </summary>
        /// <param name="dEvent">event</param>
        /// <param name="ipAddress">client ip address</param>
        /// <param name="cableId">client cableid</param>
        internal void ReceiveEventHandler(MutexEvent dEvent, IPAddress ipAddress, UInt16 cableId)
        {
            int handle = dEvent.Handle;

            MutexTokenScheduling schedule;

            schedule = _NameToQueue.GetOrAdd(dEvent.Name, (key) => new MutexTokenScheduling(dEvent.Name, _Listener));

            switch (dEvent.Event)
            {
                case DLockEvent.DEvent.InitApplyToken:
                    dEvent.Handle = AllocToken();
                    schedule.ApplyToken(dEvent, ipAddress, cableId);
                    break;
                case DLockEvent.DEvent.ApplyToken:
                    schedule.ApplyToken(dEvent, ipAddress, cableId);
                    break;
                case DLockEvent.DEvent.ReturnToken:
                    schedule.ReturnToken(dEvent, ipAddress, cableId);
                    break;
                case DLockEvent.DEvent.Exit:
                    schedule.Exit(dEvent, ipAddress, cableId);
                    ReturnToken(dEvent.Handle);
                    break;
            }
        }

        internal MutexManager(NTcpListener listener)
        {
            _Listener = listener;
        }
    }
}
