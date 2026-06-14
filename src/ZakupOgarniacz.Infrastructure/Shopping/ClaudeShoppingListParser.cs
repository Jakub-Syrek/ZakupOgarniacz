using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using ZakupOgarniacz.Core.Shopping;

namespace ZakupOgarniacz.Infrastructure.Shopping;

/// <summary>
/// Parser listy zakupów oparty o Claude (Anthropic). Używa structured output, żeby
/// dostać czysty JSON z pozycjami (fraza + ilość). Domyślny model: <c>claude-opus-4-8</c>.
/// </summary>
public sealed class ClaudeShoppingListParser : IShoppingListParser
{
    private const string SystemPrompt = """
        Jesteś parserem listy zakupów. Z polecenia użytkownika (po polsku) wyodrębnij pozycje do kupienia.
        Dla każdej pozycji zwróć `query` — zwięzłą frazę do wyszukania produktu w sklepie spożywczym
        (po polsku, forma podstawowa/mianownik, bez ilości i jednostek) — oraz `quantity`
        (liczba sztuk lub paczek; domyślnie 1, np. „3 bochenki ciemnego pieczywa” => quantity 3).
        Pomijaj wtrącenia typu „kup mi”, „poproszę”. Zwróć wyłącznie dane w wymaganym formacie.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, JsonElement> Schema = new()
    {
        ["type"] = ToElement("object"),
        ["properties"] = ToElement(new
        {
            items = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Fraza do wyszukania produktu (po polsku, mianownik, bez ilości)" },
                        quantity = new { type = "integer", description = "Ilość sztuk/paczek; domyślnie 1" },
                    },
                    required = new[] { "query", "quantity" },
                    additionalProperties = false,
                },
            },
        }),
        ["required"] = ToElement(new[] { "items" }),
        ["additionalProperties"] = ToElement(false),
    };

    private readonly ClaudeOptions _options;
    private readonly AnthropicClient? _client;

    public ClaudeShoppingListParser(ClaudeOptions options)
    {
        _options = options;
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _client = new AnthropicClient { ApiKey = options.ApiKey };
        }
    }

    public bool IsConfigured => _client is not null;

    public async Task<IReadOnlyList<ShoppingItem>> ParseAsync(string command, CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Brak klucza Anthropic API (sekcja Claude / ANTHROPIC_API_KEY).");
        }

        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = 1024,
            System = SystemPrompt,
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = Schema },
            },
            Messages = [new() { Role = Role.User, Content = command }],
        };

        var response = await _client.Messages.Create(parameters);

        var json = response.Content
            .Select(block => block.Value)
            .OfType<TextBlock>()
            .Select(text => text.Text)
            .FirstOrDefault() ?? "{}";

        var parsed = JsonSerializer.Deserialize<ParsedList>(json, JsonOptions);
        return (parsed?.Items ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i.Query))
            .Select(i => new ShoppingItem(i.Query!.Trim(), i.Quantity < 1 ? 1 : i.Quantity))
            .ToList();
    }

    private static JsonElement ToElement(object value) => JsonSerializer.SerializeToElement(value);

    private sealed class ParsedList
    {
        [JsonPropertyName("items")]
        public List<ParsedItem>? Items { get; set; }
    }

    private sealed class ParsedItem
    {
        [JsonPropertyName("query")]
        public string? Query { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }
}
