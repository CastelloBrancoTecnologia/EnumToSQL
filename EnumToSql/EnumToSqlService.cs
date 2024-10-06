using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CastelloBranco.EnumToSql;

public static partial class EnumToSqlService
{
    [GeneratedRegex(@"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$")]
    private static partial Regex regexValidateTableName();

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex regexValidateFieldName();

    private static bool ValidateTableName(string tableName)
    {
        return regexValidateTableName().IsMatch(tableName);
    }

    private static bool ValidateFieldName(string fieldName)
    {
        return regexValidateFieldName().IsMatch(fieldName);
    }

    public static void UpdateAllEnums(string connectionString,
                                      DbProviderFactory? providerFactory = null,
                                      EnumToSqlBehavior behavior = EnumToSqlBehavior.UpdateDatabase)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type[] matchingEnums = assembly.GetTypes().Where(t => t.IsEnum && t.GetCustomAttributes(typeof(EnumToSqlAttribute), false).Length > 0).ToArray();

            foreach (Type type in matchingEnums)
            {
                UpdateEnumInDatabase(type, connectionString, providerFactory, behavior);
            }
        }
    }

    public static void UpdateEnumInDatabase(Type t,
                                            string connectionString,
                                            DbProviderFactory? providerFactory = null,
                                            EnumToSqlBehavior behavior = EnumToSqlBehavior.UpdateDatabase)
    {
        if (t != null)
        {
            EnumToSqlAttribute? attr = t.GetCustomAttribute<EnumToSqlAttribute>(true);

            if (attr != null && t.IsEnum)
            {
                providerFactory ??= SqliteFactory.Instance;

                using DbConnection? con = providerFactory.CreateConnection() ?? throw new EnumToSqlException ($"ProviderFactory {providerFactory} creatted a null conection object");

                con.ConnectionString = connectionString;
                con.Open();

                string[] EnumNames =
                    t.GetEnumNames()
                     .Where(x => t.GetMember(x).First().GetCustomAttribute<EnumToSqlIgnoreAttribute>() == null)
                     .ToArray();

                string[] nomes_existentes_no_banco = LeNomesNaEnumNoBanco(con, attr.TableName, attr.NameField);

                foreach (string nome in nomes_existentes_no_banco)
                {
                    if (!string.IsNullOrEmpty(nome) && !EnumNames.Any(x => x == nome))
                    {
                        if (behavior == EnumToSqlBehavior.GenertaExceptionIfMismach)
                        {
                            throw new EnumToSqlException ($"Exception {t.Name} on Table {attr.TableName} has an unknown valuename {nome}.");
                        }
                        else if (behavior == EnumToSqlBehavior.UpdateDatabase)
                        {
                            ExcluiNomeDaEnumNoBanco(con, attr.TableName, attr.NameField, nome);
                        }
                    }
                }

                foreach (string name in t.GetEnumNames())
                {
                    if (t.GetMember(name).First().GetCustomAttribute<EnumToSqlIgnoreAttribute>() == null)
                    {
                        int value = (int)Enum.Parse(t, name);

                        if (SeNomeNaoExisteNaEnumNoBanco(con, attr.TableName, attr.NameField, name))
                        {
                            if (behavior == EnumToSqlBehavior.GenertaExceptionIfMismach)
                            {
                                throw new EnumToSqlException ($"Exception {t.Name} on Table {attr.TableName} doesn't have the valuename {name}.");
                            }
                            else if (behavior == EnumToSqlBehavior.UpdateDatabase)
                            {
                                InsereValorNaEnumNoBanco(con, attr.TableName, attr.NameField, attr.ValueField, name, value);
                            }
                        }
                        else
                        {
                            int? ValorNoBanco = LeValorNaEnumNoBanco(con, attr.TableName, attr.NameField, attr.ValueField, name);

                            if (value != ValorNoBanco)
                            {
                                if (behavior == EnumToSqlBehavior.GenertaExceptionIfMismach)
                                {
                                    throw new EnumToSqlException ($"Exception {t.Name} on Table {attr.TableName} have an valuename {name} equals to {ValorNoBanco} and was expected to be {value}.");
                                }
                                else if (behavior == EnumToSqlBehavior.UpdateDatabase)
                                {
                                    AtualizaValorNaEnumNoBanco(con, attr.TableName, attr.NameField, attr.ValueField, name, value);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static string[] LeNomesNaEnumNoBanco(DbConnection con, string tableName, string nameField)
    {
        if (! ValidateTableName(tableName))
        {
            throw new EnumToSqlException ($"Invalid Table name {tableName} !");
        }

        if (!ValidateFieldName(nameField))
        {
            throw new EnumToSqlException ($"Invalid Field name {nameField} !");
        }

        List<string> names = [];

        using (DbCommand cmdLeNome = con.CreateCommand())
        {
#pragma warning disable CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()
            cmdLeNome.CommandText = $"select {nameField} from {tableName};";
#pragma warning restore CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()

            using DbDataReader reader = cmdLeNome.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    names.Add(reader[$"{nameField}"].ToString() ?? "");
                }
            }
        }

        return [.. names];
    }

    private static int? LeValorNaEnumNoBanco(DbConnection con, string tableName, string nameField, string valueField, string name)
    {
        if (!ValidateTableName(tableName))
        {
            throw new EnumToSqlException ($"Invalid Table name {tableName} !");
        }

        if (!ValidateFieldName(nameField))
        {
            throw new EnumToSqlException ($"Invalid Field name {nameField} !");
        }

        if (!ValidateFieldName(valueField))
        {
            throw new EnumToSqlException ($"Invalid Field name {valueField} !");
        }

        using DbCommand cmdLevalor = con.CreateCommand();

#pragma warning disable CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()
        cmdLevalor.CommandText = $"select {valueField} from {tableName} where {nameField} == @{nameField};";
#pragma warning restore CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()

        DbParameter paramAtualizaEnumName = cmdLevalor.CreateParameter();
        paramAtualizaEnumName.ParameterName = $"@{nameField}";
        paramAtualizaEnumName.DbType = System.Data.DbType.String;
        cmdLevalor.Parameters.Add(paramAtualizaEnumName);

        cmdLevalor.Parameters[$"@{nameField}"].Value = name;

        using DbDataReader reader = cmdLevalor.ExecuteReader();

        if (reader.HasRows)
        {
            if (reader.Read())
            {
                object? o = reader[$"{valueField}"];

                return o == null ? null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static void AtualizaValorNaEnumNoBanco(DbConnection con, string tableName, string nameField, string valueField, string name, int value)
    {
        if (!ValidateTableName(tableName))
        {
            throw new EnumToSqlException($"Invalid Table name {tableName} !");
        }

        if (!ValidateFieldName(nameField))
        {
            throw new EnumToSqlException($"Invalid Field name {nameField} !");
        }

        if (!ValidateFieldName(valueField))
        {
            throw new EnumToSqlException ($"Invalid Field name {valueField} !");
        }

        using DbCommand cmdAtualizaEnum = con.CreateCommand();

#pragma warning disable CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()
        cmdAtualizaEnum.CommandText =
            $"update {tableName} set {valueField} = @{valueField} where {nameField} == @{nameField};";
#pragma warning restore CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()

        DbParameter paramAtualizaEnumName = cmdAtualizaEnum.CreateParameter();
        paramAtualizaEnumName.ParameterName = $"@{nameField}";
        paramAtualizaEnumName.DbType = System.Data.DbType.String;
        cmdAtualizaEnum.Parameters.Add(paramAtualizaEnumName);

        DbParameter paramAtualizaEnumValue = cmdAtualizaEnum.CreateParameter();
        paramAtualizaEnumValue.ParameterName = $"@{valueField}";
        paramAtualizaEnumValue.DbType = System.Data.DbType.Int32;
        cmdAtualizaEnum.Parameters.Add(paramAtualizaEnumValue);

        cmdAtualizaEnum.Parameters[$"@{nameField}"].Value = name;
        cmdAtualizaEnum.Parameters[$"@{valueField}"].Value = value;

        cmdAtualizaEnum.ExecuteNonQuery();
    }

    private static void InsereValorNaEnumNoBanco(DbConnection con, string tableName, string nameField, string valueField, string name, int value)
    {
        if (!ValidateTableName(tableName))
        {
            throw new EnumToSqlException ($"Invalid Table name {tableName} !");
        }

        if (!ValidateFieldName(nameField))
        {
            throw new EnumToSqlException ($"Invalid Field name {nameField} !");
        }

        if (!ValidateFieldName(valueField))
        {
            throw new EnumToSqlException ($"Invalid Field name {valueField} !");
        }

        using DbCommand cmdInsereEnum = con.CreateCommand();

#pragma warning disable CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()
        cmdInsereEnum.CommandText =
            $"insert into {tableName} ({nameField}, {valueField}) values (@{nameField}, @{valueField});";
#pragma warning restore CA2100 // string hardenizada via ValidateFieldName () e ValidateTableName()

        DbParameter paramInsereEnumName = cmdInsereEnum.CreateParameter();
        paramInsereEnumName.ParameterName = $"@{nameField}";
        paramInsereEnumName.DbType = System.Data.DbType.String;
        cmdInsereEnum.Parameters.Add(paramInsereEnumName);

        DbParameter paramInsereEnumValue = cmdInsereEnum.CreateParameter();
        paramInsereEnumValue.ParameterName = $"@{valueField}";
        paramInsereEnumValue.DbType = System.Data.DbType.Int32;
        cmdInsereEnum.Parameters.Add(paramInsereEnumValue);

        cmdInsereEnum.Parameters[$"@{nameField}"].Value = name;
        cmdInsereEnum.Parameters[$"@{valueField}"].Value = value;

        cmdInsereEnum.ExecuteNonQuery();
    }

    private static void ExcluiNomeDaEnumNoBanco(DbConnection con, string tableName, string nameField, string nome)
    {
        if (!ValidateTableName(tableName))
        {
            throw new EnumToSqlException($"Invalid Table name {tableName} !");
        }

        if (!ValidateFieldName(nameField))
        {
            throw new EnumToSqlException($"Invalid Field name {nameField} !");
        }

        using DbCommand cmdExcluiEnum = con.CreateCommand();

#pragma warning disable CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()
        cmdExcluiEnum.CommandText = $"delete from {tableName} where {nameField} == @{nameField};";
#pragma warning restore CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()

        DbParameter paramExcluiEnumName = cmdExcluiEnum.CreateParameter();
        paramExcluiEnumName.ParameterName = $"@{nameField}";
        paramExcluiEnumName.DbType = System.Data.DbType.String;
        cmdExcluiEnum.Parameters.Add(paramExcluiEnumName);

        cmdExcluiEnum.Parameters[$"@{nameField}"].Value = nome;

        cmdExcluiEnum.ExecuteNonQuery();
    }

    private static bool SeNomeNaoExisteNaEnumNoBanco(DbConnection con, string tableName, string nameField, string name)
    {
        if (!ValidateTableName(tableName))
        {
            throw new EnumToSqlException($"Invalid Table name {tableName} !");
        }

        if (!ValidateFieldName(nameField))
        {
            throw new EnumToSqlException($"Invalid Field name {nameField} !");
        }

        bool bRet;

        using (DbCommand cmdNomeExiste = con.CreateCommand())
        {
#pragma warning disable CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()
            cmdNomeExiste.CommandText =
                  $"select count (*) from {tableName} "
                + $"                where {nameField}  = @{nameField}";
#pragma warning restore CA2100 //  string hardenizada via ValidateFieldName () e ValidateTableName()

            DbParameter paramNomeExiste = cmdNomeExiste.CreateParameter();
            paramNomeExiste.ParameterName = $"@{nameField}";
            paramNomeExiste.DbType = System.Data.DbType.String;
            cmdNomeExiste.Parameters.Add(paramNomeExiste);

            cmdNomeExiste.Parameters[$"@{nameField}"].Value = name;

            bRet = (((long)(cmdNomeExiste.ExecuteScalar() ?? 0)) < 1);
        }

        return bRet;
    }
}
