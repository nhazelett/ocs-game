## Title
ADR-0004: The game is titled "OCS: Lines of Support"; code names are LinesOfSupport.*

## Status (Proposed / Accepted / Superseded)
Accepted

## Context
The working title FOOTPRINT was a placeholder. Nick wanted "OCS" in the name because the term is understood across the services.

## Options considered
"OCS: Lines of Support", "OCS: Hub and Spoke", "OCS Tempo".

## Decision
"OCS: Lines of Support". Repository slug stays `ocs-game`. Solution and projects: `LinesOfSupport.sln`, `LinesOfSupport.Sim`, `LinesOfSupport.Sim.Tests`, `LinesOfSupport.Harness`, `LinesOfSupport.Game`, `LinesOfSupport.Tools`.

## Consequences
One rename pass across docs and CI before any code exists (PR 2). The display title lives in one place in the game and can change without touching code names.

## Date
2026-11-07

## Author
Nick Hazelett; recorded by Fable
