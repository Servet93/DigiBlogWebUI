using Blog.WebUI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Blog.WebUI.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly IApiClient _apiClient;
        public MenuViewComponent(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IViewComponentResult Invoke()
        {
            var getCategoriesResult = _apiClient.GetAsync($"{Constants.ApiUri.Category}").Result;
            var getCategoriesResponseModel = new List<CategoryItem>();

            if (getCategoriesResult.IsSuccessStatusCode)
                getCategoriesResponseModel = getCategoriesResult.Content.ReadAsJsonAsync<List<CategoryItem>>().Result.Where(x => x.ParentId == null).ToList();

            return View("Menu", getCategoriesResponseModel);
        } 
    }
}
