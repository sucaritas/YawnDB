// <copyright file="DatabaseIsClosedException.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Exceptions
{
    using System;

    [Serializable]
    public class DatabaseIsClosedException : Exception
    {
        public DatabaseIsClosedException(string message)
            : base(message)
        {
        }

        public DatabaseIsClosedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
