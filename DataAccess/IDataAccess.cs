using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IDataAccess 
    {
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task DisposeAsync();
        Task RollbackTransactionAsync();
       
        Task SqlExcuteQueryAsync(string query);
        Task SqlExcuteQueryAsync(string query, SqlParameter[] sqlParameters);
        Task SqlExecuteStoredProcedureAsync(SqlCommand cmd);
        Task<T> SqlSelectAsync<T>(string query) where T : new();
        Task<T> SqlSelectAsync<T>(string query, SqlParameter sqlParameters) where T : new();
        Task<T> SqlSelectAsync<T>(string query, SqlParameter[] sqlParameters) where T : new();
        Task<List<T>> SqlSelectListAsync<T>(string query) where T : new();
        Task<List<T>> SqlSelectListAsync<T>(string query, SqlParameter sqlParameter) where T : new();
        Task<List<T>> SqlSelectListAsync<T>(string query, SqlParameter[] sqlParameters) where T : new();
    }
}
