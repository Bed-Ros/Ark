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
            return (await Create(new Document[] { doc }))[0];
        }
        public static async Task<List<long>> Create(IEnumerable<Document> docs)
        {
            var insertSql =
                $@"Insert into Files (Name, Bytes)
                values (@{nameof(Document.Name)}, @{nameof(Document.Bytes)})
                returning Id";

            using (var connection = new SqlConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var result = (await connection.QueryAsync<long>(insertSql, docs)).ToList();
                        var audits = new List<Audit>();
                        foreach (long id in result)
                        {
                            var newValues = GetDocument(connection, transaction, id);
                            audits.Add(new Audit()
                            {
                                State = nameof(AuditState.Create),
                                TableName = "Files",
                                NewValues = JsonSerializer.Serialize(newValues),
                            });
                        }
                        await Create(connection, transaction, audits);
                        transaction.Commit();
                        return result;
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
            using (var connection = new SqlConnection())
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

            using (var connection = new SqlConnection())
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

            using (var connection = new SqlConnection())
            {
                return (await connection.QueryAsync<Document>(sql)).ToList();
            }
        }


        //Аудит
        static async Task Create(SqlConnection connection, SqlTransaction t, Audit audit)
        {
            await Create(connection, t, new List<Audit>() { audit });
        }
        static async Task Create(SqlConnection connection, SqlTransaction t, List<Audit> audits)
        {
            var sql =
                $@"Insert into Audit (UserName, DateTime, TableName, State, OldValues, NewValues)
                values (
                    {Environment.UserName}, 
                    {DateTime.Now},
                    @{nameof(Audit.TableName)},
                    @{nameof(Audit.State)},
                    @{nameof(Audit.OldValues)},
                    @{nameof(Audit.NewValues)})";

            await connection.ExecuteAsync(sql, audits, t);
        }
    }
}
