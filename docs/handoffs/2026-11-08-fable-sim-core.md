**Work session handoff**

Expected look of this build and why: still no game window. The simulation now plays Spoke B end to end in text. Read `scenarios/spoke-b/golden/*.expected.txt`: those are what the four approaches and the three policies produce, day by day, with every shortfall traced to its cause.

## Changed
- `src/LinesOfSupport.Sim`: deterministic simulation core (M0-05). Data model and loader with seeded variation; SplitMix64 streams per subsystem; integer-only outcome math; day loop (events, survey returns, UCA clocks, shop work, lifecycle, supply with surface-link delivery and buffers, capability dependencies, money and UCA obligations, mission score); actions `survey`, `setLine`, `award` (SF 44, GPC, purchase order, BPA call, UCA), `coordinate`, `definitize`, `terminate`; cause records on every change; flat metrics; state hash; input logs and replay; policies do-nothing, greedy, textbook; `SessionReport` text output.
- `scenarios/spoke-b/`: the first playable scenario (30-day cut), four approach logs and seven golden reports (M0-06).
- Schema and rules: capability scope lines and delivery type (seed of the card catalog), failure, work and UCA constants; site `initialBufferDays`; vendor `mobilizationFraction`, `qualifyingProposalLagDays`, `vehicleOnly`.
- `docs/decisions/ADR-0005-sim-api.md`; `docs/assumptions.md` extended; backlog M0-05 and M0-06 done, M1-01 (Codex) and M1-02 (Fable) added.
- Tests: 73 in `LinesOfSupport.Sim.Tests` (golden, determinism, replay, invariants across seeds and policies, balance, one test per rule).

## Why
M0-05 and M0-06. The spec's M1 acceptance asks that the Spoke B approaches reproduce their effects and that textbook beats greedy beats do-nothing. Both hold now.

## Validated how
- `dotnet test src/LinesOfSupport.sln`: Sim 73 passed, Harness 61 passed (unchanged).
- 200-seed balance run on Spoke B: do-nothing mission 0.666, power down 24 days, 0/200 pass; greedy median 0.806, power down 9 days, unauthorized commitment every seed, 0/200 pass; textbook median 0.936, power down 0, 187/200 pass. About 2 seconds for all 600 games.
- Approach logs reproduce SPEC 5's story: approach 1 fuel out day 10, power with it, unauthorized commitment day 11, storm day 16, growth shortfall day 20, maintenance breakdowns late; approach 2 holds until the coordination is withdrawn day 12, then storm, handshake day 19; approach 3 continuous; approach 4 dark days 7 to 16.

## Unresolved
- The 30-day cut cannot show the UCA overdue clock or funding expiry (they fall after day 30); M1-02 adds the 90-day cut.
- Approach 4 uses a UCA with no bridge instead of SPEC 5's external support request; external support arrives at M3.
- 13 of 200 textbook seeds fail a pass criterion (slow turnkey lead plus unreliable vendors). Intended ("the right decision can still bite"); worth a look in playtest.
- Event days for approaches are fixed in this scenario; later scenarios should use windows.

## Next
- Codex: M1-01, put the real Sim behind `run` and `batch` (ADR-0005).
- Fable: M0-07 view-model contracts for the morning board and the editors; then M1-02.
- Nick: read the four `approach*.expected.txt` files (or the summary in chat) and say whether the consequences land where a CCO would expect.
