using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DB_top_shop_aspNet.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите дату заказа")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]//добавил
        [Display(Name = "Дата заказа")]
        public DateTime? Date { get; set; } // ← СДЕЛАЙТЕ NULLABLE

        [Required(ErrorMessage = "Выберите клиента")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Выберите продукт")]
        public int ProductId { get; set; }

        [Range(1, 1000, ErrorMessage = "Количество должно быть от 1 до 1000")]
        public int Quantity { get; set; }

        public Client Client { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
