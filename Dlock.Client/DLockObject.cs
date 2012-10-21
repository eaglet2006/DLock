using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLock.Client
{
    /// <summary>
    /// DLockObject is the base object for all of the distributed objects
    /// </summary>
    public abstract class DLockObject
    {
        bool _Disposed = false;

        protected DLockEvent.GlobalEvent _GlobalEvent;

        protected DLockProvider Provider { get; private set; }

        /// <summary>
        /// Name of distributed object
        /// </summary>
        public string Name { get; private set; }

        public int Handle { get; internal set; }

        internal DLockEvent.GlobalEvent GlobalEvent 
        {
            get
            {
                return _GlobalEvent;
            }
        }

        internal DLockObject(DLockProvider provider, string name)
        {
            Provider = provider;
            Name = name;
        }

        ~DLockObject()
        {
            Console.WriteLine("DLock Disposed");
        }
    }
}
