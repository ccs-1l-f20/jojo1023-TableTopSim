using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace TableTopSim.Server
{
    public static class SqlExtensions
    {
        public static async Task<bool> TryOpenAsync(this SqlConnection connection)
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
    }
}
