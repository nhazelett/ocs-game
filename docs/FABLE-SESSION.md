# Fable session workflow

Every Fable session follows this, whether Nick started it in chat or the schedule did (every three hours as of 9 Nov 2026, hourly before; tasks have been finishing inside 30 minutes). Scheduled runs start with no memory of earlier conversations: this file, the backlog, the handoff notes and the objection records are the memory. Set by Nick, 8 Nov 2026: keep moving unless truly stalled on a decision only he can make. Codex runs on its own schedule following `docs/CODEX-SESSION.md`.

**Merges happen at check-ins (Nick, 8 Nov 2026, FOR-NICK #1).** Scheduled runs cannot merge: the unattended permission check refuses merges as "Merge Without Review". So a scheduled run never attempts a merge. It reviews open PRs (section 3) and records its verdict as a PR comment, builds and opens PRs, and stacks. When Nick messages Fable in chat, that session does the merge pass: rerun tests on the combined tree, check balance if the Sim or a scenario changed, merge in order.

## 1. Start
1. Pull `main`. Read the status line in `backlog/BACKLOG.md`, the latest two handoff notes in `docs/handoffs/`, and `backlog/QUESTIONS.md`.
2. **Merge pass** (section 3).
3. Answer any question to Fable in `backlog/QUESTIONS.md` and any objection record waiting on a Fable response in `docs/objections/`.

## 2. Work
1. Take the highest Fable item in `backlog/BACKLOG.md` that is not Done and not blocked. Order inside a milestone is Fable's call (DISSENT.md section 2); milestone scope is Nick's.
2. **Claim it** before starting: push an empty branch `fable/<item-id>`. If that branch already exists on the remote and was pushed in the last three hours, another Fable session has it; take the next item.
3. Build it to the spec and the ADRs, with tests. `dotnet test src/LinesOfSupport.sln`, `python3 tools/validate/validate.py` and `python3 tools/validate/provenance.py` pass before a PR.
4. Open the PR (template, Objections section) and wait for CI. In a chat session with Nick, merge it when green; in a scheduled run, leave it open and stack the next item on it. Stop starting new Sim or scenario code at ten unmerged Fable PRs: do review, backlog and design work instead until a check-in merges the queue.
5. **Merge pass** again (section 3), then the next item. Repeat until the backlog has nothing unblocked for Fable or the session's budget is spent.
6. If Fable's backlog is empty, add the next items from SPEC 11's milestone plan (Fable's lane only), then continue.

## 2b. Reviewing the game as a player (added after the M2 gate, 8 Nov 2026)
- A game PR (anything under `game/`) is reviewed by opening its screenshots (`docs/handoffs/images/*.png`, plain PNG, under 400 KB, not LFS). No screenshot of what changed: comment and do not merge.
- Before any milestone with a player-facing surface is called ready for Nick, write down what a first-time player sees in the first two minutes, from the screenshots, in the milestone review. If you cannot, it is not ready.
- While M2R is open, no new Sim routes or scenarios start (docs/design/opening-and-guidance.md 9). Fable's items are the M2R rows.

## 3. Merge pass (after every completed task, and at session start)

In a scheduled run this is a review pass: apply the rules below, and instead of merging leave a PR comment `Fable review: ready to merge` (or what blocks it). In a chat session with Nick, merge.
For each open Codex PR:
- Changed files all inside Codex's components (`src/LinesOfSupport.Harness*`, `game/`, `tools/`, `assets/`, `.github/`, `backlog/`, Codex handoff notes) and every CI check green: merge it.
- Conflicted only in `backlog/*.md` or `.github/workflows/ci.yml`: merge `main` into the Codex branch (never force-push), keep both sides, rerun the tests, push, wait for CI, merge, and leave a one-line PR comment saying what was resolved.
  Before committing a resolution, run `git grep -nE '^(<<<<<<< |=======$|>>>>>>> )'` and stop if it finds anything; a branch can already carry markers from an earlier bad merge, and CI does not check Markdown yet (M2-16). Never chain the commit after a resolver with `;`.
- Touches `src/LinesOfSupport.Sim*`, `scenarios/schema/`, `scenarios/rules/`, `docs/SPEC.md` or `docs/decisions/`: review it as a design change; merge if it is right, otherwise file an objection per `docs/DISSENT.md`.
- CI red: do not merge; comment with the failing check and leave it for Codex.
Merging only on green CI keeps Codex's queue (limit ten open PRs) moving between Nick's check-ins.

## 4. When blocked on Nick
Only design or priority questions go to Nick (DISSENT.md section 6). Add the question to `backlog/FOR-NICK.md` in the escalation format (question, options, cost of each, which are reversible, each agent's recommendation), mark the blocked item `Blocked on Nick (FOR-NICK #n)`, and move to the next unblocked item. Never wait idle. When Nick answers in chat, record the answer there and in the item.

## 5. End
1. Handoff note `docs/handoffs/YYYY-MM-DD-fable-<topic>.md` from the template, including "PRs reviewed this session" and "Expected look of this build".
2. Update the backlog status line (Fable owns it; Codex PRs update only their own rows), and mark merged Codex items Done.
3. The session's final message is short and plain: what merged, what Fable built, anything waiting in `backlog/FOR-NICK.md`. Scheduled runs send it as the run summary (push notification).

## Environment notes
- Repo: github.com/nhazelett/ocs-game. PRs through the REST API (`gh api repos/nhazelett/ocs-game/pulls ...`); GraphQL is unavailable in these sessions. Force-push is blocked: when a branch needs rewriting, push it under a new name and close the old PR.
- .NET 8 SDK: `curl -sSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /opt/dotnet`, then `export PATH=/opt/dotnet:$PATH`.
- Python checks need `pip install --break-system-packages jsonschema pyyaml`.
- Commit messages end with the attribution lines the session provides.
