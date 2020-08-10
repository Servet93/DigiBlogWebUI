using Blog.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Blog.WebUI.Controllers
{
    public class AdminController : Controller
    {
        private readonly IApiClient apiClient;
        public AdminController(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Giriş Yapmak için kullanıcıların kullanacağı arayüz
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var tokenRequest = new { UserName = model.EmailOrUserName, Password = model.Password };
            var getTokenResult = await apiClient.PostAsJsonAsync(Constants.ApiUri.GetToken, tokenRequest);

            if (getTokenResult.IsSuccessStatusCode)
            {
                var tokenReponse = await getTokenResult.Content.ReadAsJsonAsync<TokenResponseModel>();
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.EmailOrUserName),
                    new Claim(Constants.Definitions.AccessTokenClaimName, tokenReponse.Token),
                    new Claim(Constants.Definitions.RefreshTokenClaimName, tokenReponse.RefreshToken),
                    new Claim(Constants.Definitions.TokenExpirationClaimName, tokenReponse.Expiration.ToString(System.Globalization.CultureInfo.CurrentCulture))
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties());

                return RedirectToAction("Article");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Article(int? id)
        {
            var model = new ArticleEditViewModel();

            if (id.HasValue)
            {
                var getArticleResult = await apiClient.GetAsync($"{Constants.ApiUri.Article}/{id}");

                if (getArticleResult.IsSuccessStatusCode) { 
                    var result = await getArticleResult.Content.ReadAsJsonAsync<ArticleItem>();
                    model.Body = result.Body;
                    model.Categories = result.Categories;
                    model.Title = result.Title;
                    model.Summary = result.Summary;
                    model.Id = result.Id;
                }
            }
            
            var getCategoriesResult = await apiClient.GetAsync($"{Constants.ApiUri.Category}");
            var getCategoriesResponseModel = new List<CategoryItem>();

            if (getCategoriesResult.IsSuccessStatusCode)
                getCategoriesResponseModel = await getCategoriesResult.Content.ReadAsJsonAsync<List<CategoryItem>>();

            if (id.HasValue)
            {
                ViewBag.Categories = getCategoriesResponseModel.Select(x => new SelectListItem()
                {
                    Selected = model.Categories.Contains(x.Id),
                    Value = x.Id.ToString(),
                    Text = x.Name,
                });
            }
            else
            {
                ViewBag.Categories = getCategoriesResponseModel.Select(x => new SelectListItem()
                {
                    Value = x.Id.ToString(),
                    Text = x.Name,
                });
            }
            

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ListArticle(ListArticleViewModel model)
        {
            ViewBag.PageSize = new List<SelectListItem>
            {
                new SelectListItem {Text = "10", Value = "10"},
                new SelectListItem {Text = "25", Value = "25"},
                new SelectListItem {Text = "50", Value = "50"}
            };

            var getArticleResult = await apiClient.GetAsync($"{Constants.ApiUri.Article}?page={model.CurrentPage}&pageSize={model.PerPage}&fields=Id,Title,CreatedDate,UpdatedDate");

            if (getArticleResult.IsSuccessStatusCode)
            {
                var getArticleResponseModel = await getArticleResult.Content.ReadAsJsonAsync<GetArticleResponseModel>();
                model.Articles = getArticleResponseModel.Data;
                model.Total = getArticleResponseModel.Total;
            }
            
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> CreateArticle(ArticleEditViewModel model)
        {
            var titleImageBase64 = string.Empty;
            if (model.TitleImage != null && model.TitleImage.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    model.TitleImage.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    titleImageBase64 = Convert.ToBase64String(fileBytes);
                    titleImageBase64 = $"data:{model.TitleImage.ContentType};base64, {titleImageBase64}";
                    // act on the Base64 data
                }
            }

            var saveArticleRequest = new { Id = model.Id, Title = model.Title, Summary = model.Summary, Body = model.Body, TitleImage = titleImageBase64, Categories = model.Categories };
            var saveArticleResult = await apiClient.PostAsJsonAsync(Constants.ApiUri.Article, saveArticleRequest);

            if (saveArticleResult.IsSuccessStatusCode)
                return RedirectToAction("ListArticle");

            return View("Article");
        }

        [Authorize]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var deleteArticleResult = await apiClient.DeleteAsync($"{Constants.ApiUri.Article}/{id}");
            return RedirectToAction("ListArticle");
        }
    }


}
