using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace TableTopSim.Server
{
    public static class SqlExtensions
    {
        public static async Task<bool> TryOpenAsync(this DbConnection connection)
        {
            try
            {
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static T? GetNullableSqlVal<T>(object sqlValue) where T : struct
        {
            if (sqlValue == DBNull.Value)
            {
                return null;
            }
            return (T)sqlValue;
        }
        public static void AddWithValue(this DbParameterCollection dbParameterCollection, string parameterName, object value)
        {
            dbParameterCollection.Add(new SqlParameter(parameterName, value));
        }
        public static DbCommand GetDbCommand(this DbConnection connection, string cmdText)
        {
            if(connection is SqlConnection)
            {
                return new SqlCommand(cmdText, (SqlConnection)connection) { CommandType = System.Data.CommandType.StoredProcedure };
            }
            else if(connection is NpgsqlConnection)
            {
                return new NpgsqlCommand(cmdText, (NpgsqlConnection)connection) { CommandType = System.Data.CommandType.StoredProcedure };
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
