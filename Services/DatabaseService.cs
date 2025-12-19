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
    public static class DatabaseService
    {
        #region Файлы

        //Создание одного файла
        public static async Task<long> Create(DbFile file)
        {
            var tableName = Table<DbFile>();
            var insertSql =
                $@"Insert into {tableName} 
                    ({Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.Extension))},
                    {Column<DbFile>(nameof(DbFile.Path))},
                    {Column<DbFile>(nameof(DbFile.Text))})
                output inserted.{Column<DbFile>(nameof(DbFile.Id))}
                values (
                    @{nameof(DbFile.Name)}, 
                    @{nameof(DbFile.Extension)},   
                    @{nameof(DbFile.Path)},   
                    @{nameof(DbFile.Text)})";

            var updateSql =
                $@"Update {tableName}
                Set {Column<DbFile>(nameof(DbFile.BytesStream))} = @{nameof(DbFile.BytesStream)}
                Where {Column<DbFile>(nameof(DbFile.Id))} = @{nameof(DbFile.Id)}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                //Создаем запись с большинством данных
                var insertedId = await connection.QueryFirstAsync<long>(insertSql, file, transaction);
                file.Id = insertedId;
                //Добавляем бинарные данные файла
                using var command = new SqlCommand(updateSql, connection, transaction);
                using var fileStream = File.OpenRead(file.Path);
                command.Parameters.Add($"@{nameof(DbFile.BytesStream)}", SqlDbType.VarBinary, -1).Value = fileStream;
                command.Parameters.Add($"@{nameof(DbFile.Id)}", SqlDbType.BigInt).Value = file.Id;
                await command.ExecuteNonQueryAsync();
                //Аудит
                await Create(connection, transaction,
                    new Audit()
                    {
                        Keys = JsonConvert.SerializeObject(Keys(file)),
                        State = nameof(AuditState.Create),
                        TableName = tableName,
                        NewValues = JsonConvert.SerializeObject(file),
                    });
                transaction.Commit();
                return insertedId;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        //Обновление одного файла
        public static async Task Update(DbFile file, string propertyName)
        {
            var tableName = Table<DbFile>();
            var sql =
               $@"Update {tableName}
                Set {Column<DbFile>(propertyName)} = @{propertyName}
                Where {Column<DbFile>(nameof(DbFile.Id))} = @{nameof(DbFile.Id)}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
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
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
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
                    {Column<DbFile>(nameof(DbFile.Path))},
                    {Column<DbFile>(nameof(DbFile.Extension))}
                From {Table<DbFile>()}
                Where {Column<DbFile>(nameof(DbFile.Id))} = @{nameof(DbFile.Id)}";
            return await connection.QueryFirstAsync<DbFile>(selectSql, new DbFile { Id = id }, t);
        }

        //Скачивание одного файла
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

        //Запрос количества всех файлов
        public static async Task<long> GetAllFilesCount(bool searchInText = false, string? text = null)
        {
            var args = new { FindText = text?.Trim() };
            string condition = "";
            if (!string.IsNullOrWhiteSpace(args.FindText))
            {
                condition = $@"Where CONCAT(
                                {Column<DbFile>(nameof(DbFile.Name))}, 
                                {Column<DbFile>(nameof(DbFile.Extension))}) 
                            LIKE CONCAT('%', @{nameof(args.FindText)}, '%')";
                if (searchInText)
                    condition += $" OR FREETEXT({Column<DbFile>(nameof(DbFile.Text))}, @{nameof(args.FindText)})";
            }
            var sql =
                $@"Select count(*)                 
                From {Table<DbFile>()}
                {condition}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await connection.ExecuteScalarAsync<long>(sql, args);
        }

        //Запрос страницу файлов
        public static async Task<List<DbFile>> GetFilesPage(int num, bool searchInText = false, string? text = null)
        {
            var args = new { FindText = text?.Trim() };
            string select =
                $@"Select
                    {Column<DbFile>(nameof(DbFile.Id))},
                    {Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.Extension))}";
            string condition = "";
            if (!string.IsNullOrWhiteSpace(args.FindText))
            {
                condition = $@"Where CONCAT(
                                {Column<DbFile>(nameof(DbFile.Name))}, 
                                {Column<DbFile>(nameof(DbFile.Extension))}) 
                            LIKE CONCAT('%', @{nameof(args.FindText)}, '%')";
                if (searchInText)
                {
                    condition += $" OR FREETEXT({Column<DbFile>(nameof(DbFile.Text))}, @{nameof(args.FindText)})";

                    int n = Properties.Settings.Default.FullTextSearchPlusMinusSymbols;
                    select +=
                        $@",
                        CASE
                            WHEN CHARINDEX(@{nameof(args.FindText)}, {Column<DbFile>(nameof(DbFile.Text))}) > 0
                            THEN 
                                '... ' +
                                SUBSTRING(
                                    {Column<DbFile>(nameof(DbFile.Text))},
                                    CASE
                                        WHEN CHARINDEX(@{nameof(args.FindText)}, {Column<DbFile>(nameof(DbFile.Text))}) - {n} < 1 THEN 1
                                        ELSE CHARINDEX(@{nameof(args.FindText)}, {Column<DbFile>(nameof(DbFile.Text))}) - {n}
                                    END,
                                    {n} * 2 + LEN(@{nameof(args.FindText)})
                                ) + ' ...' 
                            ELSE ''
                        END AS {nameof(DbFile.FoundText)}";
                }
            }
            var sql =
                $@"{select}
                From {Table<DbFile>()}
                {condition}
                Order by {Column<DbFile>(nameof(DbFile.Id))}
                Offset {(num - 1) * Properties.Settings.Default.ItemsPerPage} rows
                Fetch next {Properties.Settings.Default.ItemsPerPage} rows only";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return [.. (await connection.QueryAsync<DbFile>(sql, args))];
        }

        #endregion

        #region Аудит

        //Создает Аудит
        private static async Task Create(SqlConnection connection, SqlTransaction t, Audit audit)
        {
            audit.DateTime = DateTime.Now;
            audit.UserName = Environment.UserName;

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

            await connection.ExecuteAsync(sql, audit, t);
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
