using CastelloBranco.EnumToSql;
using System.Runtime.CompilerServices;

namespace CastelloBranco.EnumToSqlTest;

public class Program
{
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

    private static string? GetMyPath([CallerFilePath] string? from = null)
    {
        return Path.GetDirectoryName(from);
    }

    static void Main(string[] _)
    {
        string database = @$"{GetMyPath()}\EnumDeTeste.db";

        Console.WriteLine($"Using Sqlite database: {database}.");

        // At startup, call EnumToSqlService.UpdateAllEnums() to update all enums in database
        // from any enum in code decorated with [EnumToSqlAttribute] in any assembly that your project use
        //
        // default provider => SqliteFactory.Instance
        // default behavior => EnumToSqlBehavior.UpdateDatabase;

        EnumToSqlService.UpdateAllEnums(@$"Data Source={database}");
    }
}
