namespace Blog.WebUI
{
    public class Constants
    {

        public class Configuration
        {
            public const string HttpClientBaseAddress = "ApiBaseAddress";
        }

        public class Definitions
        {
            public const string AccessTokenClaimName = "access_token";
            public const string TokenExpirationClaimName = "expires_at";
            public const string RefreshTokenClaimName = "refresh_token";
        }

        public class ApiUri
        {
            public const string GetToken = "authentication/login";
            public const string Article = "article";
            public const string Category = "category";
            public const string RefreshToken = "authentication/refreshtoken";
        }
    }
}
