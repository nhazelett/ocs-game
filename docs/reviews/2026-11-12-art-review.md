# Art review, 12 Nov 2026 (Fable)

Nick, 12 Nov evening, before he plays the DESK gate again: the game needs to look more polished. Codex (ChatGPT) makes the images and models; Fable reviews them for quality and sends back anything that is not good enough. "Don't be a sycophant, if the quality isn't good, make it fix it." This starts the art work now rather than after the gate (ART-10 was waiting on it). The gate still decides the desk; nothing here changes what the Sim says.

Compared: the 8 Nov build before any art (2026-11-08-codex-m2r-13-day11.png), the three 9 Nov studies (2026-11-09-codex-art-01-1/2/3.png), the shipped desk today (2026-11-12-codex-desk-33-ready.png, desk-34-finished.png, desk-35-day4.png), the title, prep and debrief from the 10 Nov walk, the cast sheet (2026-11-09-codex-art-03-cast.png), and the kit diorama (assets/diorama/spoke-b.png).

## Verdict

Much better than 8 Nov, which was the engine's grey default with circles for islands. Not polished, and in places it has slipped behind its own 9 Nov study. The Tabletop study had a clear heading, a large "Morning of day 11", a lead card with a portrait and an accent bar, and three islands that filled the map. The shipped desk kept the palette and lost the hierarchy: small type everywhere, boxes that look like text fields, and a map that is one small island in a flat blue rectangle. The 3D map is still the study's placeholder geometry three days later.

## What is wrong, worst first

1. **The map is built from code primitives, and the real models are unused.** `game/board/TabletopMap3D.cs` draws every site as a Godot box with a prism roof, a cylinder or a prism. Codex built a 22-piece Blender kit on 8 Nov (tent, CHU, generator, fuel bladder, water buffalo, warehouse, forklift, trucks, guard tower, roads) and the diorama shows it reads well; none of it is on the map. Today a player cannot tell power from food from waste by looking: every one is a pale box.
2. **The sea and the light are flat.** The water is one unshaded teal plane with no shoreline, depth or movement; there is no sky or horizon; ambient light is a flat colour, so nothing sits on the ground. The island is a ten-sided cake with orange sides and a brown box for a dock. On the tutorial, two thirds of the map is empty teal.
3. **Overlays on the map are unstyled.** "Water 0% supplied / Food service 0% supplied" and "Cargo waiting" are plain white rectangles floating over the scene with no border, pointer or anchor; on day 4 the box sits on top of the trees. Lane labels ("Barge lane · closed") overlap the islands. The red X that marks a building down is a few pixels wide at Fit map.
4. **The desk lost the study's hierarchy.** Team names sit in bordered boxes that look like text inputs. The hours bars have no fill colour, so "0 of 8 h" and "8 of 8 h" read the same at a glance. "Operating area" is body text, not a heading. Menu and Developer tools overlap the top strip's border. Buttons mix left-aligned text (Open, Fit map, Arrange support) with centred text (People to reach) in the same pane.
5. **The title screen has no art.** A beige page, the name, a sentence and three buttons of equal weight. "Continue" when greyed looks like an empty text field, and Sound looks as important as New game. It is the first thing a player sees and the plainest screen in the game.
6. **Money reads oddly.** The body font's slashed zero makes "$15,000" look like "$15,ØØØ". Fine in an id, wrong in a price.
7. **Every face has its eyes closed.** The 17 portraits are consistent and clearly fictional, which is good, but all share one pose, one collar and closed eyes; side by side they look asleep.
8. **The debrief is a wall of text.** One long paragraph at body size, then boxes. It is the screen that teaches the most and has the least shape.

What works and should be kept: the palette (paper, moss, rust), state never by colour alone (the red ring and the words under the island), the portraits' flat style, the "Where things stand" rows with faces, the Night watch look's contrast.

## The bar Fable reviews against

Every ART PR is held to this. A PR that misses any point gets a comment naming it and is not merged; "better than before" is not enough.

1. **Before and after.** The same screen in the same state, before and after, plain PNG at 1280 by 800 from the renderer the Windows export uses, in docs/handoffs/images. No after without a before.
2. **The squint test.** At Fit map, with the labels cropped out, Fable can say what each building is (power, fuel, water, food, lodging, waste, MHE) and which site is in trouble. If Fable cannot, it fails.
3. **Nothing floats, nothing collides.** Every label and callout is anchored to what it describes and overlaps no island, building or other label in any screenshot.
4. **One style.** No engine-default control, no screen left in the old look, no element that looks like it came from a different game. Generated images match the palette and the flat Tabletop shapes.
5. **Not AI-looking.** No glossy airbrushed finish, melted detail, stray lettering, extra fingers, inconsistent light or fake text in a generated image. Each generated file has a PROVENANCE row with the tool and the prompt.
6. **Readable first.** Body text no smaller than today; state never by colour alone; the art never shows something the Sim did not say.
7. **Still runs.** The Windows build still starts and plays on ordinary laptop graphics; the PR says which renderer it uses and what it costs.

## Rows (Codex, game/ and assets/ only)

ART-12 to ART-17 in backlog/BACKLOG.md, in that order, for findings 1 to 7. Finding 8 (the debrief's layout) waits until after the gate, because its shape depends on what the debrief has to teach. ART-12 and ART-13 are the first steps of ART-10. Images may come from image generation, Blender, shaders or hand-written SVG; Codex picks the tool, the bar above decides.
