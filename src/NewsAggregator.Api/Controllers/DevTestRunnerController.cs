using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NewsAggregator.Api.Models.DevTools;
using Swashbuckle.AspNetCore.Annotations;

namespace NewsAggregator.Api.Controllers;

/// <summary>Інструменти розробника: запуск xUnit через Swagger (лише Development).</summary>
[ApiController]
[Route("api/dev/tests")]
[SwaggerTag("Dev — запуск тестів")]
public class DevTestRunnerController(IWebHostEnvironment env, ILogger<DevTestRunnerController> logger)
    : ControllerBase
{
    [HttpPost("run")]
    [SwaggerOperation(
        Summary = "Запустити тести (dotnet test)",
        Description =
            "Повертає структурований звіт: **statusMessage**, **counts**, **outputTailLines**, повні тексти для копіювання. " +
            "Перед першим викликом виконай `dotnet build` у корені рішення. Використовується `--no-build`, щоб не блокувати .exe під час роботи API. " +
            "**scope=all** — усі тести (потрібен Docker). **scope=unit** — лише модульні, без контейнерів.",
        OperationId = "devTestsRun")]
    [SwaggerResponse(200, "Звіт про прогін тестів", typeof(TestRunResponse))]
    [SwaggerResponse(400, "Не знайдено тестовий проєкт", typeof(TestRunResponse))]
    [SwaggerResponse(404, "Не Development — ендпоінт вимкнено")]
    [Produces("application/json")]
    public async Task<ActionResult<TestRunResponse>> Run(
        [FromQuery]
        [SwaggerParameter(Description = "`all` — повний прогін (Docker). `unit` — лише NewsAggregator.Tests.Unit.")]
        string scope = "all",
        CancellationToken cancellationToken = default)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var testProjectPath = ResolveTestProjectPath();
        if (testProjectPath is null || !System.IO.File.Exists(testProjectPath))
        {
            return BadRequest(DevTestRunResponseBuilder.BuildNotFoundProject(
                scope,
                testProjectPath ?? "файл NewsAggregator.Tests.csproj не знайдено"));
        }

        var solutionRoot = Path.GetDirectoryName(testProjectPath)!;
        while (solutionRoot is not null && !System.IO.File.Exists(Path.Combine(solutionRoot, "NewsAggregator.sln")))
            solutionRoot = Directory.GetParent(solutionRoot)?.FullName;

        var workingDir = solutionRoot ?? Path.GetDirectoryName(testProjectPath)!;

        var args = new List<string> { "test", testProjectPath, "--nologo", "--no-build" };
        if (string.Equals(scope, "unit", StringComparison.OrdinalIgnoreCase))
            args.AddRange(["--filter", "FullyQualifiedName~NewsAggregator.Tests.Unit"]);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        logger.LogInformation("Dev test run: {Args}", string.Join(" ", args));

        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) stderr.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var reg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not kill test process on cancel.");
                }
            });

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            var outText = stdout.ToString();
            var errText = stderr + "\nСкасовано клієнтом.";
            return Ok(DevTestRunResponseBuilder.Build(
                false,
                -1,
                scope,
                testProjectPath,
                (int)sw.ElapsedMilliseconds,
                outText,
                errText));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start dotnet test");
            var outText = stdout.ToString();
            var errText = stderr + "\n" + ex;
            return Ok(DevTestRunResponseBuilder.Build(
                false,
                -1,
                scope,
                testProjectPath,
                (int)sw.ElapsedMilliseconds,
                outText,
                errText));
        }

        sw.Stop();
        var exit = process.ExitCode;
        var ok = exit == 0;

        return Ok(DevTestRunResponseBuilder.Build(
            ok,
            exit,
            scope,
            testProjectPath,
            (int)sw.ElapsedMilliseconds,
            stdout.ToString(),
            stderr.ToString()));
    }

    private string? ResolveTestProjectPath()
    {
        var apiRoot = env.ContentRootPath;
        return Path.GetFullPath(Path.Combine(apiRoot, "..", "..", "tests", "NewsAggregator.Tests", "NewsAggregator.Tests.csproj"));
    }
}
