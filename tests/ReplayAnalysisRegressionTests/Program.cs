using FortniteReplayAnalyzer.Controllers;
using FortniteReplayReader.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ReplayAnalyzer = FortniteReplayAnalyzer.FortniteReplayAnalyzer;

var failures = new List<string>();

await CheckAsync("Empty upload returns HTTP 400", async () =>
{
    var controller = new ReplayController(NullLogger<ReplayController>.Instance, null, null);
    var emptyReplay = new FormFile(Stream.Null, 0, 0, "replay", "empty.replay");

    var response = await controller.PostAsync(emptyReplay);

    Require(response is BadRequestObjectResult, "Expected an empty upload to return BadRequest.");
});

Check("Empty replay analysis is null-safe", () =>
{
    var analysis = new ReplayAnalyzer().Analyze(new FortniteReplay(), null);

    Require(analysis.PlayerCount == 0, "Expected an empty replay to contain zero players.");
    Require(analysis.RealPlayerCount == 0, "Expected an empty replay to contain zero real players.");
    Require(analysis.Eliminations is { Count: 0 }, "Expected eliminations to be an empty collection.");
    Require(analysis.WinningPlayerIds is not null && !analysis.WinningPlayerIds.Any(), "Expected winning player IDs to be empty.");
    Require(analysis.BusRouteRaw is { Count: 0 }, "Expected bus routes to be an empty collection.");
});

if (failures.Count > 0)
{
    throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
}

return;

void Check(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"FAIL: {name}: {ex.GetType().Name}: {ex.Message}");
    }
}

async Task CheckAsync(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"FAIL: {name}: {ex.GetType().Name}: {ex.Message}");
    }
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
