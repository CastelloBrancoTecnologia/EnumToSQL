using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CastelloBranco.EnumToSql;

[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
public sealed class EnumToSqlAttribute : Attribute
{
    public string TableName { get; set; } = string.Empty;
    public string NameField { get; set; } = string.Empty;
    public string ValueField { get; set; } = string.Empty;
}

