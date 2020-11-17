using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TableTopSim.Shared;

namespace DataLayer
{
    public static class RoomDL
    {
        public static async Task<Player[]> GetPlayers(HttpClient http, int roomId)
        {
            var response = await http.GetAsync($"api/Room/GetPlayers/{roomId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Player[]>(content);
            }
            else
            {
                return null;
            }
        }

        public static async Task<Player> GetPlayerRoom(HttpClient http, int playerId)
        {
            var response = await http.GetAsync($"api/Room/GetPlayerRoom/{playerId}");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Player>(content);
            }
            else
            {
                return null;
            }
        }
        public static async Task<bool> StartGame(HttpClient http, int roomId, int playerId)
        {
            var response = await http.PostAsync($"api/Room/StartGame/{roomId}/{playerId}", null);
            return response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public static async Task<PlayerAndRoomId> CreateRoom(MyClientWebSocket ws, int gameId, string playerName)
        {
            if (playerName.Length > ushort.MaxValue)
            {
                return null;
            }
            WSMessageHelper helper = new WSMessageHelper(MessageType.CreateRoom, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddIntBytes(bytes, gameId);
            MessageExtensions.AddStringBytes(bytes, playerName);

            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if (response.response == null || response.error) { return null; }

            int playerId = MessageExtensions.GetNextInt(ref response.response);
            int roomId = MessageExtensions.GetNextInt(ref response.response);
            return new PlayerAndRoomId(playerId, roomId);
        }

        public static async Task<(int? playerId, bool noRoom)> JoinRoom(MyClientWebSocket ws, int roomId, string playerName)
        {
            if (playerName.Length > ushort.MaxValue)
            {
                return (null, false);
            }
            WSMessageHelper helper = new WSMessageHelper(MessageType.JoinRoom, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddIntBytes(bytes, roomId);
            MessageExtensions.AddStringBytes(bytes, playerName);
            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if (response.response == null) { return (null, false); }
            if (response.error)
            {
                bool noRoom = MessageExtensions.GetNextBool(ref response.response);
                return (null, noRoom);
            }
            else
            {
                int playerId = MessageExtensions.GetNextInt(ref response.response);
                return (playerId, false);
            }
        }

        public static async Task StartGame(MyClientWebSocket ws, int roomId, int playerId)
        {
            WSMessageHelper helper = new WSMessageHelper(MessageType.StartGame, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddIntBytes(bytes, roomId);
            MessageExtensions.AddIntBytes(bytes, playerId);
            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
        }

        public static async Task<Player> ReJoin(MyClientWebSocket ws, int playerId)
        {
            WSMessageHelper helper = new WSMessageHelper(MessageType.ReJoin, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddIntBytes(bytes, playerId);
            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if (response.response == null || response.error) { return null; }

            int? roomId = MessageExtensions.GetNextInt(ref response.response);
            if(roomId < 0)
            {
                roomId = null;
            }
            int gameId= MessageExtensions.GetNextInt(ref response.response);
            bool isHost = MessageExtensions.GetNextBool(ref response.response);
            bool roomOpen = MessageExtensions.GetNextBool(ref response.response);
            string name = MessageExtensions.GetNextString(response.response);
            return new Player(playerId, name, roomId, gameId, isHost, roomOpen);
        }
    }
}
