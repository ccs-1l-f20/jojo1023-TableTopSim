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
        public static async Task<PlayerAndRoomId> CreatePlayerAndRoom(HttpClient http, string playerName)
        {
            if(playerName == null || playerName.Length > 100)
            {
                return null;
            }
            var response = await http.PostAsync($"api/Room/CreatePlayerAndRoom/{playerName}", null);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PlayerAndRoomId>(content); ;
            }
            else
            {
                return null;
            }
        }

        public static async Task<int?> CreatePlayerInRoom(HttpClient http, string playerName, int roomId)
        {
            if (playerName == null || playerName.Length > 100)
            {
                return null;
            }
            var response = await http.PostAsync($"api/Room/CreatePlayerInRoom/{playerName}/{roomId}", null);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<int?>(content);
            }
            else
            {
                return null;
            }
        }

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

        public static async Task<PlayerAndRoomId> CreateRoom(MyClientWebSocket ws, string playerName)
        {
            if (playerName.Length > ushort.MaxValue)
            {
                return null;
            }
            WSMessageHelper helper = new WSMessageHelper(MessageType.CreateRoom, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddStringBytes(bytes, playerName);

            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if (response == null) { return null; }

            int playerId = MessageExtensions.GetNextInt(ref response);
            int roomId = MessageExtensions.GetNextInt(ref response);
            return new PlayerAndRoomId(playerId, roomId);
        }

        public static async Task<int?> JoinRoom(MyClientWebSocket ws, int roomId, string playerName)
        {
            if (playerName.Length > ushort.MaxValue)
            {
                return null;
            }
            WSMessageHelper helper = new WSMessageHelper(MessageType.JoinRoom, ws);

            var bytes = helper.StartBytes();
            MessageExtensions.AddIntBytes(bytes, roomId);
            MessageExtensions.AddStringBytes(bytes, playerName);
            await ws.SendMessageAsync(new ArraySegment<byte>(bytes.ToArray()));
            var response = await helper.GetResponse();
            if (response == null) { return null; }

            int playerId = MessageExtensions.GetNextInt(ref response);
            return playerId;
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
            if (response == null) { return null; }

            int? roomId = MessageExtensions.GetNextInt(ref response);
            if(roomId < 0)
            {
                roomId = null;
            }
            bool isHost = MessageExtensions.GetNextBool(ref response);
            bool roomOpen = MessageExtensions.GetNextBool(ref response);
            string name = MessageExtensions.GetNextString(response);
            return new Player(playerId, name, roomId, isHost, roomOpen);
        }
    }
}
