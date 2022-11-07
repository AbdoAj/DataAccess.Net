using DataAccessDotNet;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessDotNet
{
    public class DataAccess: IDataAccess
    {
        private readonly IConfiguration _Config;
        private string ConnectionString;

        public SqlConnection sqlConnection;
        private DbTransaction sqlTransaction;

        public DataAccess(IConfiguration configuration)
        {
            _Config = configuration;
            ConnectionString = _Config.GetConnectionString("Db");
            sqlConnection = new SqlConnection(ConnectionString);
        }

        #region Sql Methods 
        #region Select
        public async Task<T> SqlSelectAsync<T>(string query) where T : new()
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;
                    cmd.CommandTimeout = 0;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        return await Initialize<T>(reader);

                    }
                }
            }
            catch (Exception) { throw; }

        }
        public async Task<T> SqlSelectAsync<T>(string query, SqlParameter sqlParameters) where T : new()
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;

                    cmd.Parameters.Add(sqlParameters);
                    cmd.CommandTimeout = 0;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        return await Initialize<T>(reader);

                    }
                }
            }
            catch (Exception) { throw; }

        }
        public async Task<T> SqlSelectAsync<T>(string query, SqlParameter[] sqlParameters) where T : new()
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;

                    cmd.Parameters.AddRange(sqlParameters);
                    cmd.CommandTimeout = 0;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        return await Initialize<T>(reader);

                    }
                }
            }
            catch (Exception) { throw; }

        }
        #endregion

        #region SelectList
        public async Task<List<T>> SqlSelectListAsync<T>(string query) where T : new()
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;

                    cmd.CommandTimeout = 0;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        return await InitializeList<T>(reader);

                    }
                }
            }
            catch (Exception) { throw; }

        }
        public async Task<List<T>> SqlSelectListAsync<T>(string query, SqlParameter[] sqlParameters) where T : new()
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;

                    cmd.Parameters.AddRange(sqlParameters);
                    cmd.CommandTimeout = 0;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        return await InitializeList<T>(reader);

                    }
                }
            }
            catch (Exception) { throw ; }

        }
        public async Task<List<T>> SqlSelectListAsync<T>(string query, SqlParameter sqlParameter) where T : new()
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;

                    cmd.Parameters.Add(sqlParameter);
                    cmd.CommandTimeout = 0;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        return await InitializeList<T>(reader);

                    }
                }
            }
            catch (Exception) { throw ; }

        }
        #endregion

        public async Task SqlExecuteStoredProcedureAsync(SqlCommand cmd)
        {

            try
            {
                cmd.Transaction = (SqlTransaction)sqlTransaction;
                cmd.Connection = sqlConnection;
                cmd.CommandTimeout = 0;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException) { throw; }
            catch (Exception) { throw; }

        }
        public async Task SqlExcuteQueryAsync(string query)
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;
                    cmd.CommandTimeout = 0;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException) { throw; }
            catch (Exception) { throw; }

        }
        public async Task SqlExcuteQueryAsync(string query, SqlParameter[] sqlParameters)
        {

            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Transaction = sqlTransaction as SqlTransaction;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.AddRange(sqlParameters);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException) { throw ; }
            catch (Exception) { throw; }

        }
        #endregion


        #region SqlConnection & DbTransaction
        public async Task OpenConnectionAsync()
        {
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                    await sqlConnection.OpenAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        private async Task CloseConnectionAsync()
        {
            await Task.Run(()=> sqlConnection.Close());
        }
        public async Task BeginTransactionAsync()
        {
            sqlTransaction = await Task.Run(()=> sqlConnection.BeginTransaction());
        }
        public async Task CommitTransactionAsync()
        {
            await Task.Run(()=> sqlTransaction.Commit());
        }
        public async Task RollbackTransactionAsync()
        {
            if (sqlTransaction != null && sqlTransaction.Connection != null)
                await Task.Run(()=> sqlTransaction.Rollback());
        }
        #endregion

        #region Initializer
        private async Task<T> Initialize<T>(SqlDataReader reader) where T : new()
        {
            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

            List<T> list = new List<T>();
            if (await reader.ReadAsync())
            {
                T t = new T();
                var parentProperties = t.GetType().GetProperties();
                foreach (var parentProperty in parentProperties)
                {

                    if (columns.Any(s => s == parentProperty.Name))
                        if (reader[parentProperty.Name] != DBNull.Value)
                        {
                            var type = parentProperty.PropertyType.IsGenericType && parentProperty.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)) ?
                                        Nullable.GetUnderlyingType(parentProperty.PropertyType) : parentProperty.PropertyType;
                            var value = Convert.ChangeType(reader[parentProperty.Name], type);
                            if (value != null)
                                parentProperty.SetValue(t, value);
                        }
                }
                return t;
            }
            else
                return new T();
        }
        private async Task<List<T>> InitializeList<T>(SqlDataReader reader) where T : new()
        {

            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
            List<T> list = new List<T>();
            while (await reader.ReadAsync())
            {
                T t = new T();
                var parentProperties = t.GetType().GetProperties();
                foreach (var parentProperty in parentProperties)
                {
                    if (columns.Any(s => s == parentProperty.Name))
                        if (reader[parentProperty.Name] != DBNull.Value)
                        {

                            var type = parentProperty.PropertyType.IsGenericType && parentProperty.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)) ?
                                        Nullable.GetUnderlyingType(parentProperty.PropertyType) : parentProperty.PropertyType;

                            var value = Convert.ChangeType(reader[parentProperty.Name], type);
                            if (value != null)
                                parentProperty.SetValue(t, value);

                        }

                }
                list.Add(t);
            }
            return list;
        } 
        #endregion

        public async Task DisposeAsync()
        {
            await CloseConnectionAsync();
            GC.Collect();
        }
    }
}
