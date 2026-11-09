# LinesOfSupport.Game

Cold launch opens the title with **New game** and **Continue**. New game offers **Spoke B** and the tutorial when its scenario is installed; preparation follows. Continue resumes the autosave without opening a file dialog. **Menu** returns to the title. The **Developer tools** toggle is off on every launch; opting in exposes the scenario catalog, approach replay and saved-file loader, plus a title-screen shortcut to the testing workspace. Turning it off closes the file dialog. The older panel descriptions below refer to that workspace while M2R's remaining opening/map revisions are built.

M1-05 shell for **OCS: Lines of Support**. Requires the **.NET 8 SDK** and **Godot 4.6 .NET** (tested with 4.6.1). The standard Godot download without .NET cannot run these C# scripts. No art, add-ons, export templates or Python packages are needed to open and run the game project.

From the repository root:

```sh
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --editor --path game
```

Replace `godot` with your Godot .NET executable path if it is not on PATH. Open `game/project.godot` directly in the editor as an alternative, build the C# project, then press **F5**. The editor uses `game/LinesOfSupport.Game.sln`, which includes the pure Sim project by reference. Display title is set once in `project.godot`; the shell reads it from project settings.

## Expected build

A default-theme desktop shell: the real Sim's next day, shop queue/capacity and cash gauges, a left schematic board in a SubViewport, and a right desk with nine tabs. Inbox shows the morning board; Requirements edits submitted scope cards; later desk work follows its backlog items. Use the tab-bar arrows to reach later tabs, or keyboard focus. The board/desk split can be dragged.

The Scenario picker lists folders under `scenarios/` containing `scenario.json`, preferring `_stub` initially. Choose **spoke-b** or **spoke-b-90** to see pre-deployment preparations in Board. Each choice shows its prep-day cost, with/without effects and whether the effect is modeled. The live budget check comes from `PrepScreen.Check`; over-budget picks disable **Deploy**. Deploy creates a seed-1 Session with exactly the selected choices, including an empty selection. No day can advance before deployment. The board reads existing islands, transport links and site IDs/names/opensDay. It draws shapes, mode-labeled lines and site markers. Parallel links are separated so both labels are readable. No textures or art assets are present.

Island positions are a deterministic screen layout based on authored array order, explicitly labeled as schematic rather than geography. No coordinates, fields or Sim behavior are invented. Read-only Game-local display records are not a simulation contract. Invalid JSON/references clear the old map and show an error; missing scenario folders disable the picker. Editor runs read `../scenarios/` relative to `game/`; exported builds read `scenarios/` beside the executable, independently of the working directory. The SubViewport can host a later 3D board without changing the desk.

**Next day** calls the real `Session.AdvanceDay`. With no scenario, or after the last day, it is disabled. The top strip binds `SessionView.TopStrip()` (ADR-0006): queued hours versus daily capacity, and cash versus the full balance. A shop queue longer than one day fills the bar; the label/tooltip retain the full queued hours/days. No capacity or cash rule is reimplemented in Game.

The Sim's Day is the day **about to be played**. After playing day 10 the strip says **Day 11 (next)**, while Inbox and the status line say **day 10 results**. At the end it says **Day 30 complete** (or 90). Inbox renders `MorningBoard().Items` with the Sim's category/severity and text, alerts first. It treats text as literal text, never markup. Site colors use the lowest capability tile's Sim-provided status: green/full, amber/degraded, red/down, grey/not open or unknown. Labels also name the condition, so color is not the only signal.

Choose an **Approach**, click **Replay**, then use **Next day** to step its authored input log. Replay can start directly from prep and uses the log's seed/prep. Sim validates the scenario/rules identity; each step submits that day's actions in recorded order through `Apply`, then calls `AdvanceDay`. Switching scenario clears the replay and returns to fresh, unchecked prep choices. The interactive mode offers Inbox decisions; award editing is available in Actions; automatic policy play is not present. All gameplay rules remain in Sim (ADR-0005).

## Morning board (M2-01)

