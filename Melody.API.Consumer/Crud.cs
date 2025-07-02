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
        public static T GetById(int id)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync($"{Endpoint}/{id}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    throw new Exception($"Error al obtener datos: {response.ReasonPhrase}");
                }
            }
        }
        public static async Task<T> GetByCredentials(string correo, string contraseña)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Usar parámetros de query
                    var response = await client.GetAsync($"{Endpoint}/acceso?correo={Uri.EscapeDataString(correo)}&contraseña={Uri.EscapeDataString(contraseña)}");

                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Error al autenticar: {ex.Message}");
                }
            }
        }


        public static List<T> GetBy(string campo, int id)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync($"{Endpoint}/{campo}/{id}").Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<List<T>>(json);
                }
                else
                {
                    throw new Exception($"Error: {response.StatusCode}");
                }
            }
        }
        public static T Create(T item)
        {
            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(Endpoint, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<T>(jsonResponse);
                }
                else
                {
                    throw new Exception($"Error al crear datos: {response.ReasonPhrase}");
                }
            }
        }
        public static bool Update(int id, T item)
        {
            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PutAsync($"{Endpoint}/{id}", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    throw new Exception($"Error al actualizar datos: {response.ReasonPhrase}");
                }
            }
        }
        public static bool Delete(int id)
        {
            using (var client = new HttpClient())
            {
                var response = client.DeleteAsync($"{Endpoint}/{id}").Result;
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    throw new Exception($"Error al eliminar datos: {response.ReasonPhrase}");
                }
            }
        }

    }
}
