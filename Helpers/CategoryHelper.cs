using thuvienso.Models;

namespace thuvienso.Helpers
{
    public static class CategoryHelper
    {
        // Cho danh sách phẳng (admin, dropdown, list có thụt lề)
        public class CategoryTreeItem
        {
            public Category Category { get; set; } = null!;
            public int Level { get; set; }
            public bool Matched { get; set; } = false;
        }

        public static List<CategoryTreeItem> BuildTree(List<Category> all, int? parentId = null, int level = 0)
        {
            return all
                .Where(c => c.ParentId == parentId)
                .OrderByDescending(c => c.Id)
                .SelectMany(c =>
                {
                    var node = new CategoryTreeItem { Category = c, Level = level };
                    var children = BuildTree(all, c.Id, level + 1);
                    return new[] { node }.Concat(children);
                })
                .ToList();
        }

        // Cho menu dạng cây đệ quy (User)
        public class CategoryTreeNode
        {
            public Category Category { get; set; } = null!;
            public List<CategoryTreeNode> Children { get; set; } = new();
        }

        public static List<CategoryTreeNode> BuildNestedTree(List<Category> all, int? parentId = null)
        {
            return all
                .Where(c => c.ParentId == parentId)
                .OrderByDescending(c => c.Id)
                .Select(c => new CategoryTreeNode
                {
                    Category = c,
                    Children = BuildNestedTree(all, c.Id)
                })
                .ToList();
        }
    }
}
