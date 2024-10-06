# CBT.EnumToSql

EnumToSql is a simple library that automatically synchronizes enums in your .NET application with a relational database, particularly SQLite. This is useful when you need to ensure that your database always reflects the values defined in your enums without manual updates.

Features

Automatically creates or updates database tables for enums.

Supports exclusion of specific enum members using [EnumToSqlIgnore].

Provides configurable behavior on mismatches between enum and database values (throw exception or update database).

Default support for SQLite via SqliteFactory, but other databases are supported by passing the appropriate DbProviderFactory.

Installation

Simply reference the library in your .NET project and make sure you have a SQLite or any database available in .net.

Example Usage:

     [EnumToSqlAttribute (TableName  = "EnumDeTeste", NameField  = "Sigla", ValueField = "Valor")]   
     public enum EnumDeTeste
     {
            Um = 1,
            Tres = 3,
            Quatro = 4,
            [EnumToSqlIgnore]
            Cinco = 5,            
            Seis = 6,
     }

    // At startup, call EnumToSqlService.UpdateAllEnums() to update all enums in database

    public void Startup ()
    {
       EnumToSqlService.UpdateAllEnums(@"Data Source=C:\databases\EnumToSqlTest\EnumDeTeste.db");
    }
