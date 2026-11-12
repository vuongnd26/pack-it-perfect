# Epic: Pack It Perfect

> **Source**: `design/game-brief.md` (Features list, Build order 1–9)
> **Engine**: Unity 6000.6.8f1 (C#, URP) · **Tier**: minimal

## Goal

Pack It Perfect is an ASMR packing puzzle. The player rotates and places items into a
suitcase, bag, bento box or car trunk until every cell is filled and the lid closes.
15 levels unlock new rules gradually: outlines, zones, pairs, heavy items, upright items.

## Scope

Brief Features 1–9. Deadline: code freeze and signed APK on 15/11 (see brief).
Out of scope: monetization, KPIs, reference-game positioning (blank in the GDD, see brief "Khoảng trống").

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | Core packing loop | Logic | Complete | N/A |
| 002 | 15-level data | Config/Data | Complete | N/A |
| 003 | Obstacle rules | Logic | Complete | N/A |
| 004 | Outline imprints | Visual/Feel | Complete | N/A |
| 005 | Hint booster and star rule | Logic | Complete | N/A |
| 006 | Timer mode and lose screen | UI | Ready | N/A |
| 007 | Progression and meta screens | UI | Ready | N/A |
| 008 | Tutorial hand and placement juice | Visual/Feel | Ready | N/A |
| 009 | Art and audio pass | Visual/Feel | Ready | N/A |

## Decisions (closed 2026-11-12)

All earlier open questions are answered in `design/game-brief.md` § "Quyết định đã chốt".
Level data: `Assets/Data/Levels/levels.json`, extracted from the playable prototype
(https://claude.ai/artifact/PskwEekJTZ85nAVkZywy4j) and checked against the brief's rule table.

- Timer is mandatory on levels 5, 10, 15. There is no relax/timer mode switch. (006)
- "Install to keep packing": GDD text, no link. (006)
- Heavy and zone rules are checked on drop. Pairs are checked when all items are placed, and touching means sharing an edge. (003)
- Hint picks the first unplaced piece in data order. Upright pieces are already upright. (005)
- Outlines only advise. (004)
- Levels unlock in order. (007)
