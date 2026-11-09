**Work session handoff**

Expected look of this build and why: no new visible change for M2R-12. This item records a missing display contract and stops without shipping an incomplete naming guarantee. Inherits PR 98's front door only.

## PRs reviewed this session

No open Codex PR at session start. Opened PR 98 for M2R-10; it is not merged by Codex. Read Fable's merged M2R-01 contract, including its explicit exclusion of named record/hard-stop/debrief wording.

## Changed

M2R-12 was claimed before inspection. Its Status now names Q14 and the remaining Fable text projection. This branch stacks on PR 98 so its future all-panel walk can test the normal player surface with developer tools off; only the metadata and this handoff are new for this item.

## Why

ADR-0006's M2R-01 addendum explicitly assigns internal ids in record, refusal and debrief text to M2R-07. A complete no-id guarantee needs those Sim-written messages plus human arrangement/vehicle/funding labels. A Game-side string search/replace cannot reliably distinguish an identifier from ordinary prose or a monetary amount; it also loses the source context. No such parser or alternative interface was quietly built.

## Validated how

Inspected the merged ADR addendum and the current views: PersonView and CardTitle are delivered, while stored record/refusal/debrief strings still contain contract, commitment and funding ids. Baseline inherited from PR 98: 481 .NET tests and scenario/provenance/conflict checks pass; clean game build and front-door/Morning/Save native checks. This item changes no game, Sim, scenario or ADR file.

## Unresolved

Q14 asks Fable to finish M2R-07 or supply a read-only named-message projection, and names for Game-owned contract/vehicle/funding rows. This is a contract dependency, not a design objection or Nick-only question. No FOR-NICK addition.

## Next

Move to the next unblocked M2R item. Named requests in Inbox can bind existing Person/Message/CardTitle fields now; remaining opening and lead rows wait on their listed Fable contracts. Return to M2R-12 after Q14 is answered.
