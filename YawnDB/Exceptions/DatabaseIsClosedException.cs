namespace YawnDB.Exceptions
{
    using System;

    public class DatabaseIsClosedException : Exception
    {
        public DatabaseIsClosedException(string message) : base(message)
        {

        }

        public DatabaseIsClosedException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
