using System;

namespace Blog.WebUI.Models
{
    /// <summary>
    /// WebApi Token Response Model
    /// </summary>
    public class TokenResponseModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
    }
}
