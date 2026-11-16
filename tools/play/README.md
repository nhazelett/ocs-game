# Play driver (PLAY-01)

The driver starts New game through the real title and scenario buttons, with Developer tools off. It sends GUI clicks to Godot's root viewport (including embedded dialogs and picker menus). It reads presented controls, fully visible label lines and map labels; it does not query Sim, hidden panels, metadata, tooltips or future events. The only session setup is the requested seed, whose normal-game default remains 1. The log is a copy of the game's ordinary autosave, including prep, team and dated actions.

Use Godot **4.6.1 .NET**, .NET 8 on PATH, and the smoke tests' authenticated TCP-loopback Xvfb display (`DISPLAY`, `XAUTHORITY`). Audio is Dummy. Start in a terminal that shares that display's network namespace:

```bash
export GODOT_BIN=/absolute/path/to/Godot_v4.6.1-stable_mono_linux.x86_64
export OCS_PLAY_DIR=/absolute/scratch/claude-play-20261116
tools/play/start.sh --scenario tutorial --seed 20261116 --out "$OCS_PLAY_DIR" &
```

The launcher copies game, src, assets and scenarios to `$OCS_PLAY_DIR/build`, builds/imports there, and isolates user settings below the same scratch directory. It never copies sources/. The output directory must be empty or new, absolute, and outside the checkout. Wait for `ready.json` (inspect `build.log` or `driver.log` if it never appears). The first screen is preparations. The scenario can also be `spoke-b`.

```bash
tools/play/step.sh 'look'
tools/play/step.sh 'click "Your team · choose names and faces"'
tools/play/step.sh 'type "Name" "Morgan" #2'
tools/play/step.sh 'click "Use these names and faces"'
tools/play/step.sh "click \"I'm ready to leave\""
tools/play/step.sh 'scroll "Requests" down'
tools/play/step.sh 'key Escape'
```

Each command prints exactly two paths, `NNNN.png` and `NNNN.txt`. Read both before the next command. Screenshots are native 1280 by 800. Text groups visible words and controls by pane, top to bottom, with enabled/greyed state, checkbox/field values and visible reason lines in place. Duplicate controls have `#1`, `#2`, etc.; scroll panes use their printed names. Click a picker first, then click one of its visible choices. Missing/off-screen labels leave the game unchanged and return nearby labels. Playback runs at its normal speed and settles on results. Type replaces a field's contents. Commands are serialized; a timeout is an uncertain outcome, so inspect the screen before repeating an action.

`input-log.json` updates after every screen once deployed. The runner can finish with `tools/play/step.sh 'quit'`; this also flushes the log. Closing the game normally preserves it. There is no deployment log if the player never left preparations. Replay with:

```bash
dotnet run --project src/LinesOfSupport.Harness -- run tutorial --log "$OCS_PLAY_DIR/input-log.json"
```

`commands.jsonl` records every command and its returned paths. Keep only the report's small text logs and at most four selected plain PNGs under 400 KB, as docs/PLAYTEST-CLAUDE.md requires. After the report, remove this specific play's scratch directory, including build, screenshots and settings; the driver deliberately leaves cleanup to the runner so the player can inspect the final screen.

Verification:

```bash
python3 -m unittest discover -s tools/play -p 'test_*.py'
# After the Game build/import, on the authenticated display:
"$GODOT_BIN" --path game --resolution 1280x800 --audio-driver Dummy res://tests/PlayDriverSmokeTest.tscn
```

The native smoke test follows a short GUI command list through a real helper action and day 1, checks modal blocking and Escape, duplicate text fields, popup selection, clipped words, the native image size, and exact state equality after replaying the resulting log. Its Sim oracle lives in the test, separately from the driver. This is a Linux player-screen test; Windows execution, sound, motion quality and human judgment remain outside its evidence.
