using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace NewsAggregator.Api.Models.DevTools;

/// <summary>Відповідь для Swagger: структурований результат прогону dotnet test.</summary>
public record TestRunResponse(
    [property: JsonPropertyOrder(0)]
    [property: SwaggerSchema(Description = "Чи процес dotnet test завершився з кодом 0.")]
    bool Success,

    [property: JsonPropertyOrder(1)]
    [property: SwaggerSchema(Description = "Короткий підсумок українською для швидкого перегляду.")]
    string StatusMessage,

    [property: JsonPropertyOrder(2)]
    [property: SwaggerSchema(Description = "Розпарсені лічильники vstest (Failed / Passed / Skipped / Total).")]
    TestRunCounts? Counts,

    [property: JsonPropertyOrder(3)]
    [property: SwaggerSchema(Description = "Останній рядок на кшталт «Passed! … Total: …» з консолі.")]
    string? VstestSummaryLine,

    [property: JsonPropertyOrder(4)]
    [property: SwaggerSchema(Description = "Exit code процесу (0 = успіх).")]
    int ExitCode,

    [property: JsonPropertyOrder(5)]
    [property: SwaggerSchema(Description = "Режим: all — усі тести (Docker); unit — лише модульні.")]
    string Scope,

    [property: JsonPropertyOrder(6)]
    [property: SwaggerSchema(Description = "Тривалість у зручному вигляді (наприклад «20,4 с»).")]
    string DurationFormatted,

    [property: JsonPropertyOrder(7)]
    [property: SwaggerSchema(Description = "Тривалість у мілісекундах.")]
    int DurationMs,

    [property: JsonPropertyOrder(8)]
    [property: SwaggerSchema(Description = "Абсолютний шлях до NewsAggregator.Tests.csproj.")]
    string TestProjectPath,

    [property: JsonPropertyOrder(9)]
    [property: SwaggerSchema(Description = "Останні рядки stdout (до 50) — зручно читати в JSON.")]
    IReadOnlyList<string> OutputTailLines,

    [property: JsonPropertyOrder(10)]
    [property: SwaggerSchema(Description = "Непорожні рядки stderr.")]
    IReadOnlyList<string> ErrorLines,

    [property: JsonPropertyOrder(11)]
    [property: SwaggerSchema(Description = "Чи обрізано standardOutputText.")]
    bool OutputTextTruncated,

    [property: JsonPropertyOrder(12)]
    [property: SwaggerSchema(Description = "Чи обрізано standardErrorText.")]
    bool ErrorTextTruncated,

    [property: JsonPropertyOrder(13)]
    [property: SwaggerSchema(Description = "Повний stdout для копіювання (може бути обрізаний).")]
    string StandardOutputText,

    [property: JsonPropertyOrder(14)]
    [property: SwaggerSchema(Description = "Повний stderr для копіювання (може бути обрізаний).")]
    string StandardErrorText);

/// <summary>Лічильники з фінального рядка виводу dotnet test.</summary>
public record TestRunCounts(
    [property: JsonPropertyOrder(0)]
    [property: SwaggerSchema(Description = "Провалені.")]
    int Failed,
    [property: JsonPropertyOrder(1)]
    [property: SwaggerSchema(Description = "Успішні.")]
    int Passed,
    [property: JsonPropertyOrder(2)]
    [property: SwaggerSchema(Description = "Пропущені.")]
    int Skipped,
    [property: JsonPropertyOrder(3)]
    [property: SwaggerSchema(Description = "Усього.")]
    int Total);
