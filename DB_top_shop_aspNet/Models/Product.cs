using System.ComponentModel.DataAnnotations;

namespace DB_top_shop_aspNet.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Пожалуйста, укажите название продукта.")]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
