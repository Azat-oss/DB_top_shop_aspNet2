using System.ComponentModel.DataAnnotations;

namespace DB_top_shop_aspNet.Models
{
    public class User
    {
        public enum Roles
        {
            User,
            Manager,
            Admin
        }

        [Key]
        public int Id { get; set; }

        [Display(Name = "Логин")]
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(10, MinimumLength = 5, ErrorMessage = "Имя должно быть от 5 до 10 символов")]
        public String UserName { get; set; } = String.Empty;

        [Display(Name = "Пароль")]
        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Пароль должен быть от 1 до 10 символов")]
        // автоматически рендерится как <input type="password"> c tag-helpers
        [DataType(DataType.Password)]
        public String Password { get; set; } = String.Empty;

        [Display(Name = "Роль")]
        public Roles Role { get; set; } = Roles.User;

        public override String ToString()
        {
            return $"Имя: {UserName} Роль: {Role}";
        }
    }
}
