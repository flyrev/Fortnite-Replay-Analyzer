# Replay Expectation Overlays

Store expectation files for replay fixtures that live outside `ReplayFixtures`.

For bundled corpora from `tests/submodules`, mirror the replay path under `submodules/` and change the extension to `.json`.

Example:

- replay: `tests/submodules/xdeltax/FortniteReplayParser.NodeJS/REPLAYS/Minus2Bots.replay`
- expectation: `tests/FortniteReplayAnalyzer.ReplayRegressionTests/ReplayExpectations/submodules/xdeltax/FortniteReplayParser.NodeJS/REPLAYS/Minus2Bots.json`

The JSON schema matches the one documented in `tests/FortniteReplayAnalyzer.ReplayRegressionTests/README.md`.
