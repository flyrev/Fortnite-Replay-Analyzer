# Fortnite-Replay-Analyzer
The Ultimate Fortnite Replay Analyzer. Hosted at https://frenzy.farm

## Replay Regression Harness
- Initialize the bundled replay corpora with `git submodule update --init --recursive`.
- The harness scans both `tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayFixtures` and every `*.replay` under `tests/submodules`.
- For local fixtures, add a sibling `.json` file with the same basename to assert key fields such as player counts, winners, platform stats, and selected eliminations.
- For submodule fixtures, add overlay expectation files under `tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayExpectations/submodules/...` using the replay path under `tests/submodules`.
- Run the harness with `dotnet test tests/FortniteReplayAnalyzer.ReplayRegressionTests/FortniteReplayAnalyzer.ReplayRegressionTests.csproj --configuration Release`.
- To point the harness at a different folder or folders, set `FORTNITE_REPLAY_FIXTURES_DIR` before running `dotnet test`.

See `tests/FortniteReplayAnalyzer.ReplayRegressionTests/README.md` for the fixture format.
