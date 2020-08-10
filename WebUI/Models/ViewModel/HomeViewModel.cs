using System.Collections.Generic;

namespace Blog.WebUI.Models
{
    public class HomeViewModel
    {
        public List<ArticleItem> Articles { get; set; } = new List<ArticleItem>();
    }
}