The Board tab shows the Sim's morning items in alert-first order and its capability tiles. Linked items open their requirement, select their capability/site on the map, or select the matching arrangement in Ledger. The initial requirement/ledger destinations show real read-only context; their full editor/panel work follows in M2-03/M2-05. Items without a Sim link remain plain information. Event/island links focus their existing board text; cash/work links focus their ledger context.

**Continue** plays up to seven days, stopping after the first day with an Alert or Request, or at scenario completion. It uses the same day/replay path as Next day. Its tooltip states this behavior; it does not auto-decide requests. `MorningSmokeTest.tscn` verifies the stopping rule, a seven-day quiet fixture and the three primary link destinations. A day-11 screenshot is in the M2-01 handoff.

## Runtime smoke check

```sh
godot --headless --path game --editor --import --quit
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --headless --path game res://tests/ShellSmokeTest.tscn
godot --headless --path game res://tests/BoardSmokeTest.tscn
godot --headless --path game res://tests/SessionSmokeTest.tscn
```

Expected final line: `SHELL_SMOKE_PASS: nine tabs, board viewport, real gauges and day advance.` The scene instantiates the real main scene, visits all tabs, and emits the real Next day button signal. CI runs this from a clean GitHub checkout with Godot 4.6.1 .NET and .NET 8.

The board scene prints `BOARD_SMOKE_PASS` after checking folder discovery, authored labels and modes, picker switching, invalid JSON/reference handling and missing directories. Its test data is temporary and does not edit the authoritative scenarios.

The session scene prints `SESSION_SMOKE_PASS` after ten UI steps of Spoke B approach 1 exactly match `Replay.Run(..., stopAfterDay: 10)` by state hash: Spoke B power is down, its marker is red, gauges and Inbox match the views. It also checks finishing/disabling, scenario reset, the 90-day scenario and no-scenario handling. It can capture the day-10 Inbox screenshot with the screenshot argument below.

For a screenshot, run that test scene **without** `--headless` and append `-- --screenshot <absolute-output.png>`. Screenshots need a graphics display.

## Windows package (M0-13)

CI exports only on pushed `v*` tags, after the build, test, scenario, provenance and Godot checks pass. It uses Windows x86_64, .NET 8, **Godot 4.6.1 .NET** and the matching **4.6.1 .NET export templates** from the official Godot release. `export_presets.cfg` is tracked. Exporting does not require rcedit or a signing service.

To reproduce on Windows:

