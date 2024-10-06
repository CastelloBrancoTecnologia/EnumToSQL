using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CastelloBranco.EnumToSql;

public enum EnumToSqlBehavior
{
    UpdateDatabase = 0,
    GenertaExceptionIfMismach = 1,
}

