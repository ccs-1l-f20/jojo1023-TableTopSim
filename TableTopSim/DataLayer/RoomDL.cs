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
        public static async Task<PlayerAndRoomId> CreatePlayerAndRoom(HttpClient http, string playerName, int gameId)
        {
            if(playerName == null || playerName.Length > 100)
            {
                return null;
            }
            var response = await http.PostAsync($"api/Room/CreatePlayerAndRoom/{playerName}/{gameId}", null);
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

        public static async Task<bool> StartGame(HttpClient http, int roomId, int playerId)
        {
            var response = await http.PostAsync($"api/Room/StartGame/{roomId}/{playerId}", null);
            return response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK;
        }
    }
}
