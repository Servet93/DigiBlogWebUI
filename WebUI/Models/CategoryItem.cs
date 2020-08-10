using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.WebUI.Models
{
    
    public class CategoryItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? ParentId { get; set; }

        public CategoryItem Parent { get; set; }

        public ICollection<CategoryItem> Children { get; set; }
    }
}
