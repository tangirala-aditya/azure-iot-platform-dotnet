using System;

namespace ResourceDataCollector.Shared
{
    public class ConflictingResourceException : Exception
    {
        public ConflictingResourceException()
            : base()
        {
        }

        public ConflictingResourceException(string message)
            : base(message)
        {
        }

        public ConflictingResourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
