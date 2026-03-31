# Replay Regression Harness

The harness discovers replay files from two built-in sources:

- `tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayFixtures`
- every `*.replay` file under `tests/submodules`

Initialize the bundled corpora first:

```bash
git submodule update --init
```

You can still add your own fixtures to `tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayFixtures`.

- `match-1.replay`
- `match-1.json`

The `.json` file is optional. If it is missing, the harness still verifies that the replay can be parsed and analyzed without crashing.

## Expectation Locations

- Local fixtures can use a sibling `match-1.json` file next to `match-1.replay`.
- Submodule fixtures should use an overlay file under `tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayExpectations/submodules/...`.
- Some bundled legacy replays currently use overlays that assert `DecoderException`, because the current `FortniteReplayReader` dependency cannot decode the older `LZHLW` Oodle format.

Example overlay for `tests/submodules/xdeltax/FortniteReplayParser.NodeJS/REPLAYS/Minus2Bots.replay`:

```text
tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayExpectations/submodules/xdeltax/FortniteReplayParser.NodeJS/REPLAYS/Minus2Bots.json
```

## Expectation File

```json
{
  "displayNames": {
    "epic-id-1": "Player One"
  },
  "expect": {
    "guid": "optional-guid",
    "playerCount": 100,
    "realPlayerCount": 82,
    "botCount": 18,
    "platformStatistics": {
      "win": 60,
      "psn": 12
    },
    "winningPlayerIds": [17],
    "winningDisplayNames": ["Player One"],
    "eliminationCount": 81,
    "busRouteCount": 1,
    "containsEliminations": [
      {
        "eliminatedBy": {
          "epicId": "epic-id-1",
          "name": "Player One",
          "platform": "win"
        },
        "eliminated": {
          "epicId": "epic-id-2",
          "platform": "psn"
        }
      }
    ]
  }
}
```

Only the fields you include under `expect` are asserted.

You can also assert a failure case:

```json
{
  "expectException": "InvalidReplayException"
}
```

## Running It

```bash
dotnet test tests/FortniteReplayAnalyzer.ReplayRegressionTests/FortniteReplayAnalyzer.ReplayRegressionTests.csproj --configuration Release
```

To use a different fixture folder temporarily:

```bash
FORTNITE_REPLAY_FIXTURES_DIR=/path/to/replays dotnet test tests/FortniteReplayAnalyzer.ReplayRegressionTests/FortniteReplayAnalyzer.ReplayRegressionTests.csproj --configuration Release
```

You can provide multiple override roots by separating them with your platform path separator (`:` on macOS/Linux, `;` on Windows).
