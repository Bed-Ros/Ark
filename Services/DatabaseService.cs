using Ark.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Ark.Services
{
    public class FileFilter
    {
        public string? Text { get; set; }
        public bool SearchInText { get; set; }
        public long? SourceId { get; set; }

    }

    public class UpsertResult
    {
        public long Id { get; set; }
        public string Action { get; set; } = null!;
    }

    public static class DatabaseService
    {
        private async static Task TransactionDecorator(Func<SqlConnection, SqlTransaction, Task> act)
        {
            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                await act(connection, transaction);
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        private async static Task<T> TransactionDecorator<T>(Func<SqlConnection, SqlTransaction, Task<T>> act)
        {
            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var result = await act(connection, transaction);
                transaction.Commit();
                return result;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        #region Источники

        //Создание одного источника
        public static async Task<long> Create(Source source)
        {
            return await TransactionDecorator(async (connection, transaction) =>
                await Create(source, connection, transaction));
        }
        private static async Task<long> Create(Source source, SqlConnection connection, SqlTransaction transaction)
        {
            var tableName = Table<Source>();
            var sql =
                $@"MERGE INTO {tableName} AS T
                USING (VALUES (@{nameof(Source.MachineName)}, @{nameof(Source.RootPath)})) AS S 
                    ({Column<Source>(nameof(Source.MachineName))}, {Column<Source>(nameof(Source.RootPath))})
                    ON (T.{Column<Source>(nameof(Source.MachineName))} = S.{Column<Source>(nameof(Source.MachineName))} OR
                        T.{Column<Source>(nameof(Source.MachineName))} is NULL AND S.{Column<Source>(nameof(Source.MachineName))} is NULL) AND
                        T.{Column<Source>(nameof(Source.RootPath))} = S.{Column<Source>(nameof(Source.RootPath))}
                WHEN MATCHED THEN
	                UPDATE SET T.{Column<Source>(nameof(Source.RootPath))} = S.{Column<Source>(nameof(Source.RootPath))},
                        T.{Column<Source>(nameof(Source.MachineName))} = S.{Column<Source>(nameof(Source.MachineName))}
                WHEN NOT MATCHED THEN
                    INSERT ({Column<Source>(nameof(Source.MachineName))}, {Column<Source>(nameof(Source.RootPath))})
                    VALUES (S.{Column<Source>(nameof(Source.MachineName))}, S.{Column<Source>(nameof(Source.RootPath))})

                output inserted.{Column<Source>(nameof(Source.Id))} as {nameof(UpsertResult.Id)}, $action as {nameof(UpsertResult.Action)};";

            //Источник
            var upsertResult = await connection.QueryFirstAsync<UpsertResult>(sql, source, transaction);
            source.Id = upsertResult.Id;
            //Аудит(только при условии insert, так как update ничего не меняет)
            if (upsertResult.Action.Equals(nameof(AuditState.Insert), StringComparison.InvariantCultureIgnoreCase))
            {
                await Create(connection, transaction,
                    new Audit()
                    {
                        Keys = JsonConvert.SerializeObject(Keys(source)),
                        State = upsertResult.Action,
                        TableName = tableName,
                        NewValues = JsonConvert.SerializeObject(source),
                    });
            }
            return source.Id;
        }

        //Запрос всех источников
        public static async Task<IEnumerable<Source>> GetAllSources()
        {
            var sql = $@"Select * From {Table<Source>()}";
            using SqlConnection connection = new(Properties.Settings.Default.ConnectionString);
            connection.Open();
            return await connection.QueryAsync<Source>(sql);
        }

        #endregion

        #region Файлы

        //Создание одного файла
        public static async Task<UpsertResult> Create(DbFile file)
        {
            return await TransactionDecorator(async (connection, transaction) =>
                await Upsert(file, connection, transaction));
        }
        private static async Task<UpsertResult> Upsert(DbFile file, SqlConnection connection, SqlTransaction transaction)
        {
            var tableName = Table<DbFile>();
            var sql =
                $@"MERGE INTO {tableName} AS T
                USING (values (
                    @{nameof(DbFile.Name)}, 
                    @{nameof(DbFile.Extension)},   
                    @{nameof(DbFile.FullPath)},   
                    @{nameof(DbFile.SourceId)},   
                    @{nameof(DbFile.Text)},   
                    @{nameof(DbFile.BytesStream)})) AS S 
                    ({Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.Extension))},
                    {Column<DbFile>(nameof(DbFile.FullPath))},
                    {Column<DbFile>(nameof(DbFile.SourceId))},
                    {Column<DbFile>(nameof(DbFile.Text))}, 
                    {Column<DbFile>(nameof(DbFile.BytesStream))}) 
                    ON T.{Column<DbFile>(nameof(DbFile.FullPath))} = S.{Column<DbFile>(nameof(DbFile.FullPath))} AND
                        T.{Column<DbFile>(nameof(DbFile.SourceId))} = S.{Column<DbFile>(nameof(DbFile.SourceId))}
                WHEN MATCHED THEN
	                UPDATE SET 
                        T.{Column<DbFile>(nameof(DbFile.Name))} = S.{Column<DbFile>(nameof(DbFile.Name))},
                        T.{Column<DbFile>(nameof(DbFile.Extension))} = S.{Column<DbFile>(nameof(DbFile.Extension))},
                        T.{Column<DbFile>(nameof(DbFile.Text))} = S.{Column<DbFile>(nameof(DbFile.Text))},
                        T.{Column<DbFile>(nameof(DbFile.BytesStream))} = S.{Column<DbFile>(nameof(DbFile.BytesStream))}
                WHEN NOT MATCHED THEN
                    INSERT 
                        ({Column<DbFile>(nameof(DbFile.Name))},
                        {Column<DbFile>(nameof(DbFile.Extension))},
                        {Column<DbFile>(nameof(DbFile.FullPath))},
                        {Column<DbFile>(nameof(DbFile.SourceId))},
                        {Column<DbFile>(nameof(DbFile.Text))},
                        {Column<DbFile>(nameof(DbFile.BytesStream))}) 
                    VALUES 
                        (S.{Column<DbFile>(nameof(DbFile.Name))},
                        S.{Column<DbFile>(nameof(DbFile.Extension))},
                        S.{Column<DbFile>(nameof(DbFile.FullPath))},
                        S.{Column<DbFile>(nameof(DbFile.SourceId))},
                        S.{Column<DbFile>(nameof(DbFile.Text))},
                        S.{Column<DbFile>(nameof(DbFile.BytesStream))})

                output inserted.{Column<DbFile>(nameof(DbFile.Id))} as {nameof(UpsertResult.Id)}, $action as {nameof(UpsertResult.Action)};";

            //Источник
            var source = new Source()
            {
                MachineName = Environment.MachineName,
                RootPath = Path.GetPathRoot(file.FullPath) ?? ""
            };
            //если это сетевой ресурс, то не запоминаем имя компьютера
            if (file.FullPath.StartsWith("\\\\") || file.FullPath.StartsWith("//"))
                source.MachineName = null;            
            file.SourceId = await Create(source, connection, transaction);
            //Файл
            using var command = new SqlCommand(sql, connection, transaction);
            using var fileStream = File.OpenRead(file.FullPath);
            command.Parameters.Add($"@{nameof(DbFile.BytesStream)}", SqlDbType.VarBinary, -1).Value = fileStream;
            command.Parameters.AddWithValue($"@{nameof(DbFile.Name)}", file.Name);
            command.Parameters.AddWithValue($"@{nameof(DbFile.Extension)}", file.Extension);
            command.Parameters.AddWithValue($"@{nameof(DbFile.FullPath)}", file.FullPath);
            command.Parameters.AddWithValue($"@{nameof(DbFile.SourceId)}", file.SourceId);
            command.Parameters.AddWithValue($"@{nameof(DbFile.Text)}", file.Text ?? (object)DBNull.Value);
            using var reader = await command.ExecuteReaderAsync();
            reader.Read();
            var result = new UpsertResult()
            {
                Id = reader.GetInt64(nameof(UpsertResult.Id)),
                Action = reader.GetString(nameof(UpsertResult.Action))
            };
            file.Id = result.Id;
            //Аудит
            await Create(connection, transaction,
                new Audit()
                {
                    Keys = JsonConvert.SerializeObject(Keys(file)),
                    State = result.Action,
                    TableName = tableName,
                    NewValues = JsonConvert.SerializeObject(file),
                });
            return result;
        }

        //Обновление одного файла
        public static async Task Update(DbFile file, string propertyName)
        {
            var tableName = Table<DbFile>();
            var sql =
               $@"Update {tableName}
                Set {Column<DbFile>(propertyName)} = @{propertyName}
                Where {Column<DbFile>(nameof(DbFile.Id))} = @{nameof(DbFile.Id)}";

            await TransactionDecorator(async (connection, transaction) =>
            {
                var oldFile = await GetFile(file.Id);
                await connection.ExecuteAsync(sql, file, transaction);
                await Create(connection, transaction,
                    new Audit()
                    {
                        Keys = JsonConvert.SerializeObject(Keys(file)),
                        State = nameof(AuditState.Update),
                        TableName = tableName,
                        NewValues = JsonConvert.SerializeObject(file),
                        OldValues = JsonConvert.SerializeObject(oldFile),
                    });
            });
        }

        //Запрос одного файла
        public static async Task<DbFile> GetFile(long id)
        {
            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await GetFile(connection, null, id);
        }
        static async Task<DbFile> GetFile(SqlConnection connection, SqlTransaction? t, long id)
        {
            var selectSql =
                $@"Select
                    {Column<DbFile>(nameof(DbFile.Id))},
                    {Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.FullPath))},
                    {Column<DbFile>(nameof(DbFile.Extension))}
                From {Table<DbFile>()}
                Where {Column<DbFile>(nameof(DbFile.Id))} = @{nameof(DbFile.Id)}";
            return await connection.QueryFirstAsync<DbFile>(selectSql, new DbFile { Id = id }, t);
        }

        //Запрос файлов по фильтру
        public static async Task<IEnumerable<DbFile>> GetFiles(FileFilter? filter = null)
        {
            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await GetFiles(connection, null, filter);
        }
        static async Task<IEnumerable<DbFile>> GetFiles(SqlConnection connection, SqlTransaction? t, FileFilter? filter)
        {
            var selectSql =
                $@"Select
                    {Column<DbFile>(nameof(DbFile.Id))},
                    {Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.FullPath))},
                    {Column<DbFile>(nameof(DbFile.Extension))}
                From {Table<DbFile>()}
                {FileFilterToCondition(filter)}";
            return await connection.QueryAsync<DbFile>(selectSql, filter, t);
        }

        //Скачивание одного файла. Возвращает найден ли файл
        public static async Task<bool> DownloadFile(long id, string path)
        {
            var selectSql =
                $@"Select {Column<DbFile>(nameof(DbFile.BytesStream))}
                From {Table<DbFile>()}
                Where {Column<DbFile>(nameof(DbFile.Id))} = @{nameof(DbFile.Id)}";

            using SqlConnection connection = new(Properties.Settings.Default.ConnectionString);
            connection.Open();
            SqlCommand command = new(selectSql, connection);
            command.Parameters.AddWithValue($"@{nameof(DbFile.Id)}", id);

            using SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
            if (!await reader.ReadAsync())
                return false;
            using Stream dbStream = reader.GetStream(0);
            using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await dbStream.CopyToAsync(fileStream);
            return true;
        }

        //Запрос Id файлов по фильтру
        public static IEnumerable<long> GetAllFilesIds(FileFilter? filter = null)
        {
            var sql = $@"Select {Column<DbFile>(nameof(DbFile.Id))}
                        From {Table<DbFile>()}
                        {FileFilterToCondition(filter)}";
            using SqlConnection connection = new(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var reader = connection.ExecuteReader(sql,filter);
            while (reader.Read())
            {
                yield return reader.GetInt64(0);
            }
        }

        //Превращает FileFilter в условие Where...
        private static string FileFilterToCondition(FileFilter? filter)
        {
            if (filter == null) return "";

            List<string> conditions = [];
            if (filter.SourceId != null)
            {
                conditions.Add($"{Column<DbFile>(nameof(DbFile.SourceId))} = @{nameof(DbFile.SourceId)}");
            }
            if (!string.IsNullOrWhiteSpace(filter.Text))
            {
                var textCondition = $"{Column<DbFile>(nameof(DbFile.FullPath))} LIKE CONCAT('%', @{nameof(DbFile.Text)}, '%')";
                if (filter?.SearchInText == true)
                {
                    textCondition += $" OR FREETEXT({Column<DbFile>(nameof(DbFile.Text))}, @{nameof(DbFile.Text)})";
                }
                conditions.Add(textCondition);
            }
            var condition = "";
            if (conditions.Count > 0)
            {
                condition = "Where ";
                var and = " AND ";
                foreach (string s in conditions)
                {
                    condition += $"({s}){and}";
                }
                condition = condition[..^and.Length];
            }
            return condition;
        }

        //Запрос количества всех файлов
        public static async Task<long> GetAllFilesCount(FileFilter? filter)
        {
            DbFile args = new() { Text = filter?.Text?.Trim(), SourceId = filter?.SourceId ?? 0 };
            var sql =
                $@"Select count(*)                 
                From {Table<DbFile>()}
                {FileFilterToCondition(filter)}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await connection.ExecuteScalarAsync<long>(sql, args);
        }

        //Запрос страницу файлов
        public static async Task<List<DbFile>> GetFilesPage(int num, FileFilter? filter)
        {
            DbFile args = new() { Text = filter?.Text?.Trim(), SourceId = filter?.SourceId ?? 0 };
            string select =
                $@"Select
                    {Column<DbFile>(nameof(DbFile.Id))},
                    {Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.FullPath))},
                    {Column<DbFile>(nameof(DbFile.Extension))}";
            if (filter?.SearchInText == true)
            {
                int n = Properties.Settings.Default.FullTextSearchPlusMinusSymbols;
                select +=
                    $@",
                        CASE
                            WHEN CHARINDEX(@{nameof(DbFile.Text)}, {Column<DbFile>(nameof(DbFile.Text))}) > 0
                            THEN 
                                '... ' +
                                SUBSTRING(
                                    {Column<DbFile>(nameof(DbFile.Text))},
                                    CASE
                                        WHEN CHARINDEX(@{nameof(DbFile.Text)}, {Column<DbFile>(nameof(DbFile.Text))}) - {n} < 1 THEN 1
                                        ELSE CHARINDEX(@{nameof(DbFile.Text)}, {Column<DbFile>(nameof(DbFile.Text))}) - {n}
                                    END,
                                    {n} * 2 + LEN(@{nameof(DbFile.Text)})
                                ) + ' ...' 
                            ELSE ''
                        END AS {nameof(DbFile.FoundText)}";
            }

            var sql =
                $@"{select}
                From {Table<DbFile>()}
                {FileFilterToCondition(filter)}
                Order by {Column<DbFile>(nameof(DbFile.Id))}
                Offset {(num - 1) * Properties.Settings.Default.ItemsPerPage} rows
                Fetch next {Properties.Settings.Default.ItemsPerPage} rows only";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return [.. await connection.QueryAsync<DbFile>(sql, args)];
        }

        #endregion

        #region Аудит

        //Создает Аудит
        private static async Task Create(SqlConnection connection, SqlTransaction transaction, Audit audit)
        {
            audit.DateTime = DateTime.Now;
            audit.UserName = Environment.UserName;
            audit.State = audit.State.ToUpper();

            var sql =
                $@"Insert into {Table<Audit>()} (
                    {Column<Audit>(nameof(Audit.UserName))},
                    {Column<Audit>(nameof(Audit.DateTime))},
                    {Column<Audit>(nameof(Audit.TableName))},
                    {Column<Audit>(nameof(Audit.State))},
                    {Column<Audit>(nameof(Audit.Keys))},
                    {Column<Audit>(nameof(Audit.OldValues))},
                    {Column<Audit>(nameof(Audit.NewValues))})
                values (
                    @{nameof(Audit.UserName)}, 
                    @{nameof(Audit.DateTime)}, 
                    @{nameof(Audit.TableName)},
                    @{nameof(Audit.State)},
                    @{nameof(Audit.Keys)},
                    @{nameof(Audit.OldValues)},
                    @{nameof(Audit.NewValues)})";

            await connection.ExecuteAsync(sql, audit, transaction);
        }

        #endregion

        #region Работа с атрибутами классов

        //Возвращает атрибут свойства класса, обозначающий колонку в БД
        private static string Column(PropertyInfo propInfo)
        {
            var columnAttr = propInfo.GetCustomAttribute<ColumnAttribute>();
            if (string.IsNullOrEmpty(columnAttr?.Name))
                throw new NullReferenceException();
            return columnAttr.Name;
        }
        private static string Column<T>(string propertyName)
        {
            var prop = typeof(T).GetProperty(propertyName);
            return prop is null ? throw new NullReferenceException() : Column(prop);
        }

        //Возвращает атрибут класса, обозначающий таблицу в БД
        private static string Table<T>()
        {
            var tableName = typeof(T).GetCustomAttribute<TableAttribute>();
            if (string.IsNullOrEmpty(tableName?.Name))
                throw new NullReferenceException();
            return tableName.Name;
        }

        //Возвращает атрибуты класса, которые помечены ключевыми
        private static Dictionary<string, object?> Keys<T>(T obj)
        {
            Dictionary<string, object?> result = [];
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                var keyAttr = prop.GetCustomAttribute<KeyAttribute>();
                if (keyAttr == null) continue;
                result[prop.Name] = prop.GetValue(obj);
            }
            return result;
        }

        #endregion
    }
}
