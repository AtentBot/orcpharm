using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DTOs;

namespace Service;

public class OpenAIPrescriptionParserService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OpenAIPrescriptionParserService> _logger;

    public OpenAIPrescriptionParserService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAIPrescriptionParserService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key not configured");
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<PrescriptionOcrResultDto> ParsePrescriptionAsync(string imageBase64, string fileType)
    {
        try
        {
            _logger.LogInformation("Starting OCR parsing with OpenAI Vision API");

            var prompt = GetPrescriptionParsingPrompt();

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:{fileType};base64,{imageBase64}"
                                }
                            }
                        }
                    }
                },
                max_tokens = 1500,
                temperature = 0.1,
                response_format = new { type = "json_object" }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                requestBody
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"OpenAI API returned {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
            var jsonText = result!.Choices[0].Message.Content;

            _logger.LogInformation($"OCR completed. Response length: {jsonText.Length}");

            var ocrResult = JsonSerializer.Deserialize<PrescriptionOcrResultDto>(
                jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return ocrResult ?? new PrescriptionOcrResultDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OCR parsing");
            throw;
        }
    }

    private string GetPrescriptionParsingPrompt()
    {
        return @"
Você é um assistente especializado em ler prescrições médicas brasileiras de farmácia de manipulação.

Analise a imagem da receita e extraia as seguintes informações em formato JSON:

{
  ""doctor"": {
    ""name"": ""nome completo do médico"",
    ""crm"": ""número CRM"",
    ""rqe"": ""RQE se houver"",
    ""specialty"": ""especialidade se houver""
  },
  ""patient"": {
    ""name"": null,
    ""usage"": ""uso oral|uso tópico|uso local|uso externo""
  },
  ""prescriptionDate"": ""DD/MM/AAAA ou DD/MM/AA"",
  ""items"": [
    {
      ""lineNumber"": 1,
      ""rawText"": ""texto exato da linha como está escrito"",
      ""component"": ""nome do componente/matéria-prima"",
      ""quantity"": ""5 ou 15 ou 50"",
      ""unit"": ""%|g|mg|mL|qsp|null"",
      ""confidence"": 0.95
    }
  ],
  ""instructions"": ""posologia/modo de usar"",
  ""totalVolume"": ""50g|100mL|null se não especificado"",
  ""overallConfidence"": 0.85,
  ""warnings"": []
}

REGRAS IMPORTANTES:
1. Extraia TODAS as linhas da fórmula, mesmo que não estejam totalmente legíveis
2. Seja preciso com as concentrações e unidades (%, g, mg, mL)
3. Mantenha os nomes dos componentes EXATAMENTE como escritos (não normalize)
4. Identifique corretamente 'qsp' (quantidade suficiente para)
5. Se algo não estiver legível, extraia o que conseguir e marque confidence baixa (< 0.5)
6. Para linhas com espaços em branco (______), identifique o componente e marque quantity como quantidade indicada ou null
7. Não invente informações - se não conseguir ler, deixe null ou confidence baixa
8. Data pode estar em formato DD/MM/AAAA ou DD/MM/AA

ATENÇÃO ESPECIAL:
- 'O ureia' ou 'ureia' são variações comuns
- 'ácido salicílico' pode estar escrito de várias formas
- Nomes comerciais (Bepantol, etc) devem ser preservados
- 'creme base' ou 'creme base sem fragrância' são componentes completos

Retorne APENAS o JSON, sem texto adicional antes ou depois.
";
    }

    private class OpenAIResponse
    {
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();
    }

    private class Choice
    {
        public Message Message { get; set; } = new();
    }

    private class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}
