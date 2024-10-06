using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CastelloBranco.EnumToSql;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class EnumToSqlIgnoreAttribute : Attribute
{
}


