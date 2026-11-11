**Work session handoff**

Fable, scheduled run, 11 Nov 2026 around 19:00 UTC.

Expected look of this build and why: no new screen. On the team panel and in the "hand this to" picker, hours now round up to the half hour instead of the whole hour. The tutorial's finance trip reads 2.5 hours for MSgt Joan Avery, 3 for you and 4 for SrA Eli Novak (it read 3, 3 and 4, so the faster teammate looked no faster than you). Her bar and "free hours today" follow: 5.5 free after the trip, not 5. The end-of-day check says "MSgt Joan Avery has 5.5 free hours today."

## PRs reviewed this session

- None open at the start of the run: the 11 Nov check-in merged the whole queue (PR 178). No Codex PR to review.

## Changed

- `TeamMemberView.HoursPerDay`, `HoursSpokenForToday`, `FreeHoursToday`, `HoursQueued`, `TeamWorkView.HoursLeft` and `SessionView.HoursIfAssigned` are now `double`, rounded up to the half hour (`Session.HalfHours`). The game project builds unchanged against them (checked with `dotnet build game/LinesOfSupport.Game.csproj`): every use is string interpolation or a `ProgressBar` value.
- `Session.EndOfDay.cs`: the two teammate sentences format hours with `0.#`, invariant culture.
- `Policies.Divide` uses the ceiling of the half-hour figures, which is exactly the old whole-hour figure, so no policy hands work out differently.
- New test `TeamTests.A_teammates_pace_shows_in_the_hours_rounded_to_the_half_hour`; the hand-over test's expected hours follow the new rounding.
- Backlog: DESK-14 done; status line.

## Why

DESK-14, found reviewing DESK-22's screenshot ("MSgt Robin Hayes · 3 hours at their pace" for a 3-hour task). D2 says the experienced CCO gets through work faster; the screen has to show it.

## Validated how

`dotnet test src/LinesOfSupport.sln` (573 Sim, 121 Harness; goldens unchanged), `python3 tools/validate/validate.py`, `python3 tools/validate/provenance.py`, `pytest tools/validate` (72), game project build.

## Unresolved

- For Codex, not blocking: the game interpolates these doubles with the machine's culture, so a German Windows would print "2,5". Formatting with `0.#` and `CultureInfo.InvariantCulture` on the team panel and picker would match the Sim's sentences. The Sim's other hour figures (top strip, previews) stay whole hours; only the team view and hand-over were in this row.

## Next

Nothing else unblocked for Fable before the DESK gate. Codex has DESK-27 and DESK-29.
