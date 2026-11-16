# Claude plays the game (PLAY milestone rows)

Nick, 16 Nov 2026, in chat: Claude should play the game itself and be more critical of it than he would be, so the work is not waiting on him. He still plays it from time to time, and his play stays the gate. He also asked that this not fill up storage on his computer.

## 1. What it is for

A daily cold-start play of the tutorial by a player who has never seen the game, played on the real build, one click at a time, from what is on the screen. It finds what Nick found on 10 and 12 Nov (what am I looking at, what do I do next, two buttons that seem to do the same thing, a dead end) before he plays, so each of his plays starts from a build that has already been through one.

It cannot judge motion, sound, timing, how the mouse feels, or whether the game is fun. A model is also an odd first-time player: patient, it reads every word, and it never gets bored. Findings that come only from that are sorted out (section 5).

## 2. When

Once a day: the first scheduled Fable run on or after 14:00 UTC whose date has no `docs/reviews/YYYY-MM-DD-claude-play.md` yet, and only when `game/`, `src/` or `scenarios/` changed on `main` since the last play. Otherwise the run skips it. Not before PLAY-01 (the driver) is merged.

## 3. The player

The player is a fresh agent (the Agent tool, general-purpose), not the Fable session that runs it, because Fable knows what each button is meant to do and would play the design's intent, not the screen. It gets only the brief below and the driver; it does not read the repository, the design docs or the backlog.

The brief, word for word:

> You are playing a strategy game called Lines of Support for the first time. You are the kind of player it is made for: you know government contracting in general, but you have never seen this game, its rules or its screens. Play the Tutorial from New game. You act only through `tools/play/step.sh "<command>"`, which does one thing on the screen and prints the path of a screenshot and a text file describing what is now on screen; look at both after every step. Commands: `click "<label>"` (add `#2` for the second of two with the same label), `type "<field>" "<text>"`, `scroll "<pane>" up` or `down`, `key Escape`, `look`. Do not open any other file. Play to win, as far as day 20 or until you have been stuck for ten steps in a row. Keep notes as you go, in plain words, each with the day and the screen: every moment you did not know what you were looking at, what a button would do, or what to do next; anything that contradicted something else on screen; anything you did that did not do what you expected; and the moment you stopped, if you stopped. At the end, answer: who were you, what were you trying to do, did you manage it, and what one change would have helped most. Be blunt; praise nothing that did not earn it.

## 4. Running it

1. Godot 4.6.1 .NET under the authenticated TCP-loopback Xvfb display the smoke tests use, Dummy audio, built from `main`. Godot, the build and every screenshot go in the session's scratch directory, never in the repository.
2. Start the driver (PLAY-01) on the tutorial with a seed (the date as YYYYMMDD), then hand the player the brief.
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

## 7. Storage

Nick asked that this not fill up his computer. A clone of the repository is what reaches his computer, and git keeps every committed picture forever (the repository's history is already about 92 MB, half of it handoff screenshots). So:

- Screenshots from a play are never committed, except at most four per report that the report points at, plain PNG under 400 KB.
- Godot, the build output and the play's screenshots live in the session's scratch directory and are deleted at the end of the run.
- New Windows builds replace the old one: PLAYTEST.md says to delete the previous build's zip and folder when downloading a new one.
