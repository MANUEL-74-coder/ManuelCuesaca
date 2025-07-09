using Melody.Modelos.DTOs;
using System.Text;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Melody.Modelos.Auth;

namespace Melody.MVC.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public AuthResponse Login(LoginDto model)
        {
            return ProcesarAuth(model, _configuration["ApiSettings:LoginEndpoint"]);
        }

        public AuthResponse Registrar(RegistroDto model)
        {
            return ProcesarAuth(model, _configuration["ApiSettings:RegistroEndpoint"]);
        }

        private AuthResponse ProcesarAuth<T>(T item, string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("Endpoint no configurado");
            }

            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(endpoint, content).Result;
                var jsonResponse = response.Content.ReadAsStringAsync().Result;

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
                    string errorMessage = "Error en la operación";
                    var errores = new List<string>();

                    if (errorObj?.error != null)
                    {
                        errorMessage = errorObj.error.ToString();
                    }
                    else if (errorObj?.errores != null)
                    {
                        foreach (var error in errorObj.errores)
                        {
                            errores.Add(error.ToString());
                        }
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