using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DataLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TableTopSim.Server.Controllers
{
    [Route("api/Game")]
    [ApiController]
    public class GameController : ControllerBase
    {
        SqlConnection connection;
        public GameController(SqlConnection connection)
        {
            this.connection = connection;
        }

        [HttpPost("AddGame")]
        public async Task<int?> AddGame([FromBody] GameDataDto gameData)
        {
            SqlCommand command = new SqlCommand("uspAddGame", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@Name", gameData.Name);
            command.Parameters.AddWithValue("@Width", gameData.Width);
            command.Parameters.AddWithValue("@Height", gameData.Height);
            command.Parameters.AddWithValue("@MinPlayers", gameData.MinPlayers);
            command.Parameters.AddWithValue("@MaxPlayers", gameData.MaxPlayers);
            command.Parameters.AddWithValue("@SpriteDictionary", gameData.SerializedSpriteDictionary);

            if (!(await connection.TryOpenAsync())) { return null; }

            int retVal = (int)(await command.ExecuteScalarAsync());
            connection.Close();
            return retVal;
        }

        [HttpPost("AddGameImages/{gameId}")]
        public async Task<IActionResult> AddGameImages(int gameId, [FromBody] string imagesStr)
        {
            Dictionary<int, ImageDto> images = JsonConvert.DeserializeObject<Dictionary<int, ImageDto>>(imagesStr);
            if (!(await connection.TryOpenAsync())) { return BadRequest(); }

            await DeleteGameImages(gameId, connection);

            foreach(var k in images.Keys)
            {
                var image = images[k];
                bool s = await AddGameImage(gameId, k, image, connection);
                if (!s)
                {
                    connection.Close();
                    return BadRequest();
                }
            }

            connection.Close();
            return Ok();
        }
        static async Task DeleteGameImages(int gameId, SqlConnection connection)
        {
            SqlCommand command = new SqlCommand("uspDeleteGameImages", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@GameId", gameId);
            await command.ExecuteNonQueryAsync();
        }
        static async Task<bool> AddGameImage(int gameId, int inGameImageId, ImageDto image, SqlConnection connection)
        {
            SqlCommand command = new SqlCommand("uspAddGameImage", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@GameId", gameId);
            command.Parameters.AddWithValue("@InGameImageId", inGameImageId);
            command.Parameters.AddWithValue("@Image", image.Image);
            command.Parameters.AddWithValue("@Format", image.Format);
            return (bool)(await command.ExecuteScalarAsync());
        }

        [HttpPost("DeleteGame/{gameId}")]
        public async Task<IActionResult> DeleteGame(int gameId)
        {
            SqlCommand command = new SqlCommand("uspDeleteGame", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@GameId", gameId);

            if (!(await connection.TryOpenAsync())) { return BadRequest(); }

            await command.ExecuteNonQueryAsync();
            connection.Close();
            return Ok();
        }
        [HttpGet("GetGames")]
        public async Task<string> GetGames()
        {
            SqlCommand command = new SqlCommand("uspGetGames", connection) { CommandType = CommandType.StoredProcedure };
            if (!(await connection.TryOpenAsync())) { return null; }
            Dictionary<int, GameDataDto> games = new Dictionary<int, GameDataDto>();

            var dr = await command.ExecuteReaderAsync();
            while (await dr.ReadAsync())
            {
                games.Add((int)dr["GameId"], new GameDataDto((string)dr["Name"], 0, 0, (int)dr["MinPlayers"], (int)dr["MaxPlayers"], null));
            }
            connection.Close();
            return JsonConvert.SerializeObject(games);
        }

        [HttpGet("GetGame/{gameId}")]
        public async Task<GameDataDto> GetGame(int gameId)
        {
            return await GetGame(connection, gameId);
        }

        public static async Task<GameDataDto> GetGame(SqlConnection connection, int gameId)
        {
            SqlCommand command = new SqlCommand("uspGetGame", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@GameId", gameId);

            if (!(await connection.TryOpenAsync())) { return null; }
            var dr = await command.ExecuteReaderAsync();
            GameDataDto game = null;
            if (await dr.ReadAsync())
            {
                game = new GameDataDto((string)dr["Name"], (int)dr["Width"], (int)dr["Height"], (int)dr["MinPlayers"], (int)dr["MaxPlayers"], (string)dr["SpriteDictionary"]);
            }
            connection.Close();
            return game;
        }
        [HttpGet("GetGameImages/{gameId}")]
        public async Task<string> GetGameImages(int gameId)
        {
            SqlCommand command = new SqlCommand("uspGetGameImages", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@GameId", gameId);

            if (!(await connection.TryOpenAsync())) { return null; }
            var dr = await command.ExecuteReaderAsync();
            Dictionary<int, ImageDto> images = new Dictionary<int, ImageDto>();
            while (await dr.ReadAsync())
            {
                int imgId = (int)dr["ImageId"];
                var img = new ImageDto(imgId, (string)dr["Format"], (byte[])dr["Image"]);
                img.UpdateUrl();
                img.Image = null;
                images.Add(imgId, img);
            }
            connection.Close();
            return JsonConvert.SerializeObject(images);
        }
    }
}
