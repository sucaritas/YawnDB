using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YawnDB.Exceptions
{
    class RegistrationException : InvalidOperationException
    {
        public RegistrationException(string message) : base(message)
        {

        }

        public RegistrationException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
