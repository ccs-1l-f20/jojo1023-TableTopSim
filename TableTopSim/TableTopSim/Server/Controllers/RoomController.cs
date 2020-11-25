using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TableTopSim.Shared;

namespace TableTopSim.Server.Controllers
{
    [Route("api/Room")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        DbConnection connection;
        public RoomController(DbConnection connection)
        {
            this.connection = connection;
        }

        //[HttpPost("CreatePlayerAndRoom/{playerName}")]
        //public async Task<PlayerAndRoomId> CreatePlayerAndRoom(string playerName)
        //{
        //    return await CreatePlayerAndRoom(connection, playerName);
        //}
        public static async Task<PlayerAndRoomId> CreatePlayerAndRoom(DbConnection connection, int gameId, string playerName)
        {
            DbCommand command = connection.GetDbCommand("uspCreatePlayerAndRoom");
            command.Parameters.AddWithValue("@GameId", gameId);
            command.Parameters.AddWithValue("@PlayerName", playerName);

            if (!(await connection.TryOpenAsync())) { return null; }

            var dr = await command.ExecuteReaderAsync();

            PlayerAndRoomId retVal = null;
            if (await dr.ReadAsync())
            {
                retVal = new PlayerAndRoomId((int)dr["PlayerId"], (int)dr["RoomId"]);
            }

            connection.Close();
            return retVal;
        }

        //[HttpPost("CreatePlayerInRoom/{playerName}/{roomId}")]
        //public async Task<int?> CreatePlayerInRoom(string playerName, int roomId)
        //{
        //    return await CreatePlayerInRoom(connection, playerName, roomId);
        //}
        public static async Task<(int? playerId, bool noRoom)> CreatePlayerInRoom(DbConnection connection, string playerName, int roomId)
        {
            DbCommand command = connection.GetDbCommand("uspCreatePlayerInRoom");
            command.Parameters.AddWithValue("@PlayerName", playerName);
            command.Parameters.AddWithValue("@RoomId", roomId);

            if (!(await connection.TryOpenAsync())) { return (null, false); }

            var dr = await command.ExecuteReaderAsync();

            int? retVal = null;
            bool noRoom = false;
            if (await dr.ReadAsync())
            {
                retVal = SqlExtensions.GetNullableSqlVal<int>(dr["PlayerId"]);
                if (retVal == null)
                {
                    noRoom = (bool)dr["NoRoom"];
                }
            }

            connection.Close();
            return (retVal, noRoom);
        }

        [HttpGet("GetPlayers/{roomId}")]
        public async Task<Player[]> GetPlayers(int roomId)
        {
            DbCommand command = connection.GetDbCommand("uspGetPlayers");
            command.Parameters.AddWithValue("@RoomId", roomId);

            if (!(await connection.TryOpenAsync())) { return null; }

            var dr = await command.ExecuteReaderAsync();
            List<Player> players = null;
            while (await dr.ReadAsync())
            {
                if (players == null) { players = new List<Player>(); }
                int hostId = (int)dr["HostPlayerId"];
                int playerId = (int)dr["PlayerId"];
                players.Add(new Player(playerId, (string)dr["Name"], roomId, (int)dr["GameId"], playerId == hostId, (bool)dr["RoomOpen"]));
            }

            connection.Close();
            return players == null ? null : players.ToArray();
        }
        [HttpGet("GetPlayerRoom/{playerId}")]
        public async Task<Player> GetPlayerRoom(int playerId)
        {
            return await GetPlayerRoom(connection, playerId);
        }
        public static async Task<Player> GetPlayerRoom(DbConnection connection, int playerId)
        {
            DbCommand command = connection.GetDbCommand("uspGetPlayerRoom");
            command.Parameters.AddWithValue("@PlayerId", playerId);

            if (!(await connection.TryOpenAsync())) { return null; }

            var dr = await command.ExecuteReaderAsync();
            Player p = null;
            if (await dr.ReadAsync())
            {
                int? roomId = SqlExtensions.GetNullableSqlVal<int>(dr["RoomId"]);
                bool isHost = false;
                int gameId = -1;
                if (roomId != null)
                {
                    isHost = playerId == (int)dr["HostPlayerId"];
                    gameId = (int)dr["GameId"];
                }
                p = new Player((int)dr["PlayerId"], (string)dr["Name"], roomId, gameId, isHost, (bool)dr["RoomOpen"]);
            }
            connection.Close();
            return p;
        }

        [HttpPost("StartGame/{roomId}/{playerId}")]
        public async Task<IActionResult> StartGame(int roomId, int playerId)
        {
            if (await StartGame(connection, roomId, playerId))
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
        public static async Task<bool> StartGame(DbConnection connection, int roomId, int playerId)
        {
            DbCommand command = connection.GetDbCommand("uspStartGame");
            command.Parameters.AddWithValue("@RoomId", roomId);
            command.Parameters.AddWithValue("@PlayerId", playerId);

            if (!(await connection.TryOpenAsync())) { return false; }

            bool retVal = (bool)(await command.ExecuteScalarAsync());

            connection.Close();
            return retVal;
        }
    }
}
