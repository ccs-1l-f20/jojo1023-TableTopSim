using System;
using System.Collections.Generic;
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
            string cmdText = @"Insert Into Rooms Values(@gameId, 1)
Declare @RoomId int
Set @RoomId = SCOPE_IDENTITY()
Insert Into Players Values(@playerName, @RoomId)
Declare @PlayerId int
Set @PlayerId = SCOPE_IDENTITY()
Insert Into RoomHosts Values(@RoomId, @PlayerId)


SELECT @PlayerId As [PlayerId], @RoomId As [RoomId]";

            SqlCommand command = new SqlCommand(cmdText, connection);
            command.Parameters.AddWithValue("@playerName", playerName);
            command.Parameters.AddWithValue("@gameId", gameId);

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


        [HttpPost("CreatePlayerinRoom/{playerName}/{roomId}")]
        public async Task<int?> CreatePlayerinRoom(string playerName, int roomId)
        {
            string cmdText = @"IF EXISTS (SELECT * FROM Rooms WHERE RoomId=@roomId And RoomOpen = 1) 
BEGIN
  Insert Into Players Values(@playerName, @roomId)
  Select SCOPE_IDENTITY() As [PlayerId]
END
ELSE
BEGIN
    SELECT NULL As [PlayerId]
END";

            SqlCommand command = new SqlCommand(cmdText, connection);
            command.Parameters.AddWithValue("@playerName", playerName);
            command.Parameters.AddWithValue("@roomId", roomId);

            if (!(await connection.TryOpenAsync())) { return null; }

            var dr = await command.ExecuteReaderAsync();
            int? retVal = null;
            if (await dr.ReadAsync())
            {
                retVal = SqlExtensions.GetNullableSqlVal<int>(dr["PlayerId"]);
            }

            connection.Close();
            return retVal;
        }

        [HttpGet("GetPlayers/{roomId}")]
        public async Task<Player[]> GetPlayers(int roomId)
        {
            string cmdText = @"Select * From Players Where RoomId = @roomId";
            SqlCommand command = new SqlCommand(cmdText, connection);
            command.Parameters.AddWithValue("@roomId", roomId);

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
            string cmdText = @"IF EXISTS 
(SELECT * FROM Rooms 
Inner Join RoomHosts On Rooms.RoomId = RoomHosts.RoomId
Where Rooms.RoomId=@roomId And Rooms.RoomOpen = 1 And RoomHosts.HostPlayerId=@playerId) 
BEGIN
  Update Rooms Set RoomOpen = 0 Where RoomId = @roomId
  Select Cast(1 as bit) As [Sucsessful]
END
ELSE
BEGIN
    SELECT Cast(0 as bit) As [Sucsessful]
END
";
            SqlCommand command = new SqlCommand(cmdText, connection);
            command.Parameters.AddWithValue("@roomId", roomId);
            command.Parameters.AddWithValue("@playerId", playerId);

            if (!(await connection.TryOpenAsync())) { return BadRequest(); }

            bool retVal = false;
            var dr = command.ExecuteReader();

            if (dr.Read())
            {
                retVal = (bool)dr["Sucsessful"];
            }

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
