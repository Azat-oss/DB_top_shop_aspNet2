using System.ComponentModel.DataAnnotations;

namespace DB_top_shop_aspNet.Models
{
    public record OrderDto
    {
        public int Id { get; init; }
        public DateTime Date { get; init; }
        public int ClientId { get; init; }
        public string? ClientName { get; init; }
        public int ProductId { get; init; }
        public string? ProductName { get; init; }
        public int Quantity { get; init; }

        public OrderDto(Order order)
        {
            Id = order.Id;
            Date = order.Date ?? DateTime.UtcNow;
            ClientId = order.ClientId;
            ClientName = order.Client?.Name;
            ProductId = order.ProductId;
            ProductName = order.Product?.Name;
            Quantity = order.Quantity;
        }
    }

    // DTO для создания/обновления
    public class CreateOrderDto
    {
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Клиент обязателен")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Товар обязателен")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, 1000, ErrorMessage = "Количество должно быть от 1 до 1000")]
        public int Quantity { get; set; }
    }

    public class UpdateOrderDto : CreateOrderDto { }

    // DTO для входа в API
    public class LoginApiDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }




}

