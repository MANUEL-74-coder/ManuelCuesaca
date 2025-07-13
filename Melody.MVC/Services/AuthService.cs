using Melody.Modelos.DTOs;
using System.Text;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Melody.Modelos.Auth;
using System.Net.Http.Headers;

namespace Melody.MVC.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // Métodos públicos simplificados
        public async Task<AuthResponse> LoginAsync(LoginDto model)
        {
            return await ProcesarAuthAsync(model, "ApiSettings:LoginEndpoint");
        }

        public async Task<AuthResponse> RegistrarAsync(RegistroDto model)
        {
            return await ProcesarAuthAsync(model, "ApiSettings:RegistroEndpoint");
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordDto model)
        {
            return await ProcesarAuthAsync(model, "ApiSettings:ForgotPasswordEndpoint");
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordDto model)
        {
            return await ProcesarAuthAsync(model, "ApiSettings:ResetPasswordEndpoint");
        }

        // Método privado mejorado con HttpClientFactory y async
        private async Task<AuthResponse> ProcesarAuthAsync<T>(T item, string endpointConfigKey)
        {
            var endpoint = _configuration[endpointConfigKey];
            if (string.IsNullOrEmpty(endpoint))
                throw new InvalidOperationException($"Endpoint {endpointConfigKey} no configurado");

            using var client = _httpClientFactory.CreateClient();

            var json = JsonConvert.SerializeObject(item);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var resultado = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                return new AuthResponse
                {
                    IsSuccess = true,
                    Message = resultado?.mensaje?.ToString() ?? "Operación exitosa",
                    Token = resultado?.token?.ToString()
                };
            }
            else
            {
                var errorObj = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                var errores = new List<string>();
                string errorMessage = "Error en la operación";

                if (errorObj?.error != null)
                {
                    errorMessage = errorObj.error.ToString();
                }
                else if (errorObj?.errores != null)
                {
                    foreach (var error in errorObj.errores)
                        errores.Add(error.ToString());
                    errorMessage = string.Join(", ", errores);
                }

                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = errorMessage,
                    Errors = errores
                };
            }
        }
        public UserSessionInfo? GetCurrentUser()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            if (string.IsNullOrEmpty(token))
                return null;

            return new UserSessionInfo
            {
                UserId = _httpContextAccessor.HttpContext?.Session.GetString("UserId") ?? "",
                UserName = _httpContextAccessor.HttpContext?.Session.GetString("UserName") ?? "",
                Email = _httpContextAccessor.HttpContext?.Session.GetString("UserEmail") ?? "",
                Roles = _httpContextAccessor.HttpContext?.Session.GetString("UserRoles")?.Split(',').ToList() ?? new List<string>(),
                Token = token
            };
        }

        public bool IsAuthenticated()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                return jwt.ValidTo > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public void Logout()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }
        public string? ObtenerToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
        }

        public TokenInfo? ExtraerInfoToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                var claims = jsonToken.Claims.ToList();

                return new TokenInfo
                {
                    UserId = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value,
                    UserName = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value,
                    Email = claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email)?.Value,
                    Roles = claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList()
                };
            }
            catch
            {
                return null;
            }
        }
    }
}