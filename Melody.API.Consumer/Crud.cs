using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Melody.API.Consumer
{
    public static class Crud<T>
    {
        public static string Endpoint { get; set; }

        public static List<T> GetAll()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(Endpoint).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<List<T>>(json);
                }
                else
                {
                    throw new Exception($"Error al obtener datos: {response.ReasonPhrase}");
                }
            }
        }
        public static async Task<bool> PutWithAuth(string action, object data, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{Endpoint}/{action}", content);
            return response.IsSuccessStatusCode;
        }
        public static async Task<List<T>> GetAllWithAuth<T>(string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(Endpoint);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(json);
            }
            else
            {
                throw new Exception($"Error al obtener datos: {response.ReasonPhrase}");
            }
        }
        public static async Task<T> GetByIdWithAuth<T>(int id, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{Endpoint}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            else
            {
                throw new Exception($"Error al obtener datos: {response.ReasonPhrase}");
            }
        }
        public static async Task<T> CreateWithAuth<T>(T item, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(Endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            else
            {
                throw new Exception($"Error al crear datos: {response.ReasonPhrase}");
            }
        }
        public static async Task<bool> Update<T>(int id, T item, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{Endpoint}/{id}", content);

            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> DeleteWithAuth(int id, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"{Endpoint}/{id}");
            return response.IsSuccessStatusCode;
        }
        public static async Task<bool> UpdateWithAuth(int id, MultipartFormDataContent formData, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsync($"{Endpoint}/{id}", formData);
            return response.IsSuccessStatusCode;
        }

        public static async Task<T> GetWithAuth(string campo, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{Endpoint}/{campo}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            throw new HttpRequestException($"Error: {response.StatusCode}");
        }

        public static async Task<List<T>> GetListWithAuth(string campo, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{Endpoint}/{campo}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            throw new HttpRequestException($"Error: {response.StatusCode}");
        }

        public static async Task<bool> PostWithFormData(MultipartFormDataContent formData, string token, string action = null)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = string.IsNullOrEmpty(action) ? Endpoint : $"{Endpoint}/{action}";
            var response = await client.PostAsync(url, formData);
            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> UpdateWithFormData(string campo, MultipartFormDataContent formData, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsync($"{Endpoint}/{campo}", formData);
            return response.IsSuccessStatusCode;
        }

        public static async Task<List<T>> GetWithQuery(string campo, string queryValue)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{Endpoint}/{campo}?q={queryValue}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            throw new HttpRequestException($"Error: {response.StatusCode}");
        }
        public static async Task<List<T>> GetWithQueryAuth(string campo, string queryValue, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{Endpoint}/{campo}?q={queryValue}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            throw new HttpRequestException($"Error: {response.StatusCode}");
        }
        public static async Task<T> PostWithAuth(string action, object data, string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{Endpoint}/{action}", content);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            throw new HttpRequestException($"Error: {response.StatusCode}");
        }
    }
}