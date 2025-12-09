using Ark.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ark.Services
{
    public static class DatabaseService
    {
        //Документы
        //Создание
        public static async Task<long> Create(DbFile doc)
        {
            var insertSql =
                $@"Insert into {DbFile.TableName()} (Name, Bytes, Text)
                output inserted.Id
                values (
                    @{nameof(DbFile.Name)}, 
                    @{nameof(DbFile.Bytes)},
                    @{nameof(DbFile.Text)})";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var insertedId = await connection.QueryFirstAsync<long>(insertSql, doc, transaction);
                doc.Id = insertedId;
                await Create(connection, transaction,
                    new Audit()
                    {
                        Keys = JsonSerializer.Serialize(doc.Keys()),
                        State = nameof(AuditState.Create),
                        TableName = DbFile.TableName(),
                        NewValues = JsonSerializer.Serialize(doc),
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

        //Запрос одного
        public static async Task<DbFile> GetDocument(long id)
        {
            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await GetDocument(connection, null, id);
        }
        static async Task<DbFile> GetDocument(SqlConnection connection, SqlTransaction? t, long id)
        {
            var selectSql =
                $@"Select *
                From {DbFile.TableName()}
                Where Id = @{nameof(DbFile.Id)}";
            return await connection.QueryFirstAsync<DbFile>(selectSql, new { Id = id }, t);
        }

        //Запрос нескольких
        static string DocumetTextCondition(string? text)
        {
            //TODO Сделать text параметром
            return string.IsNullOrWhiteSpace(text) ? "" : $"Where Name like '%{text.Trim()}%' or contains(Text, '{text.Trim()}')";
        }
        public static async Task<long> GetDocumentsCount(string? text = null)
        {
            var sql =
                $@"Select count(*) 
                {DocumetTextCondition(text)}
                From {DbFile.TableName()}";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return await connection.ExecuteScalarAsync<long>(sql);
        }

        public static async Task<List<DbFile>> GetDocumentsPage(int num, string? text = null)
        {
            var sql =
                $@"Select Id, Name
                From {DbFile.TableName()}
                {DocumetTextCondition(text)}
                Order by Id
                Offset {(num - 1) * Properties.Settings.Default.ItemsPerPage} rows
                Fetch next {Properties.Settings.Default.ItemsPerPage} rows only";

            using var connection = new SqlConnection(Properties.Settings.Default.ConnectionString);
            return (await connection.QueryAsync<DbFile>(sql)).ToList();
        }


        //Аудит
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
    }
}
