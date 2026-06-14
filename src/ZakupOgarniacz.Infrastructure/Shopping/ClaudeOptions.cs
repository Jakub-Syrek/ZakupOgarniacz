namespace ZakupOgarniacz.Infrastructure.Shopping;

/// <summary>Konfiguracja parsera Claude (sekcja <c>Claude</c>; klucz trzymaj w user-secrets / env).</summary>
public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    /// <summary>Klucz Anthropic API. Domyślnie z <c>ANTHROPIC_API_KEY</c> (patrz DI).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Model. Domyślnie najnowszy Opus; do zmiany na tańszy w configu, jeśli chcesz.</summary>
    public string Model { get; set; } = "claude-opus-4-8";
}
