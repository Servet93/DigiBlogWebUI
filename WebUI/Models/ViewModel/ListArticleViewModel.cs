using System;
using System.Collections.Generic;

namespace Blog.WebUI.Models
{
    public class ListArticleViewModel
    {
        public IEnumerable<ArticleItem> Articles { get; set; }
        public int PerPage { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int Total { get; set; }

        
        public int PageCount()
        {
            return Convert.ToInt32(Math.Ceiling(Total/ (double)PerPage));
        }
    }
}
