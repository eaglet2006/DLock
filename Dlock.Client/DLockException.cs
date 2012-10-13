using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLock.Client
{
    public class DLockException : Exception
    {
        public enum ErrorType
        {
            Successful = 0,
            Disconnect = 1,
        }

        public ErrorType Error { get; private set; }

        public DLockException(string message, ErrorType errorType)
            : base(message)
        {
            Error = errorType;
        }
    }
}
