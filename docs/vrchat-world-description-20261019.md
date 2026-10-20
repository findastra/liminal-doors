# Liminal Doors: the VRChat world description (2026-10-19)

Astra, 2026-10-19: "Make sure that the tracker link is in the world description on VR chat. So when people click it,
that they can also go to that website and track their own progress." The link goes to the **found floors** page, and
only there; the tools stay on The Threshold's pet program page ("they're two separate things").

- **Found floors page:** https://findastra.github.io/the-threshold/ (The Threshold's GitHub Pages,
  [the-threshold#3](https://github.com/findastra/the-threshold/pull/3)). Visitors tick each floor they've found; the
  ticks stay in their own browser.

## The description to paste at upload

In Unity: **VRChat SDK > Show Control Panel > Builder**, the world's **Description** field:

```
Doors that open onto quiet in-between places. Every door takes you somewhere random, and a small glass animal hides on every floor. Tick off the floors you've found: findastra.github.io/the-threshold
```

- The address is written without `https://` so it stays short. VRChat may not make it clickable inside the game, so
  people may have to type it.
- Keep the last sentence when the description changes for a new episode.

## Status

- **Not uploaded yet** (`handoff-claude-20261019-stops-02-03.md`, on `claude/fresh-start-20261018`), so there is no
  description on VRChat to change. The SDK asks for it at the first upload, and uploads need Astra's OK.
- After the upload, check it with VRChat's public world API: `https://api.vrchat.cloud/api/1/worlds/<wrld_…>`, field
  `description`. It reads without signing in.

Written by Claude Opus 5.5 (`claude-opus-5-5`), 2026-10-19.
