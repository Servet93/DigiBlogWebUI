using Blog.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Blog.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IApiClient _apiClient;

        public HomeController(ILogger<HomeController> logger, IApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index(int? catId)
        {
            var model = new HomeViewModel();

            var getArticleResult = await _apiClient.GetAsync($"{Constants.ApiUri.Article}?catId={catId}&page=1&pageSize=25&fields=Id,Title,Summary,TitleImage,CreatedDate");

            if (getArticleResult.IsSuccessStatusCode)
            {
                var getArticleResponseModel = await getArticleResult.Content.ReadAsJsonAsync<GetArticleResponseModel>();
                model.Articles = getArticleResponseModel.Data;
            }

            return View(model);
        }

        public async Task<IActionResult> ArticleDetail(int id)
        {
            var model = new ArticleItem();

            var getArticleResult = await _apiClient.GetAsync($"{Constants.ApiUri.Article}/{id}");

            if (getArticleResult.IsSuccessStatusCode)
                model = await getArticleResult.Content.ReadAsJsonAsync<ArticleItem>();

            return View(model);
        }

    }
}
