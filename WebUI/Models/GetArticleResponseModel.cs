using System.Collections.Generic;

namespace Blog.WebUI.Models
{
    public class GetArticleResponseModel
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ArticleItem> Data { get; set; }
    }
}
