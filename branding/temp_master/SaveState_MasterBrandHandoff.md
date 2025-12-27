
# SaveState — Master Brand Handoff (v1)

## Purpose
This document is the single source of truth for the SaveState brand system.
All assets, motion, sound, and integrations must follow this guide.

---

## Phase 1 — Identity
- Logo system: primary, flat, wordmark, icon-only
- Dual-mode parity: dark + light
- Geometry: slightly rounded, pixel-hybrid DNA
- Color palette: Core Blue, Helix Cyan, Memory Violet, Deep Space Black, Grid Gray, Light Surface White
- Typography: role-based (Brand, UI, Display, Mono)

## Rules
- SVG is source of truth
- Glow only on primary logo
- No gradients outside brand assets
- Maintain contrast parity across modes

---

## Phase 2 — Distribution
- App icons: Windows, macOS, iOS, Android
- Favicons: 16–64 + SVG
- Social / OpenGraph cards
- Avatars

Directory reference:
SaveState_BrandKit_v2/phase_2_distribution/

---

## Phase 3 — Motion & Experience
### Motion Semantics
- DNA Drift: idle loop
- Save Pulse: one-shot feedback
- Save Complete: confirmation one-shot

### Render Passes
- MP4 reference renders
- Alpha WebM
- ProRes 4444
- PNG alpha sequences

### Engine Targets
- MonoGame: SpriteBatch + frame index timing
- Unity: Animator / Timeline
- Godot: AnimatedSprite2D

---

## Phase 3C — Sound & Haptics
- Save Pulse: short digital tick
- Save Complete: two-tone confirmation
- Haptics: soft tap patterns

---

## Versioning Rules
- Never rename files after lock
- Replace contents only
- Increment version folder when structure changes

---

## Do / Don’t
DO:
- Use provided assets
- Respect timing semantics
- Keep motion subtle

DON’T:
- Add glow to micro icons
- Stretch logos
- Introduce new colors without spec update

---

## Status
This handoff supersedes all prior notes.
Future updates must append, not overwrite.

