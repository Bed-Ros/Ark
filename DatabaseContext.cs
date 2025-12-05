using Ark.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ark
{
    public static class DatabaseContext
    {
        //Документы
        //Создание
        public static async Task<long> Create(Document doc)
        {
            var insertSql =
                $@"Insert into Files (Name, Bytes)
                output inserted.Id
                values (@{nameof(Document.Name)}, @{nameof(Document.Bytes)})";

            using (var connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var insertedId = await connection.QueryFirstAsync<long>(insertSql, doc, transaction);
                        var newValues = await GetDocument(connection, transaction, insertedId);
                        await Create(connection, transaction,
                            new Audit()
                            {
                                State = nameof(AuditState.Create),
                                TableName = "Files",
                                NewValues = JsonSerializer.Serialize(newValues),
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
            }
        }

        //Запрос одного
        public static async Task<Document> GetDocument(long id)
        {
            using (var connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                return await GetDocument(connection, null, id);
            }
        }
        static async Task<Document> GetDocument(SqlConnection connection, SqlTransaction? t, long id)
        {
            var selectSql =
                $@"Select *
                From Files
                Where Id = @{nameof(Document.Id)}";
            return await connection.QueryFirstAsync<Document>(selectSql, new { Id = id }, t);
        }

        //Запрос нескольких
        public static long GetDocumentsCount()
        {
            var sql = "Select count(*) From Files";

            using (var connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                return connection.ExecuteScalar<long>(sql);
            }
        }

        public static async Task<List<Document>> GetDocumentsPage(int num)
        {
            var sql =
                $@"Select * 
                From Files
                Order by Id
                Offset {(num - 1) * Properties.Settings.Default.ItemsPerPage} rows
                Fetch next {Properties.Settings.Default.ItemsPerPage} rows only";

            using (var connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                return (await connection.QueryAsync<Document>(sql)).ToList();
            }
        }


        //Аудит
        static async Task Create(SqlConnection connection, SqlTransaction t, Audit audit)
        {
            audit.DateTime = DateTime.Now;
            audit.UserName = Environment.UserName;

            var sql =
                $@"Insert into Audit (UserName, DateTime, TableName, State, OldValues, NewValues)
                values (
                    @{nameof(Audit.UserName)}, 
                    @{nameof(Audit.DateTime)}, 
                    @{nameof(Audit.TableName)},
                    @{nameof(Audit.State)},
                    @{nameof(Audit.OldValues)},
                    @{nameof(Audit.NewValues)})";

            await connection.ExecuteAsync(sql, audit, t);
        }
    }
}
