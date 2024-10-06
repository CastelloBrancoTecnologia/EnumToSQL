using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastelloBranco.EnumToSql
{
    public class EnumToSqlException : Exception
    {        public EnumToSqlException()
        {
        }

        public EnumToSqlException(string message) : base(message)
        {
        }

        public EnumToSqlException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
