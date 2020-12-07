using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public static class GameDL
    {
        public static async Task<int?> AddGame(HttpClient http, GameDataDto gameData)
        {
            if (gameData == null || gameData.Name == null || gameData.Name.Length > 100)
            {
                return null;
            }
            var response = await http.PostAsJsonAsync($"api/Game/AddGame", gameData);

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
        public static async Task<bool> AddGameImages(HttpClient http, int gameId, Dictionary<int, ImageDto> images)
        {
            //try
            //{
            //var oldTo = http.Timeout;
            //http.Timeout = oldTo * 20;
            bool succsess = true;
            if (images.Count > 0)
            {
                bool first = true;
                foreach (var id in images.Keys)
                {
                    ImageDto image = images[id];
                    image.Id = id;
                    HttpResponseMessage response;
                    if (first)
                    {
                        response = await http.PostAsJsonAsync($"api/Game/AddGameImages/{gameId}", JsonConvert.SerializeObject(new Dictionary<int, ImageDto>() { { id, image } }));
                        first = false;
                    }
                    else
                    {
                        response = await http.PostAsJsonAsync($"api/Game/AddGameImage/{gameId}", JsonConvert.SerializeObject(image));
                    }
                    //http.Timeout = oldTo;
                    succsess = response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK;
                    if (!succsess)
                    {
                        break;
                    }
                }
            }
            return succsess;
            //}
            //catch(Exception e)
            //{
            //    Debug.WriteLine(e.Message);
            //    Debug.WriteLine(e.StackTrace);
            //    throw e;
            //}
        }
        public static async Task<bool> DeleteGame(HttpClient http, int gameId)
        {
            var response = await http.PostAsync($"api/Game/DeleteGame/{gameId}", null);
            return response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public static async Task<Dictionary<int, GameDataDto>> GetGames(HttpClient http)
        {
            var response = await http.GetAsync($"api/Game/GetGames");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<int, GameDataDto>>(content);
            }
            else
            {
                return null;
            }
        }
        public static async Task<GameDataDto> GetGame(HttpClient http, int gameId)
        {
            var response = await http.GetAsync($"api/Game/GetGame/{gameId}");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GameDataDto>(content);
            }
            else
            {
                return null;
            }
        }

        public static async Task<Dictionary<int, ImageDto>> GetGameImages(HttpClient http, int gameId)
        {
            var response = await http.GetAsync($"api/Game/GetGameImages/{gameId}");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<int, ImageDto>>(content);
            }
            else
            {
                return null;
            }
        }
    }
}
