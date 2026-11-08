# Fable session workflow

Every Fable session follows this, whether Nick started it in chat or the two-hour schedule did. Scheduled runs start with no memory of earlier conversations: this file, the backlog, the handoff notes and the objection records are the memory. Set by Nick, 8 Nov 2026: keep moving unless truly stalled on a decision only he can make. Codex runs on its own two-hour schedule, about an hour offset from Fable, following `docs/CODEX-SESSION.md`, so each Fable merge pass usually finds a fresh batch of Codex PRs.

## 1. Start
1. Pull `main`. Read the status line in `backlog/BACKLOG.md`, the latest two handoff notes in `docs/handoffs/`, and `backlog/QUESTIONS.md`.
2. **Merge pass** (section 3).
3. Answer any question to Fable in `backlog/QUESTIONS.md` and any objection record waiting on a Fable response in `docs/objections/`.

## 2. Work
1. Take the highest Fable item in `backlog/BACKLOG.md` that is not Done and not blocked. Order inside a milestone is Fable's call (DISSENT.md section 2); milestone scope is Nick's.
2. **Claim it** before starting: push an empty branch `fable/<item-id>`. If that branch already exists on the remote and was pushed in the last three hours, another Fable session has it; take the next item.
3. Build it to the spec and the ADRs, with tests. `dotnet test src/LinesOfSupport.sln`, `python3 tools/validate/validate.py` and `python3 tools/validate/provenance.py` pass before a PR.
4. Open the PR (template, Objections section), wait for CI, merge your own PR when green.
5. **Merge pass** again (section 3), then the next item. Repeat until the backlog has nothing unblocked for Fable or the session's budget is spent.
6. If Fable's backlog is empty, add the next items from SPEC 11's milestone plan (Fable's lane only), then continue.

## 3. Merge pass (after every completed task, and at session start)
For each open Codex PR:
- Changed files all inside Codex's components (`src/LinesOfSupport.Harness*`, `game/`, `tools/`, `assets/`, `.github/`, `backlog/`, Codex handoff notes) and every CI check green: merge it.
- Conflicted only in `backlog/*.md` or `.github/workflows/ci.yml`: merge `main` into the Codex branch (never force-push), keep both sides, rerun the tests, push, wait for CI, merge, and leave a one-line PR comment saying what was resolved.
- Touches `src/LinesOfSupport.Sim*`, `scenarios/schema/`, `scenarios/rules/`, `docs/SPEC.md` or `docs/decisions/`: review it as a design change; merge if it is right, otherwise file an objection per `docs/DISSENT.md`.
- CI red: do not merge; comment with the failing check and leave it for Codex.
Merging only on green CI keeps Codex's queue (limit five open PRs) moving between Nick's check-ins.

## 4. When blocked on Nick
Only design or priority questions go to Nick (DISSENT.md section 6). Add the question to `backlog/FOR-NICK.md` in the escalation format (question, options, cost of each, which are reversible, each agent's recommendation), mark the blocked item `Blocked on Nick (FOR-NICK #n)`, and move to the next unblocked item. Never wait idle. When Nick answers in chat, record the answer there and in the item.

## 5. End
1. Handoff note `docs/handoffs/YYYY-MM-DD-fable-<topic>.md` from the template, including "PRs reviewed this session" and "Expected look of this build".
2. Update the backlog status line.
3. The session's final message is short and plain: what merged, what Fable built, anything waiting in `backlog/FOR-NICK.md`. Scheduled runs send it as the run summary (push notification).

## Environment notes
- Repo: github.com/nhazelett/ocs-game. PRs through the REST API (`gh api repos/nhazelett/ocs-game/pulls ...`); GraphQL is unavailable in these sessions. Force-push is blocked: when a branch needs rewriting, push it under a new name and close the old PR.
- .NET 8 SDK: `curl -sSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /opt/dotnet`, then `export PATH=/opt/dotnet:$PATH`.
- Python checks need `pip install --break-system-packages jsonschema pyyaml`.
- Commit messages end with the attribution lines the session provides.
