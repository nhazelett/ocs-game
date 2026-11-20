# Claude plays the game (PLAY milestone rows)

Nick, 16 Nov 2026, in chat: Claude should play the game itself and be more critical of it than he would be, so the work is not waiting on him. He still plays it from time to time, and his play stays the gate. He also asked that this not fill up storage on his computer.

## 1. What it is for

A cold-start play of the tutorial by a player who has never seen the game, played on the real build, one click at a time, from what is on the screen. It finds what Nick found on 10 and 12 Nov (what am I looking at, what do I do next, two buttons that seem to do the same thing, a dead end) before he plays, so each of his plays starts from a build that has already been through one.

It cannot judge motion, sound, timing, how the mouse feels, or whether the game is fun. A model is also an odd first-time player: patient, it reads every word, and it never gets bored. Findings that come only from that are sorted out (section 5).

## 2. When

Nick, 20 Nov 2026, in chat: plays run when they are needed and not when they are not; no fixed daily slot, and as many in a day as the work calls for. He approves every play scheduled this way in advance.

Every Fable run decides, after its merge pass, whether a play is needed. One is needed when any of these merged on `main` since the last play's build:

- a fix for a defect a play or Nick found on the tutorial's path, so the play checks the fix (a verification play);
- a change a player sees in the tutorial (anything under `game/`, or a Sim or scenario change to words, numbers or choices on screen) that no play has been through yet;
- a change to an action a play has not exercised (stopping, handing over, ending a contract): the play then uses a second brief, section 3b, alongside the first.

None is needed when only docs, the backlog, handoffs, tests, goldens or Sim internals no player sees changed, or when the last play's blocking defect has not been fixed yet (the player would stop at the same wall). Nick asking for one always makes one needed.

When a play is needed, the run plays it. If the run cannot (a fix it is waiting for is still in CI, or the run has no time left), it schedules a one-time Fable play session for when it can run, with a prompt that says which build to wait for, and says so in its summary; it never schedules a play that is not needed, and it removes a scheduled play that a later run already did. Reports keep the name `docs/reviews/YYYY-MM-DD-claude-play.md`; a second play the same day adds `-2`, and so on.

## 3. The player

The player is a fresh agent (the Agent tool, general-purpose), not the Fable session that runs it, because Fable knows what each button is meant to do and would play the design's intent, not the screen. It gets only the brief below and the driver; it does not read the repository, the design docs or the backlog.

The brief, word for word:

> You are playing a strategy game called Lines of Support for the first time. You are the kind of player it is made for: you know government contracting in general, but you have never seen this game, its rules or its screens. You are playing its Tutorial. The game is already open on the Tutorial's first screen (it was reached through New game, then Tutorial), so do not press Back on that first screen and do not go back to the title: play from the screen you are given. You act only through `tools/play/step.sh "<command>"`, which does one thing on the screen and prints the path of a screenshot and a text file describing what is now on screen; look at both after every step. Commands: `click "<label>"` (add `#2` for the second of two with the same label), `type "<field>" "<text>"`, `scroll "<pane>" up` or `down`, `key Escape`, `look`. Do not open any other file. Play to win, to the end of the Tutorial or until you have been stuck for ten steps in a row. Keep notes as you go, in plain words, each with the day and the screen: every moment you did not know what you were looking at, what a button would do, or what to do next; anything that contradicted something else on screen; anything you did that did not do what you expected; and the moment you stopped, if you stopped. At the end, answer: who were you, what were you trying to do, did you manage it, and what one change would have helped most. Be blunt; praise nothing that did not earn it.

## 3b. The second brief: a player who changes their mind

Used only when section 2 calls for it. The 20 Nov play found a defect that only a player who stopped a buy could reach, and the batch policies never stop, hand over or end anything. This player gets the section 3 brief word for word with one sentence added at the end:

> At least twice in the game, change your mind about something you have already done (a buy, who is doing a piece of work, or a contract) and use whatever the game offers to stop, hand over or replace it, then carry on playing to win.

It runs as its own play beside the first (at most two at once on a two-CPU machine) and gets its own section in the same report.

## 4. Running it

1. Godot 4.6.1 .NET under the authenticated TCP-loopback Xvfb display the smoke tests use, Dummy audio, built from `main`. Godot, the build and every screenshot go in the session's scratch directory, never in the repository.
2. Start the driver (PLAY-01) on the tutorial with a seed (the date as YYYYMMDD), then hand the player the brief. Give the player one file of its own to keep notes in, outside the repository, so its early notes survive a long play. Until DESK-67 and DESK-68 are merged, also tell it not to click Menu: both leave the game, and a second visit to the preparation screen has no way out (the 17 Nov review lost two plays to it). Run at most two games at once on a two-CPU machine; three starved the driver.
3. After the play, replay the driver's `input-log.json` through the harness (`run tutorial --log ...`) and read the debrief: a finding that is really a Sim result (a 0% that should not be one, a refusal that is wrong) is checked against the report.
4. Sim pass: `batch tutorial --seeds 200` with the three policies. Note anything where do-nothing or greedy beats textbook, or a measure no policy moves.

## 5. Sorting the findings

Fable sorts every note into one of:

- **Defect**: the game or the Sim does something wrong. A backlog row for the right owner.
- **Design**: the screen is right by its row but the player still could not follow it. A decision in the design doc, then rows, or FOR-NICK if only Nick can decide it.
- **Known**: already an open row or decision; link it, add nothing.
- **Model**: comes only from the player being a model (it read a long paragraph that a person would skip, it typed a label exactly). Listed, not acted on.

## 6. The report

`docs/reviews/YYYY-MM-DD-claude-play.md`: the build (commit), the seed, how far the player got and where it stopped; what it saw in the first two minutes; its notes in its own words, each with Fable's sort and the row it became; its four end answers; the Sim pass. The PR carries the report, the new rows and the command and input logs (small text, under `docs/reviews/plays/YYYY-MM-DD/`), and merges like any other Fable PR.

Nick is notified only when the play found a new defect or design finding; the first line is the most important one.

## 6b. A review on request

Nick can ask for more than the usual play (17 Nov 2026: "a thorough review of the gameplay"). Such a review may use more than one player, other briefs (a hasty player who makes mistakes on purpose; Spoke B played cold) and Fable's own pass on the Sim. Its report is `docs/reviews/YYYY-MM-DD-gameplay-review.md` with its logs under `docs/reviews/plays/YYYY-MM-DD-review/`, so the day's play still runs. The picture limit and the storage rules below apply to it as they do to a play.

## 7. Storage

Nick asked that this not fill up his computer. A clone of the repository is what reaches his computer, and git keeps every committed picture forever (the repository's history is already about 92 MB, half of it handoff screenshots). So:

- Screenshots from a play are never committed, except at most four per report that the report points at, plain PNG under 400 KB.
- Godot, the build output and the play's screenshots live in the session's scratch directory and are deleted at the end of the run.
- New Windows builds replace the old one: PLAYTEST.md says to delete the previous build's zip and folder when downloading a new one.
