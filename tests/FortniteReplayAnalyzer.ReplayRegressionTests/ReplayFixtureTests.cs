using System.Text.Json;
using FortniteReplayAnalyzer.Data;
using FortniteReplayReader;
using Xunit;
using Xunit.Sdk;
using Analyzer = FortniteReplayAnalyzer.FortniteReplayAnalyzer;

namespace FortniteReplayAnalyzer.ReplayRegressionTests;

public sealed class ReplayFixtureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    [Fact]
    public void ReplayFixturesParseAndMatchExpectations()
    {
        var replayFiles = ReplayFixturePaths.GetReplayFiles();

        if (replayFiles.Count == 0)
        {
            return;
        }

        var failures = new List<string>();

        foreach (var replayFile in replayFiles)
        {
            try
            {
                RunFixture(replayFile);
            }
            catch (Exception ex)
            {
                failures.Add($"{replayFile.DisplayName}\n{ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            throw new XunitException($"Replay fixture failures:{Environment.NewLine}{Environment.NewLine}{string.Join($"{Environment.NewLine}{Environment.NewLine}", failures)}");
        }
    }

    private static void RunFixture(ReplayFixtureFile replayFile)
    {
        var expectation = LoadExpectation(replayFile);
        var displayNames = expectation.DisplayNames ?? new Dictionary<string, string>();

        FortniteGame? game = null;
        var exception = Record.Exception(() =>
        {
            var replay = new ReplayReader().ReadReplay(replayFile.FullPath);
            game = new Analyzer().Analyze(replay, displayNames);
        });

        if (!string.IsNullOrWhiteSpace(expectation.ExpectException))
        {
            Assert.NotNull(exception);
            AssertExpectedException(expectation.ExpectException, exception!);
            return;
        }

        if (exception is not null)
        {
            throw new XunitException($"Unexpected exception while analyzing replay:{Environment.NewLine}{exception}");
        }

        Assert.NotNull(game);

        if (expectation.Expect is null)
        {
            return;
        }

        AssertExpectedGame(expectation.Expect, game!);
    }

    private static ReplayFixtureExpectation LoadExpectation(ReplayFixtureFile replayFile)
    {
        var expectationFile = ReplayFixturePaths.FindExpectationFile(replayFile);

        if (expectationFile is null)
        {
            return new ReplayFixtureExpectation();
        }

        try
        {
            return JsonSerializer.Deserialize<ReplayFixtureExpectation>(File.ReadAllText(expectationFile), JsonOptions)
                ?? new ReplayFixtureExpectation();
        }
        catch (JsonException ex)
        {
            throw new XunitException($"Could not parse expectation file '{expectationFile}': {ex.Message}");
        }
    }

    private static void AssertExpectedException(string expectedException, Exception exception)
    {
        var matches = string.Equals(exception.GetType().Name, expectedException, StringComparison.Ordinal)
            || string.Equals(exception.GetType().FullName, expectedException, StringComparison.Ordinal);

        Assert.True(matches, $"Expected exception '{expectedException}', but got '{exception.GetType().FullName}'.");
    }

    private static void AssertExpectedGame(ReplayGameExpectation expected, FortniteGame actual)
    {
        if (expected.Guid is not null)
        {
            Assert.Equal(expected.Guid, actual.Guid);
        }

        if (expected.PlayerCount.HasValue)
        {
            Assert.Equal(expected.PlayerCount.Value, actual.PlayerCount);
        }

        if (expected.RealPlayerCount.HasValue)
        {
            Assert.Equal(expected.RealPlayerCount.Value, actual.RealPlayerCount);
        }

        if (expected.BotCount.HasValue)
        {
            Assert.Equal(expected.BotCount.Value, actual.BotCount);
        }

        if (expected.PlatformStatistics is not null)
        {
            var actualStats = actual.PlatformStatistics ?? new Dictionary<string, int>();

            Assert.Equal(expected.PlatformStatistics.Count, actualStats.Count);

            foreach (var pair in expected.PlatformStatistics.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                Assert.True(actualStats.TryGetValue(pair.Key, out var count), $"Expected platform '{pair.Key}' to be present.");
                Assert.Equal(pair.Value, count);
            }
        }

        if (expected.WinningPlayerIds is not null)
        {
            Assert.Equal(expected.WinningPlayerIds, actual.WinningPlayerIds?.ToArray() ?? Array.Empty<int>());
        }

        if (expected.WinningDisplayNames is not null)
        {
            Assert.Equal(expected.WinningDisplayNames, actual.WinningDisplayNames?.ToArray() ?? Array.Empty<string>());
        }

        if (expected.EliminationCount.HasValue)
        {
            Assert.Equal(expected.EliminationCount.Value, actual.Eliminations?.Count ?? 0);
        }

        if (expected.BusRouteCount.HasValue)
        {
            Assert.Equal(expected.BusRouteCount.Value, actual.BusRouteRaw?.Count ?? 0);
        }

        if (expected.ContainsEliminations is not null)
        {
            var actualEliminations = actual.Eliminations ?? Array.Empty<FortniteElimination>();

            foreach (var expectedElimination in expected.ContainsEliminations)
            {
                var found = actualEliminations.Any(actualElimination => Matches(actualElimination, expectedElimination));

                Assert.True(found, $"Expected elimination was not found: {Describe(expectedElimination)}");
            }
        }
    }

    private static bool Matches(FortniteElimination actual, ReplayEliminationExpectation expected)
    {
        return Matches(actual.EliminatedBy, expected.EliminatedBy)
            && Matches(actual.Eliminated, expected.Eliminated);
    }

    private static bool Matches(FortnitePlayer actual, ReplayPlayerExpectation? expected)
    {
        if (expected is null)
        {
            return true;
        }

        if (expected.EpicId is not null && !string.Equals(expected.EpicId, actual.EpicId, StringComparison.Ordinal))
        {
            return false;
        }

        if (expected.Name is not null && !string.Equals(expected.Name, actual.Name, StringComparison.Ordinal))
        {
            return false;
        }

        if (expected.Platform is not null && !string.Equals(expected.Platform, actual.Platform, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static string Describe(ReplayEliminationExpectation expected)
    {
        return $"EliminatedBy={Describe(expected.EliminatedBy)}, Eliminated={Describe(expected.Eliminated)}";
    }

    private static string Describe(ReplayPlayerExpectation? expected)
    {
        if (expected is null)
        {
            return "*";
        }

        return $"epicId={expected.EpicId ?? "*"}, name={expected.Name ?? "*"}, platform={expected.Platform ?? "*"}";
    }

    private sealed class ReplayFixtureExpectation
    {
        public Dictionary<string, string>? DisplayNames { get; init; }

        public ReplayGameExpectation? Expect { get; init; }

        public string? ExpectException { get; init; }
    }

    private sealed class ReplayGameExpectation
    {
        public string? Guid { get; init; }

        public int? PlayerCount { get; init; }

        public int? RealPlayerCount { get; init; }

        public int? BotCount { get; init; }

        public Dictionary<string, int>? PlatformStatistics { get; init; }

        public int[]? WinningPlayerIds { get; init; }

        public string[]? WinningDisplayNames { get; init; }

        public int? EliminationCount { get; init; }

        public int? BusRouteCount { get; init; }

        public ReplayEliminationExpectation[]? ContainsEliminations { get; init; }
    }

    private sealed class ReplayEliminationExpectation
    {
        public ReplayPlayerExpectation? EliminatedBy { get; init; }

        public ReplayPlayerExpectation? Eliminated { get; init; }
    }

    private sealed class ReplayPlayerExpectation
    {
        public string? EpicId { get; init; }

        public string? Name { get; init; }

        public string? Platform { get; init; }
    }

    private sealed record ReplayFixtureSource(string Key, string Label, string RootPath);

    private sealed record ReplayFixtureFile(string SourceKey, string SourceLabel, string RootPath, string FullPath, string RelativePath)
    {
        public string DisplayName => $"{SourceLabel}/{RelativePath.Replace(Path.DirectorySeparatorChar, '/')}";
    }

    private static class ReplayFixturePaths
    {
        private const string FixtureDirectoryEnvironmentVariable = "FORTNITE_REPLAY_FIXTURES_DIR";

        private static readonly string ProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../"));

        private static readonly string TestsDirectory = Path.GetFullPath(Path.Combine(ProjectDirectory, ".."));

        private static readonly string ReplayExpectationsDirectory = Path.Combine(ProjectDirectory, "ReplayExpectations");

        public static IReadOnlyList<ReplayFixtureFile> GetReplayFiles()
        {
            var files = new List<ReplayFixtureFile>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in GetSources())
            {
                if (!Directory.Exists(source.RootPath))
                {
                    continue;
                }

                foreach (var replayFile in Directory.EnumerateFiles(source.RootPath, "*.replay", SearchOption.AllDirectories))
                {
                    var fullPath = Path.GetFullPath(replayFile);

                    if (!seenPaths.Add(fullPath))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(source.RootPath, fullPath);
                    files.Add(new ReplayFixtureFile(source.Key, source.Label, source.RootPath, fullPath, relativePath));
                }
            }

            return files
                .OrderBy(file => file.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string? FindExpectationFile(ReplayFixtureFile replayFile)
        {
            var overlayExpectation = Path.Combine(
                ReplayExpectationsDirectory,
                replayFile.SourceKey,
                Path.ChangeExtension(replayFile.RelativePath, ".json"));

            if (File.Exists(overlayExpectation))
            {
                return overlayExpectation;
            }

            if (string.Equals(replayFile.SourceKey, "submodules", StringComparison.Ordinal))
            {
                return null;
            }

            var sidecarExpectation = Path.ChangeExtension(replayFile.FullPath, ".json");

            if (File.Exists(sidecarExpectation))
            {
                return sidecarExpectation;
            }

            return null;
        }

        private static IReadOnlyList<ReplayFixtureSource> GetSources()
        {
            var overrideDirectories = GetOverrideDirectories();

            if (overrideDirectories.Count > 0)
            {
                return overrideDirectories
                    .Select((directory, index) => new ReplayFixtureSource($"external-{index + 1}", $"external-{index + 1}", directory))
                    .ToList();
            }

            return new List<ReplayFixtureSource>
            {
                new("local", "local", Path.Combine(ProjectDirectory, "ReplayFixtures")),
                new("submodules", "submodules", Path.Combine(TestsDirectory, "submodules"))
            };
        }

        private static IReadOnlyList<string> GetOverrideDirectories()
        {
            var overrideValue = Environment.GetEnvironmentVariable(FixtureDirectoryEnvironmentVariable);

            if (string.IsNullOrWhiteSpace(overrideValue))
            {
                return Array.Empty<string>();
            }

            return overrideValue
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(path => Path.GetFullPath(Environment.ExpandEnvironmentVariables(path)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
