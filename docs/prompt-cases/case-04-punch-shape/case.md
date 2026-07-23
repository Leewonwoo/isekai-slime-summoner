# Case 04: Bilateral Punch Shape Revision

- Date: 2026-07-14
- Tool: ChatGPT image generation / image-to-image editing
- Goal: Revise the Punch Slime attack sheet so the attack reads as two short side arms ending in blunt rounded fists, matching the supplied visual reference.

## v1 problem

- Prompt: `docs/prompts/slime-prompts.md` §4 attack sheet prompt, first version.
- Result: `v1.png`
- Problem: The attack looked like one long, one-sided protrusion. It did not match the desired silhouette of short bilateral arms with compact fists.

## v2 improvement

- Change: Used the existing attack sheet as the edit target and the supplied slime image as a supporting shape reference.
- Techniques: image-to-image editing, reference-image conditioning, invariant locking, negative prompt, explicit frame-by-frame motion specification.
- Invariants: strict 3×3 layout, exactly 9 cells, same featureless pale gray-blue body, flat `#00FF00` chroma background, no face or permanent limbs.
- Prompt: `docs/prompts/slime-prompts.md` §4 attack sheet prompt, bilateral-fist revision.
- Result: `v2.png`

## v3 storyboard-driven revision

- Change: Replaced the bilateral attack concept with the supplied 3×3 rough storyboard: one attached right-side punch, diagonal extension, clear peak, and return to rest.
- Techniques: sketch-to-image conditioning, reference-image conditioning, invariant locking, negative prompt, explicit frame timing.
- Invariants: strict 3×3 layout, exactly 9 cells, same featureless body, flat `#00FF00` chroma background, no face, no permanent limbs, no left-side appendage.
- Prompt: `docs/prompts/slime-prompts.md` §4 attack sheet prompt, storyboard-driven revision.
- Result: `v3.png`

## Verification

- v2: bilateral short arms and compact fists were confirmed before the storyboard revision.
- v3 frames 1–3: body-only preparation followed by a small right-side fist bud.
- v3 frames 4–5: one thick attached right arm extends diagonally upward-right, with frame 5 as the readable punch peak.
- v3 frames 6–9: the same punch retracts and the body returns to rest.
- Applied asset: `Assets/Art/Units/unit_punch_slime_attack_sheet.png`.
