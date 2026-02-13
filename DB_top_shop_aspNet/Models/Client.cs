using System.ComponentModel.DataAnnotations;

namespace DB_top_shop_aspNet.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Пожалуйста, укажите имя клиента.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пожалуйста, укажите email клиента.")]
        public string Email { get; set; } = string.Empty;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
