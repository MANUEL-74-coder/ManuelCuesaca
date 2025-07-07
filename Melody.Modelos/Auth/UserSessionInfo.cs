using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.Auth
{
    public class UserSessionInfo
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();
        public string Token { get; set; } = "";

        // Helpers para verificar roles
        public bool IsArtista => Roles.Contains("artista");
        public bool IsAdmin => Roles.Contains("admin");
        public bool IsUserFree => Roles.Contains("userfree");
        public bool IsUserPremium => Roles.Contains("userpremium");
    }
}
