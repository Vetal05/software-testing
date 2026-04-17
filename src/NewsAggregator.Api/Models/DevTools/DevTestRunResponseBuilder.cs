using System.Text.RegularExpressions;

namespace NewsAggregator.Api.Models.DevTools;

public static class DevTestRunResponseBuilder
{
    private const int MaxFullTextChars = 45_000;
    private const int TailLineCount = 50;

    public static TestRunResponse Build(
        bool success,
        int exitCode,
        string scope,
        string testProjectPath,
        int durationMs,
        string stdout,
        string stderr)
    {
        var counts = TryParseVstestCounts(stdout);
        var summaryLine = FindVstestSummaryLine(stdout);
        var status = BuildStatusMessage(success, exitCode, counts);
        var (fullOut, truncOut) = TruncateText(stdout, MaxFullTextChars);
        var (fullErr, truncErr) = TruncateText(stderr, MaxFullTextChars);

        return new TestRunResponse(
            Success: success,
            StatusMessage: status,
            Counts: counts,
            VstestSummaryLine: summaryLine,
            ExitCode: exitCode,
            Scope: scope,
            DurationFormatted: FormatDuration(durationMs),
            DurationMs: durationMs,
            TestProjectPath: testProjectPath,
            OutputTailLines: TakeTailLines(stdout, TailLineCount),
            ErrorLines: SplitNonEmptyLines(stderr),
            OutputTextTruncated: truncOut,
            ErrorTextTruncated: truncErr,
            StandardOutputText: fullOut,
            StandardErrorText: fullErr);
    }

    public static TestRunResponse BuildNotFoundProject(string scope, string? pathOrMessage) =>
        new(
            Success: false,
            StatusMessage: "Не знайдено тестовий проєкт або некоректна структура каталогів.",
            Counts: null,
            VstestSummaryLine: null,
            ExitCode: -1,
            Scope: scope,
            DurationFormatted: "0 мс",
            DurationMs: 0,
            TestProjectPath: pathOrMessage ?? "(невідомо)",
            OutputTailLines: Array.Empty<string>(),
            ErrorLines: new[] { "Очікується шлях …/tests/NewsAggregator.Tests/NewsAggregator.Tests.csproj відносно ContentRoot API." },
            OutputTextTruncated: false,
            ErrorTextTruncated: false,
            StandardOutputText: "",
            StandardErrorText: pathOrMessage ?? "");

    private static TestRunCounts? TryParseVstestCounts(string stdout)
    {
        var m = Regex.Match(
            stdout,
            @"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)",
            RegexOptions.Multiline);
        if (!m.Success)
            return null;

        return new TestRunCounts(
            Failed: int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
            Passed: int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
            Skipped: int.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture),
            Total: int.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static string? FindVstestSummaryLine(string stdout)
    {
        var lines = stdout.Replace("\r\n", "\n").Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var t = lines[i].Trim();
            if (t.StartsWith("Passed!", StringComparison.Ordinal) || t.StartsWith("Failed!", StringComparison.Ordinal))
                return t;
        }

        return null;
    }

    private static string BuildStatusMessage(bool success, int exitCode, TestRunCounts? c)
    {
        if (c is not null)
        {
            if (success && c.Failed == 0)
                return $"Успіх: усі {c.Total} тестів пройдено.";

            if (c.Failed > 0)
                return $"Є провали: {c.Failed} з {c.Total} (успішно {c.Passed}, пропущено {c.Skipped}).";
        }

        return success
            ? $"Процес завершено успішно (код виходу {exitCode})."
            : $"Процес завершено з кодом {exitCode}. Переглянь standardOutputText та errorLines.";
    }

    private static string FormatDuration(int ms)
    {
        if (ms < 1000)
            return $"{ms} мс";
        return $"{ms / 1000.0:0.##} с";
    }

    private static (string text, bool truncated) TruncateText(string s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return ("", false);
        if (s.Length <= max)
            return (s, false);
        return (s[..max] + "\n\n… [обрізано для зручності відображення в Swagger]", true);
    }

    private static IReadOnlyList<string> TakeTailLines(string s, int count)
    {
        if (string.IsNullOrEmpty(s))
            return Array.Empty<string>();

        var lines = s.Replace("\r\n", "\n").Split('\n');
        if (lines.Length <= count)
            return lines;

        return lines[^count..];
    }

    private static IReadOnlyList<string> SplitNonEmptyLines(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Array.Empty<string>();

        return s.Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.None)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();
    }
}
