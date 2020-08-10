using Blog.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Blog.WebUI
{
    public interface IApiClient
    {
        Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data);
        Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T data);
        Task<HttpResponseMessage> GetAsync(string url, object data = null);
        Task<HttpResponseMessage> DeleteAsync(string url);
    }

    public class ApiClient : IApiClient
    {
        private string _currentToken;
        private string _refreshToken;

        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ClaimsIdentity _identity;

        public ApiClient(HttpClient httpClient,
            IConfiguration configuration,
            IIdentity identity,
            IHttpContextAccessor contextAccessor,
            IHostingEnvironment environment)
        {
            httpClient.BaseAddress = new Uri(configuration.GetValue<string>(Constants.Configuration.HttpClientBaseAddress));
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.Timeout = TimeSpan.FromSeconds(180);

            _client = httpClient;
            _contextAccessor = contextAccessor;
            _identity = identity as ClaimsIdentity;
        }

        public void AddToken()
        {
            var token = _identity
                ?.Claims.FirstOrDefault(x => x.Type == Constants.Definitions.AccessTokenClaimName)
                ?.Value;

            if (string.IsNullOrEmpty(token))
                return;

            _refreshToken = _identity
                ?.Claims.FirstOrDefault(x => x.Type == Constants.Definitions.RefreshTokenClaimName)
                ?.Value;

            _currentToken = token;

            if (_client.DefaultRequestHeaders.Contains("authorization"))
            {
                _client.DefaultRequestHeaders.Remove("authorization");
            }

            _client.DefaultRequestHeaders.Add("authorization", "bearer " + token);
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
        {
            AddToken();

            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _client.PostAsync(url, content);

            if (await ChallengeAuthorization(response))
            {
                AddToken();
                return await _client.PostAsync(url, content);
            }

            return response;
        }

        public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T data)
        {
            AddToken();

            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _client.PutAsync(url, content);

            if (await ChallengeAuthorization(response))
            {
                AddToken();
                return await _client.PutAsync(url, content);
            }

            return response;
        }

        public async Task<HttpResponseMessage> GetAsync(string url, object data = null)
        {
            AddToken();
            HttpResponseMessage response = await PrivateGetAsync(url, data);

            if (await ChallengeAuthorization(response))
            {
                AddToken();
                return await PrivateGetAsync(url, data);
            }

            return response;
        }

        private async Task<HttpResponseMessage> PrivateGetAsync(string url, object data)
        {
            HttpResponseMessage response;

            if (data != null)
            {
                var dataAsString = JsonConvert.SerializeObject(data);
                var content = new StringContent(dataAsString);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var uri = new Uri(url, UriKind.Relative);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = uri,
                    Content = content
                };

                response = await _client.SendAsync(request);
            }
            else
            {
                response = await _client.GetAsync(url);
            }

            return response;
        }

        /// <inheritdoc cref="IGatewayApiClient.DeleteAsync"/>
        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            AddToken();

            var response = await _client.DeleteAsync(url);

            if (await ChallengeAuthorization(response))
            {
                AddToken();
                return await _client.DeleteAsync(url);
            }

            return response;
        }

        private async Task<bool> ChallengeAuthorization(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (response.Headers.Any(x => x.Key == "Token-Expired" && x.Value.FirstOrDefault() == "true"))
                {
                    var dataAsString = JsonConvert.SerializeObject(new
                    {
                        Token = _currentToken,
                        RefreshToken = _refreshToken
                    });

                    var content = new StringContent(dataAsString);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var refreshTokenResponse = await _client.PostAsync(Constants.ApiUri.RefreshToken, content);

                    if (refreshTokenResponse.IsSuccessStatusCode)
                    {
                        // Removing expired token info from claims
                        var tokenClaim = _identity.Claims
                            .FirstOrDefault(x => x.Type == Constants.Definitions.AccessTokenClaimName);

                        var refreshTokenClaim = _identity.Claims
                            .FirstOrDefault(x => x.Type == Constants.Definitions.RefreshTokenClaimName);

                        var expirationTokenClaim = _identity.Claims
                            .FirstOrDefault(x => x.Type == Constants.Definitions.TokenExpirationClaimName);

                        _identity.RemoveClaim(tokenClaim);
                        _identity.RemoveClaim(refreshTokenClaim);
                        _identity.RemoveClaim(expirationTokenClaim);

                        // adding new token info
                        var tokenReponse = await refreshTokenResponse.Content.ReadAsJsonAsync<TokenResponseModel>();
                        var claims = new List<Claim>
                        {
                            new Claim(Constants.Definitions.AccessTokenClaimName, tokenReponse.Token),
                            new Claim(Constants.Definitions.RefreshTokenClaimName, tokenReponse.RefreshToken),
                            new Claim(Constants.Definitions.TokenExpirationClaimName, tokenReponse.Expiration.ToString(CultureInfo.CurrentCulture))
                        };

                        _identity.AddClaims(claims);
                        var identity = new ClaimsIdentity(_identity.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await _contextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties());

                        return true;
                    }
                }

                // Burada Refresh token'da başarılı sonuç gelmezse ne yapılacaksa o yapılmalı.
                await _contextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties());
                throw new AuthenticationException("Refresh Token Alınamadı!");
            }

            return false;
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            var dataAsString = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(dataAsString);
        }
    }
}

