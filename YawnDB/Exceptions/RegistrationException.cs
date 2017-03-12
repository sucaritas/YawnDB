// <copyright file="RegistrationException.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class RegistrationException : InvalidOperationException
    {
        public RegistrationException(string message)
            : base(message)
        {
        }

        public RegistrationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
