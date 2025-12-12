using Ark.Models;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
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
                $@"Insert into {tableName} (Name, Extension, Bytes, Text)
                output inserted.Id
                values (
                    @{nameof(DbFile.Name)}, 
                    @{nameof(DbFile.Extension)}, 
                    @{nameof(DbFile.Bytes)},
                    @{nameof(DbFile.Text)})";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var insertedId = await connection.QueryFirstAsync<long>(insertSql, file, transaction);
                file.Id = insertedId;
                await Create(connection, transaction,
                    new Audit()
                    {
                        Keys = JsonConvert.SerializeObject(file.Keys()),
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
            var columnName = Column<DbFile>(propertyName);
            var tableName = Table<DbFile>();

            var sql =
               $@"Update {tableName}
                Set {columnName} = @{propertyName}
                Where Id = @{nameof(DbFile.Id)}";

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
                        Keys = JsonConvert.SerializeObject(file.Keys()),
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
        public static async Task<DbFile> GetFile(long id, bool includeBytes = false)
        {
            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await GetFile(connection, null, id, includeBytes);
        }
        static async Task<DbFile> GetFile(SqlConnection connection, SqlTransaction? t, long id, bool includeBytes = false)
        {
            var tableName = Table<DbFile>();
            var bytes = includeBytes ? ", Bytes" : "";
            var selectSql =
                $@"Select Id, Name, Extension, Text{bytes}
                From {tableName}
                Where Id = @{nameof(DbFile.Id)}";
            return await connection.QueryFirstAsync<DbFile>(selectSql, new { Id = id }, t);
        }

        //Условие поиска по тексту
        static string FileTextCondition(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.Trim();
            int n = Properties.Settings.Default.FullTextSearchPlusMinusSymbols;
            var tableName = Table<DbFile>();

            //TODO Сделать text параметром
            return
                $@"SELECT
                    CASE
                        WHEN CHARINDEX({text}, T.Text) > 0
                        THEN 
                            '... ' +
                            SUBSTRING(
                                T.Text,
                                CASE
                                    WHEN CHARINDEX({text}, T.Text) - {n} < 1 THEN 1
                                    ELSE CHARINDEX({text}, T.Text) - {n}
                                END,
                                {n} * 2 + LEN({text})
                            ) + ' ...' AS {nameof(DbFile.FoundText)}
                        ELSE ''
                    END
                FROM
                    {tableName} as T
                WHERE
                    CONCAT(Name, Extension) like '%{text}%' OR
                    contains(Text, '{text}')";
        }

        //Запрос количества всех файлов
        public static async Task<long> GetAllFilesCount(bool searchInText = false, string? text = null)
        {
            var tableName = Table<DbFile>();
            string condition = "";
            if (!string.IsNullOrWhiteSpace(text))
            {
                condition = "CONCAT(Name, Extension) LIKE CONCAT('%', @FindText, '%')";
                if (searchInText)
                    condition += " OR CONTAINS(Text, @FindText)";
            }
            var sql =
                $@"Select count(*)                 
                From {tableName}
                Where {condition}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await connection.ExecuteScalarAsync<long>(sql, new { FindText = text });
        }

        //Запрос страницу файлов
        public static async Task<List<DbFile>> GetFilesPage(int num, bool searchInText = false, string? text = null)
        {
            var sql =
                $@"Select
                    {Column<DbFile>(nameof(DbFile.Id))},
                    {Column<DbFile>(nameof(DbFile.Name))},
                    {Column<DbFile>(nameof(DbFile.Extension))}
                From {Table<DbFile>()}
                Order by Id
                Offset {(num - 1) * Properties.Settings.Default.ItemsPerPage} rows
                Fetch next {Properties.Settings.Default.ItemsPerPage} rows only";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return (await connection.QueryAsync<DbFile>(sql, new { FindText = text })).ToList();
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
            if (prop is null) throw new NullReferenceException();
            return Column(prop);
        }

        //Возвращает атрибут класса, обозначающий таблицу в БД
        private static string Table<T>()
        {
            var tableName = typeof(T).GetCustomAttribute<TableAttribute>();
            if (string.IsNullOrEmpty(tableName?.Name))
                throw new NullReferenceException();
            return tableName.Name;
        }

        #endregion
    }
}
