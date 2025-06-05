using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Exception thrown when search operations fail due to type mismatches
    /// </summary>
    public class InvalidSearchException : Exception
    {
        public InvalidSearchException(string message) : base(message)
        {
        }

        public InvalidSearchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
