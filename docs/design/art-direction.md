# Art direction brief (ART milestone)

Nick, 9 Nov 2026: "Let's put ChatGPT on the art styles. It can develop that stuff." Codex owns the look. This page is the brief: what the art has to do, the limits, and how a style gets chosen. Codex develops the style; Nick picks; Fable reviews each PR's screenshots as a first-time player.

## What Nick has said about the look
- Audience: a CCO alone at a desk. It is a training game that should feel like a game, not a tool.
- The map scrolls like Command and Conquer. No fog. "No bad graphics."
- He wants to see the potential for animation and 3D. On 8 Nov the build showed none.
- It must not look AI-made. No bullet lists in game text (R9). The same goes for art: no generic glossy generated look, no mismatched styles between screens.

## Where the game is today
Engine default grey panels and font. One grey silhouette for every person. Sites are beige boxes on blob islands. The day changes in one frame. No sound. No art files, theme, tween or shader anywhere in game/.

## Rules
1. Art never changes what the Sim says. Colors and motion come from the view models (MapDay, site state, records). No invented state.
2. State must read without color alone (shape or label too).
3. Text stays readable at 1280x800: body text no smaller than today.
4. Every asset file has a row in assets/PROVENANCE.md with Origin and License (CI checks it). Generated images record the tool and the prompt. No third-party art without a license that allows redistribution. Fonts: open licenses only (OFL or similar), file in the repo.
5. No real unit patches, flags, faces of real people, or vendor logos.
6. One style across every screen. A screen left in the old style is a bug.
7. The ART rows do not block the M2R gate. If Nick's playtest produces fixes, those go first.

## How the style gets chosen (ART-01)
Codex builds three clearly different directions as the same screen: the day 11 morning (map with Spoke B down, Capt Ruiz's lead item, top strip). Each is a real Godot render at 1280x800, not a mock in another tool, with a one-paragraph note: the idea, palette, font, how people and sites are drawn, what it costs to carry across every screen. Fable posts the three images to Nick in chat. Nick picks one or mixes. Nothing else in ART starts before that pick.

## Nick, 9 Nov 2026 evening
He saw the three ART-01 directions. Keep all three and let the player choose (ART-07), but it is "not a hot requirement": a much larger visual overhaul is intended later, so nothing in ART should be gold-plated. Fixing how the game plays (M2R-36) comes before more art.

## Suno (music and sound), Nick, 10 Nov 2026
Nick has connected Suno to both agents and leaves it to us whether the game gets music; it also makes sound effects and loops. His limit: at most 100 generation credits per run; ask him before spending more. Downloading a file costs a separate download credit and needs his say-so (FOR-NICK). Every Suno file committed gets a PROVENANCE row: generated, Suno, the model, the prompt, the clip id. Instrumental only, no vocals, no artist or song names in prompts. Nothing a real unit or nation would recognize as theirs.
