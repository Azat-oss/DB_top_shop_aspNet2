using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Net.Sockets;

namespace DB_top_shop_aspNet.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;

        public int? StatusCode { get; private set; }
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string ErrorMessage { get; private set; } = "Произошла внутренняя ошибка сервера";
        public string? ShortErrorInfo { get; private set; }

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Обработка необработанных исключений
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                var ex = exceptionFeature.Error;
                _logger.LogError(ex, "Необработанное исключение на странице {Path}", exceptionFeature.Path);

                ShortErrorInfo = ex switch
                {
                    SocketException socketEx => GetFriendlySockMessage(socketEx),
                    SqlException sqlEx => GetFriendlySqlMessage(sqlEx),
                    FileNotFoundException => "Файл не найден.",
                    InvalidOperationException => "Некорректная операция.",
                    _ => "Произошла непредвиденная ошибка Не верный пароль!! должно быть Password=Passw0rd"
                };
            }

            // Обработка HTTP-статусов (404, 403 и т.д.)
            var statusCodeFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            if (statusCodeFeature != null)
            {
                StatusCode = int.Parse(HttpContext.Request.Query["statusCode"]);
                _logger.LogWarning("Статус-код {StatusCode} для пути {OriginalPath}", StatusCode, statusCodeFeature.OriginalPath);

                ShortErrorInfo = StatusCode switch
                {
                    404 => "Запрошенная страница не найдена!",
                    403 => "Доступ запрещён!",
                    429 => "Слишком много запросов. Попробуйте позже!",
                    500 => "Внутренняя ошибка сервера!",
                    _ => $"Ошибка HTTP {StatusCode}."
                };
            }
        }

        private string GetFriendlySqlMessage(SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                53 => "Не удалось подключиться к серверу базы данных. Проверьте доступность SQL Server.",
                4060 => "Ошибка доступа к базе данных. Проверьте параметры подключения.",
                18456 => "Ошибка авторизации в SQL Server.",
                _ => "Ошибка при работе с базой данных."
            };
        }

        private string GetFriendlySockMessage(SocketException socketEx)
        {
            return socketEx.ErrorCode switch
            {
                10061 => "Подключение не установлено: конечный компьютер отверг запрос.",
                _ => $"Ошибка сети: {socketEx.Message}"
            };
        }
    }

}