1. Install the .NET 8 SDK and Godot 4.6.1 .NET. Put `dotnet` on PATH.
2. In Godot choose **Editor > Manage Export Templates > Install from File** and select `Godot_v4.6.1-stable_mono_export_templates.tpz` from [the official 4.6.1 release](https://github.com/godotengine/godot-builds/releases/tag/4.6.1-stable). CI downloads and installs it automatically.
3. From the repo root, use PowerShell and choose an output directory that does not exist yet:

   ```powershell
   ./tools/export/windows.ps1 -Godot 'C:/path/to/Godot_v4.6.1-stable_mono_win64_console.exe' -OutputDirectory "$env:TEMP/LinesOfSupport-build"
   ```

The script builds, imports, exports, copies runtime data, checks the real exported main scene, then creates `LinesOfSupport-build.zip`. The check runs headlessly from a temporary working directory, verifies the scenario picker/map and referenced shared rules, and requires `EXPORT_SMOKE_PASS`. Release templates prohibit scene-path overrides, so this uses the explicit `-- --export-smoke-test` automation argument on the real main scene; normal launches show no test controls.

The zip contains `LinesOfSupport.exe`, its PCK, the Godot-generated self-contained .NET data directory, sibling scenario/theme/shared-rules JSON, and `scenarios/<folder>/golden/approach*.json` for Replay. Keep these together. No SDK or Godot installation is needed to run it. Reference sources, golden expected reports, validators, handoffs and kit experiment scenes are excluded. The packaged smoke check also replays approach 1 to day-10 power down from outside the checkout.

After an authorized `v*` tag is pushed, open the repository's **Actions > CI > the tag run > Artifacts**, then download `LinesOfSupport-<tag>-windows-x86_64`. Extract that artifact and the package zip inside it, and launch `LinesOfSupport.exe`. This work does not create a tag or publish a release. The package can step the schematic session and authored approach replays; there is no action UI yet.

## Commitment decisions (M2-02)

Inbox lists unresolved handshakes and out-of-scope work before decided history, with the original record text. Handshakes offer Ratify / Park / Refuse; out-of-scope work offers Deny / Ratify. Buttons send ResolveCommitmentAction and show the Sim response. Ratification uses the Sim-selected usable funding line; no price is entered. Parked items remain actionable, while other decisions retain their status/date.

A successful player action during a replay takes control of the existing session and stops future scripted actions. The Inbox warns before the decision, and the status line identifies Replay or Your decisions. Rejected actions do not stop the replay. Selecting the scenario or loading Replay again starts afresh. CommitmentSmokeTest exercises all five choices against direct Sim state hashes, messages and the next-day takeover behavior.

## Requirements (M2-03)

The Requirements tab lists only cards returned by RequirementEditor(card). Checkboxes send SetLineAction, without advancing time; the panel then reloads the Sim view. Missing-line warnings use GapIfOff verbatim. The dependency checklist shows none / in requirement / arranged and covering arrangements; peak demand and units are shown. There is no dollar entry. RequirementSmokeTest verifies edits against direct Sim hashes and checks the warning/checklist changes.

## Actions (M2-04)

Choose a submitted requirement or a site/capability, then instrument, vendor, known vehicle and funding line from ActionEditor. Unavailable instruments are disabled with their actual reason. Quantity, requested start and duration are numeric pickers; quotes, mobilization and totals are display-only. UCA NTE uses the engine default, never Game arithmetic.

Preview reruns on every change and is read-only: red hard stops block Award; amber soft gaps may be accepted. Cost, shop time, queue ahead and earliest-start range come directly from Preview. The start estimate explicitly ignores queue ahead. Inputs and preview scroll independently, and Award stays in the footer. The button sends exactly the previewed action; the response appears in the status line. ActionSmokeTest compares live previews and the final award to direct Sim state/results.

## Other desk actions (M2-06)

Board offers surveys for unsurveyed islands, showing their knowledge tier and pending state. Select a capability tile to coordinate a chosen quantity per day from another site; units come from its action view. Ledger rows offer Terminate, disabled while already queued. UCAs show proposal/definitization state and offer a funding picker plus Definitize once the proposal has arrived. All actions go to Sim and show ActionResult; no lag, capacity or financial consequence is computed here. OtherActionsSmokeTest compares all four commands to direct Sim actions on real fixtures.


## Save, load and resume (M3-08)

The scenario area has **Continue saved game**, **Load…** and **Save**. Deploy creates the first autosave; Next day and Continue write a new autosave each morning. Save also records decisions already made today, without advancing time. The file is `autosave.json` in Godot's user data directory (`user://`); the Save tooltip and success message show the full path. Writes use a temporary file in the same directory and replace the previous autosave only after the new file is written.

Continue saved game resumes that autosave. Load opens a filesystem chooser for any saved input-log JSON and selects the matching installed scenario. Both use `Replay.Resume` with the log's seed, preparations and actions. Resuming a saved authored replay gives control to the player: unplayed future scripted actions are not saved or resumed. Finished saves reopen their completed game/debrief. Invalid JSON, unavailable scenarios and incompatible rules give a status message and preserve the open game; an autosave error leaves the game playable and reports the failed write.

`SaveSmokeTest.tscn` tests the real buttons, morning-10 approach-1 state, cash and Spoke B power tile, same-day decisions, both autosave advance controls, loading another scenario's chosen file, finished games, invalid saves and failed writes. CI runs it in two separate Godot processes: `-- --save-path <file> --write-only`, then `-- --save-path <file> --resume-only`. With no arguments it performs both phases using temporary files in one process.

M2R-16 adds **Go to market** on a requirement. The market starts with no supplier selected; choose who, then how to arrange support. Exploring terms only previews the existing Sim quote, stops and gaps. **Arrange support** sends that exact action. Changing requirements resets the choices. Wider names and the illustrated map remain separate M2R items.
