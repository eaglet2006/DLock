using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLock.Client
{
    abstract class MessagePool
    {
        class Message : IComparable<Message>
        {
            internal DateTime ActiveTime {get; set;}
            internal string Name {get; private set;}
            internal DLockEvent.DEvent Event {get; private set;}
            internal object State {get; private set;}

            internal Message(string name, DLockEvent.DEvent evt, object state, int delayMilliseconds)
            {
                if (name == null)
                {
                    throw new ArgumentException("Name can't be null");
                }

                Name = name;
                Event = evt;
                State = state;
                ActiveTime = DateTime.Now.AddMilliseconds(delayMilliseconds);
            }

            public override bool Equals(object obj)
            {
                Message other = (Message)obj;
                if (other == null)
                {
                    return false;
                }

                return this.Name == other.Name;
            }

            public override int GetHashCode()
            {
                return this.Name.GetHashCode();
            }

            #region IComparable<Message> Members

            public int CompareTo(Message other)
            {
                return this.ActiveTime.CompareTo(other.ActiveTime);
            }

            #endregion
        }

        List<Message> _MessageList = new List<Message>();
        object _LockObj = new object();

        System.Threading.Thread _Thread;

        internal MessagePool()
        {
            _Thread = new System.Threading.Thread(ThreadProc);
            _Thread.IsBackground = true;
            _Thread.Start();
        }

        ~MessagePool()
        {
            try
            {
                if (_Thread != null)
                {
                    _Thread.Abort();
                }
            }
            catch
            {
            }
            finally
            {
                _Thread = null;
            }
        }

        private void ThreadProc()
        {
            List<Message> procMessages = new List<Message>();
            
            while (true)
            {
                procMessages.Clear();

                lock (_LockObj)
                {
                    DateTime now = DateTime.Now;
                    
                    _MessageList.Sort();

                    foreach (Message message in _MessageList)
                    {
                        if (now >= message.ActiveTime)
                        {
                            procMessages.Add(message);
                        }
                    }
                }

                if (procMessages.Count > 0)
                {
                    foreach (Message message in procMessages)
                    {
                        try
                        {
                            ProcessMessage(message.Name, message.Event, message.State);
                        }
                        catch
                        {
                        }
                    }

                    lock (_LockObj)
                    {
                        foreach (Message message in procMessages)
                        {
                            _MessageList.Remove(message);
                        }
                    }
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        protected abstract void ProcessMessage(string name, DLockEvent.DEvent evt, object state);

        protected void SendDelayMessage(string name, DLockEvent.DEvent evt, int delayMilliseconds)
        {
            SendDelayMessage(name, evt, null, delayMilliseconds);
        }

        protected void SendDelayMessage(string name, DLockEvent.DEvent evt, object state, int delayMilliseconds)
        {
            Message message = new Message(name, evt, state, delayMilliseconds);

            lock (_LockObj)
            {
                foreach (Message msg in _MessageList)
                {
                    if (msg.Equals(message))
                    {
                        if (message.ActiveTime > msg.ActiveTime)
                        {
                            msg.ActiveTime = message.ActiveTime;
                        }
                        
                        return;
                    }
                }

                _MessageList.Add(message);
            }


        }
    }
}
