using System.Text;
using System.Text.Json;

namespace Service.Notifications;

public class WhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "1D8F046A269F-4556-804C-0586245F169A";
    private readonly string _baseUrl = "https://api.atentbot.com/message/sendText/crescer";

    public WhatsAppService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool Success, string Message)> SendMessageAsync(string phoneNumber, string message)
    {
        try
        {
            var cleanNumber = CleanPhoneNumber(phoneNumber);
            
            var payload = new
            {
                number = cleanNumber,
                text = message
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);

            var response = await _httpClient.PostAsync(_baseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Mensagem enviada com sucesso");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Erro ao enviar mensagem: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao enviar mensagem via WhatsApp: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SendPasswordResetCodeAsync(string phoneNumber, string code, string employeeName)
    {
        var message = $"🔐 OrcPharm - Recuperação de Senha\n\n" +
                      $"Olá {employeeName}!\n\n" +
                      $"Seu código de recuperação é: *{code}*\n\n" +
                      $"Este código expira em 15 minutos.\n" +
                      $"Se você não solicitou esta recuperação, ignore esta mensagem.";

        return await SendMessageAsync(phoneNumber, message);
    }

    public async Task<(bool Success, string Message)> Send2FACodeAsync(string phoneNumber, string code, string purpose)
    {
        var purposeText = purpose.ToUpper() switch
        {
            "LOGIN" => "autenticação de login",
            "CONTROLLED_SUBSTANCE" => "autorização de substância controlada",
            _ => "verificação"
        };

        var message = $"🔐 OrcPharm - Código de Verificação\n\n" +
                      $"Código para {purposeText}: *{code}*\n\n" +
                      $"Este código expira em 5 minutos.\n" +
                      $"Nunca compartilhe este código.";

        return await SendMessageAsync(phoneNumber, message);
    }

    public async Task<(bool Success, string Message)> SendPasswordChangedNotificationAsync(string phoneNumber, string employeeName)
    {
        var message = $"🔐 OrcPharm - Senha Alterada\n\n" +
                      $"Olá {employeeName}!\n\n" +
                      $"Sua senha foi alterada com sucesso.\n\n" +
                      $"Se você não realizou esta alteração, entre em contato com o administrador imediatamente.";

        return await SendMessageAsync(phoneNumber, message);
    }

    private string CleanPhoneNumber(string phoneNumber)
    {
        var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        if (!cleaned.StartsWith("55") && cleaned.Length == 11)
        {
            cleaned = "55" + cleaned;
        }

        return cleaned;
    }
}
