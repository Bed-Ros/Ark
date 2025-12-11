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
        //Файлы
        //Создание одного файла
        public static async Task<long> Create(DbFile file)
        {
            var insertSql =
                $@"Insert into {DbFile.TableName()} (Name, Extension, Bytes, Text)
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
                        TableName = DbFile.TableName(),
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
            var prop = typeof(DbFile).GetProperty(propertyName);
            if (prop is null) throw new NullReferenceException();
            var columnName = GetColumnName(prop);
            if (columnName is null) throw new NullReferenceException();

            var sql =
               $@"Update {DbFile.TableName()}
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
                        TableName = DbFile.TableName(),
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
            var bytes = includeBytes ? ", Bytes" : "";
            var selectSql =
                $@"Select Id, Name, Extension, Text{bytes}
                From {DbFile.TableName()}
                Where Id = @{nameof(DbFile.Id)}";
            return await connection.QueryFirstAsync<DbFile>(selectSql, new { Id = id }, t);
        }

        //Условие поиска по тексту
        static string FileTextCondition(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }
            else
            {

            }


            //--Ensure the search term is formatted for CONTAINSTABLE if needed(e.g., precise phrase)
            //-- For a precise word match you can use: '"' + @SearchTerm + '"'

            @"DECLARE @SearchTerm NVARCHAR(100) = 'your_keyword';
            DECLARE @ContextLength INT = 50; -- characters around the keyword

            SELECT
                '... ' +
                SUBSTRING(
                    T.ContentColumn,
                    --Calculate the start position, ensuring it's not less than 1
                    -- CHARINDEX returns the starting position(1 - based index)
                    CASE
                        WHEN CHARINDEX(@SearchTerm, T.ContentColumn) - @ContextLength < 1 THEN 1
                        ELSE CHARINDEX(@SearchTerm, T.ContentColumn) - @ContextLength
                    END,
                    --Calculate the length to extract, ensuring it doesn't exceed the column length
                    --(length calculation needs careful handling of start / end boundaries)
                    @ContextLength * 2 + LEN(@SearchTerm)
                ) + ' ...' AS Snippet
            FROM
                YourTableName AS T
            WHERE
                CONTAINS(T.ContentColumn, @SearchTerm); --Use FTS for efficient filtering"


            //TODO Сделать text параметром
            //TODO Добавить работу с расширением файла?
            return string.IsNullOrWhiteSpace(text) ? "" : $"Where Name like '%{text.Trim()}%'";
            //TODO добавить or contains(Text, '{text.Trim()}')"
        }

        //Запрос количества всех файлов
        public static async Task<long> GetAllFilesCount(string? text = null)
        {
            var sql =
                $@"Select count(*)                 
                From {DbFile.TableName()}
                {FileTextCondition(text)}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await connection.ExecuteScalarAsync<long>(sql);
        }

        //Запрос страницу файлов
        public static async Task<List<DbFile>> GetFilesPage(int num, string? text = null)
        {
            var sql =
                $@"Select Id, Name, Extension, Text
                From {DbFile.TableName()}
                {FileTextCondition(text)}
                Order by Id
                Offset {(num - 1) * Properties.Settings.Default.ItemsPerPage} rows
                Fetch next {Properties.Settings.Default.ItemsPerPage} rows only";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return (await connection.QueryAsync<DbFile>(sql)).ToList();
        }

        //Создает Аудит
        static async Task Create(SqlConnection connection, SqlTransaction t, Audit audit)
        {
            audit.DateTime = DateTime.Now;
            audit.UserName = Environment.UserName;

            var sql =
                $@"Insert into Audit (UserName, DateTime, TableName, State, Keys, OldValues, NewValues)
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

        //Возвращает атрибут свойства класса, обозначающий колонку в БД
        private static string? GetColumnName(PropertyInfo propInfo)
        {
            var columnAttr = propInfo.GetCustomAttribute<ColumnAttribute>();
            if (string.IsNullOrEmpty(columnAttr?.Name))
                return null;
            else
                return columnAttr.Name;

        }
    }
}
