using BCrypt.Net;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        // Поле только для формы (не сохраняется в БД)
        [NotMapped]
        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        public string? PasswordInput { get; set; }

        // Поле для хранения хеша в БД
        //[Required]
        [Column("Password")]
        public string PasswordHash { get; set; } = string.Empty;

        [Display(Name = "Роль")]
        public Roles Role { get; set; } = Roles.User;

        public void SetPassword(string plainPassword)
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        }

        public bool VerifyPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);
        }

        public override string ToString()
        {
            return $"Имя: {UserName} Роль: {Role}";
        }
    }
}
