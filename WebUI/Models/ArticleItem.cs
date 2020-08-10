using System;
using System.Collections.Generic;

namespace Blog.WebUI.Models
{
    public class ArticleItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string TitleImage { get; set; } 
        public string Body { get; set; }
        public List<int> Categories { get; set; }
    }
}
