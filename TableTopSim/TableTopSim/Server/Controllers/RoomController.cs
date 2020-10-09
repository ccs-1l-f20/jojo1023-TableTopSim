using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TableTopSim.Shared;

namespace TableTopSim.Server.Controllers
{
    [Route("api/Room")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        SqlConnection connection;
        public RoomController(SqlConnection connection)
        {
            this.connection = connection;
        }

        [HttpPost("CreatePlayerAndRoom/{playerName}/{gameId}")]
        public async Task<PlayerAndRoomId> CreatePlayerAndRoom(string playerName, int gameId)
        {
            SqlCommand command = new SqlCommand("uspCreatePlayerAndRoom", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@PlayerName", playerName);
            command.Parameters.AddWithValue("@GameId", gameId);

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


        [HttpPost("CreatePlayerInRoom/{playerName}/{roomId}")]
        public async Task<int?> CreatePlayerInRoom(string playerName, int roomId)
        {
            SqlCommand command = new SqlCommand("uspCreatePlayerInRoom", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@PlayerName", playerName);
            command.Parameters.AddWithValue("@RoomId", roomId);

            if (!(await connection.TryOpenAsync())) { return null; }

            int? retVal = SqlExtensions.GetNullableSqlVal<int>(await command.ExecuteScalarAsync());

            connection.Close();
            return retVal;
        }

        [HttpGet("GetPlayers/{roomId}")]
        public async Task<Player[]> GetPlayers(int roomId)
        {
            SqlCommand command = new SqlCommand("uspGetPlayers", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@RoomId", roomId);

            if (!(await connection.TryOpenAsync())) { return null; }

            var dr = await command.ExecuteReaderAsync();
            List<Player> players = null;
            while (await dr.ReadAsync())
            {
                if (players == null) { players = new List<Player>(); }
                players.Add(new Player((int)dr["PlayerId"], (string)dr["Name"], (int)dr["RoomId"]));
            }

            connection.Close();
            return players == null ? null : players.ToArray();
        }

        [HttpPost("StartGame/{roomId}/{playerId}")]
        public async Task<IActionResult> StartGame(int roomId, int playerId)
        {
            SqlCommand command = new SqlCommand("uspStartGame", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@RoomId", roomId);
            command.Parameters.AddWithValue("@PlayerId", playerId);

            if (!(await connection.TryOpenAsync())) { return BadRequest(); }

            bool retVal = (bool)(await command.ExecuteScalarAsync());

            connection.Close();

            if (retVal)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

    }
}
