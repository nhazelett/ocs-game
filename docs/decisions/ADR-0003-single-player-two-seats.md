## Title
ADR-0003: Single player holding two seats (OCS planner, CCO); hot-seat for groups; no network play before 1.0

## Status (Proposed / Accepted / Superseded)
Proposed (accepted when the spec v0.2 PR merges)

## Context
The brief asked whether the OCS and CCO layers should be played by one person switching roles, cooperating players, an instructor-led group, or a combination. Primary learner is the CCO (Nick, 7 Nov 2026). See SPEC.md section 3.

## Options considered
1. Networked co-op from the start.
2. One player, two seats, with hot-seat on one machine as the instructor-led group mode; a handoff file (OCS plan exported as scenario state) as a cheap later step; netcode as a stretch.
3. CCO-only game with the OCS layer scripted.

## Decision
Option 2. The SCO is a channel the CCO seat opens (and the OCS seat uses for joint structures), not a third seat.

## Consequences
- No lobby, sync or desync work before 1.0.
- The OCS seat stays small: requirements, priorities, external support requests, transitions, joint boards through the SCO.
- Instructor use works on day one with a projector.

## Date
2026-11-07

## Author
Fable, for Nick Hazelett
