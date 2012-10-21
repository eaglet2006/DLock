using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLock.Client
{
    public class Mutex : DLockObject, IDisposable
    {
        bool _Disposed = false;

        object _LockObj = new object();

        bool _HoldMutex = false;

        bool HoldMutex
        {
            get
            {
                lock (_LockObj)
                {
                    return _HoldMutex; 
                }
            }

            set
            {
                lock (_LockObj)
                {
                    _HoldMutex = value;
                }
            }
        }

        private bool _Suspected = false;

        /// <summary>
        /// Got mutex but it is suspected.
        /// Suspected means some other mutex in other client keep the mutex token and disconnected.
        /// </summary>
        public bool Suspected
        {
            get
            {
                lock (_LockObj)
                {
                    return _Suspected;
                }
            }

            set
            {
                lock (_LockObj)
                {
                    _Suspected = value;
                }
            }
        }

        internal Mutex(DLockProvider provider, string name)
            :base(provider, name)
        {
            _GlobalEvent = DLockEvent.GlobalEvent.MutexEvent;

            //System.Threading.Mutex mutex = new System.Threading.Mutex();
            //mutex.ReleaseMutex

        }

        ~Mutex()
        {
            Console.WriteLine("Mutex disposed");
            Dispose();
        }

        #region Mutex public  Methods

        //
        // Summary:
        //     Releases the System.Threading.Mutex once.
        //
        // Exceptions:
        //   System.ApplicationException:
        //     The calling thread does not own the mutex.
        public void ReleaseMutex()
        {
            Provider.MutexMgr.ReleaseMutex(this);
            HoldMutex = false;
        }

        //
        // Summary:
        //     Blocks the current thread until the current System.Threading.WaitHandle receives
        //     a signal.
        //
        // Returns:
        //     true if the current instance receives a signal. If the current instance is
        //     never signaled, System.Threading.WaitHandle.WaitOne(System.Int32,System.Boolean)
        //     never returns.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The current instance has already been disposed.
        //
        //   System.Threading.AbandonedMutexException:
        //     The wait completed because a thread exited without releasing a mutex. This
        //     exception is not thrown on Windows 98 or Windows Millennium Edition.
        //
        //   System.InvalidOperationException:
        //     The current instance is a transparent proxy for a System.Threading.WaitHandle
        //     in another application domain.
        public bool WaitOne()
        {
            return WaitOne(System.Threading.Timeout.Infinite);
        }

        //
        // Summary:
        //     Blocks the current thread until the current instance receives a signal, using
        //     a System.TimeSpan to measure the time interval.
        //
        // Parameters:
        //   timeout:
        //     A System.TimeSpan that represents the number of milliseconds to wait, or
        //     a System.TimeSpan that represents -1 milliseconds to wait indefinitely.
        //
        // Returns:
        //     true if the current instance receives a signal; otherwise, false.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The current instance has already been disposed.
        //
        //   System.ArgumentOutOfRangeException:
        //     timeout is a negative number other than -1 milliseconds, which represents
        //     an infinite time-out.-or-timeout is greater than System.Int32.MaxValue.
        //
        //   System.Threading.AbandonedMutexException:
        //     The wait completed because a thread exited without releasing a mutex. This
        //     exception is not thrown on Windows 98 or Windows Millennium Edition.
        //
        //   System.InvalidOperationException:
        //     The current instance is a transparent proxy for a System.Threading.WaitHandle
        //     in another application domain.
        public virtual bool WaitOne(TimeSpan timeout)
        {
            return WaitOne((int)timeout.TotalMilliseconds);
        }

        //
        // Summary:
        //     Blocks the current thread until the current System.Threading.WaitHandle receives
        //     a signal, using a 32-bit signed integer to measure the time interval.
        //
        // Parameters:
        //   millisecondsTimeout:
        //     The number of milliseconds to wait, or System.Threading.Timeout.Infinite
        //     (-1) to wait indefinitely.
        //
        // Returns:
        //     true if the current instance receives a signal; otherwise, false.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The current instance has already been disposed.
        //
        //   System.ArgumentOutOfRangeException:
        //     millisecondsTimeout is a negative number other than -1, which represents
        //     an infinite time-out.
        //
        //   System.Threading.AbandonedMutexException:
        //     The wait completed because a thread exited without releasing a mutex. This
        //     exception is not thrown on Windows 98 or Windows Millennium Edition.
        //
        //   System.InvalidOperationException:
        //     The current instance is a transparent proxy for a System.Threading.WaitHandle
        //     in another application domain.
        public bool WaitOne(int millisecondsTimeout)
        {
            bool suspected;
            bool ret = Provider.MutexMgr.WaitOne(this, millisecondsTimeout, out suspected);

            if (ret)
            {
                HoldMutex = true;
            }

            return ret;
        }


        #endregion
        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                if (!_Disposed)
                {
                    if (HoldMutex)
                    {
                        ReleaseMutex();
                    }
                    //Dispose code
                }
            }
            catch
            {
            }
            finally
            {
                _Disposed = true;
            }
        }

        #endregion
    }
}
