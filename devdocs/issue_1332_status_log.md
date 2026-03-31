# Issue #1332: Discord Server Reorganization — Status Log

## Issue
https://github.com/storybuilder-org/StoryCAD/issues/1332

## Current Status
**Phase:** Planning complete, ready for Phase 1 (channel reorganization) and Phase 2 (community events)
**Milestone:** Release 4.1
**Last Updated:** 2026-03-31

---

## Decisions Made

### Server Ownership
- **StoryBuilder Foundation** is the parent/owner of the Discord server

### Pro Tier
- **Price**: $2.99/mo (Discord minimum)
- **Platform**: Discord Server Subscriptions only — no app store integration, no store-to-Discord bridging
- **Positioning**: Community membership, not software feature gating
- **Pitch**: "Support the tool you use for less than a cup of coffee"
- **No grandfathering**: Existing Discord members start on same terms as everyone else; they are the target audience

### Pro Perks (6 categories)
1. **Influence**: Quarterly feature voting polls in Pro channel
2. **Critique**: Structured exchange with secure sharing (Google Drive or similar), Chez Rambo model
3. **Learning**: Workshops & office hours, session notes and recordings
4. **Recognition**: Pro badge/role (automatic from Discord subscription)
5. **Community**: Pro-only channels, beta access, priority support
6. **Resources**: Monthly curated packs (prompts, markets, templates)

### Events
- **Office hours**: 2 hrs/week Terry, 2 hrs/week Jake — public, announced via @everyone
- **Special events**: ~1/week (presentations, workshops, AMAs) — public, announced on Discord + website events page
- **Pro-only events**: Not at launch; add when demand warrants
- **Platform**: Discord voice/Stage; upgrade to Zoom/Meet later if needed

### Channel Structure
- 26 channels total: 17 public, 7 Pro-gated, 2 admin
- Organized around what writers do, not around the product
- Chez Rambo community model as reference
- Developer/writer distinction removed — server is for writers
- Full structure documented in issue #1332 body

### Launch Phasing
- **Before go-live (start now)**: Channel reorganization, office hours, special events
- **At go-live (4.1)**: Enable subscription, gate Pro channels, voting, beta access, resource drops
- **Post-launch**: Critique exchange (needs secure sharing model), workshops with recordings, Pro-only events

---

## Open Questions

1. **Critique exchange sharing model**: Google Drive? Per-rotation shared folder? Must prevent outline exposure beyond the critique group
2. **Event scheduling**: Days/times for office hours — time zone considerations
3. **mac-beta channel**: Remove (macOS launched) or keep for ongoing feedback?
4. **Tax implications**: Does Discord subscription income affect 501(c)(3) status? Needs tax advisor

---

## Progress Log

### 2026-03-31
- Reviewed all planning documents in `/mnt/c/temp/issue_1297_subscription_tiers/`
- Worked through all open questions with Terry:
  - Pro price confirmed at $2.99/mo (Discord minimum, verified via Discord docs)
  - No grandfathering — existing members are the audience, not exceptions
  - Events start public, Pro-only added as needed
  - Office hours: 2hr/wk Terry + 2hr/wk Jake
  - Special events: ~1/week, announced Discord + website
  - Launch phasing: start events now, paywall at 4.1
- Rewrote issue #1332 body with full scope: Pro definition, channel structure, events strategy, 5-phase implementation plan, open questions
- Replaced implementation plan with proper WBS (5 work packages, 35 tasks, owners, dependencies, dependency diagram)
- Split documents from #1297: created `issue_1332_discord_subscriptions.md` (setup reference) and this status log
- Trimmed #1297 status log and pricing model to remove #1332 scope
- Recorded StoryBuilder Foundation as Discord server owner
- Both Terry and Jake have admin/ownership responsibilities — no single point of failure for any task

### 2026-03-24 (from #1297 planning sessions)
- Proposed 24-channel structure (later refined to 26)
- Decided Pro billing is Discord Server Subscriptions only
- Decided to remove developer/writer channel distinction
- Pro positioned as membership model (influence, critique, workshops, recognition, resources)

### 2026-03-20 (from #1297 planning sessions)
- Evaluated full store-to-Discord linking pipeline — rejected as too complex
- **Key architectural decision**: Pro on Discord, Collaborator on app stores, fully independent systems
- Researched Discord Server Subscriptions: eligibility, fees, constraints
- Decided to start with Discord for events, upgrade platform later if needed

### 2026-03-19 (from #1297 planning sessions)
- Reviewed current Discord channel structure and Chez Rambo server as community model
- Researched event platforms: Discord (free), Zoom ($13-17/mo), Google Meet (free nonprofit, limited)

---

## Related Documents
- `devdocs/issue_1332_discord_subscriptions.md` — Discord subscription setup reference, pricing, eligibility, links
- `/mnt/c/temp/issue_1297_subscription_tiers/proposed_channel_structure.md` — Original channel proposal (now captured in issue body)
- `/mnt/c/temp/issue_1297_subscription_tiers/pricing_model_discord_integration.md` — Original combined pricing doc (split: Discord portions now in #1332 docs)

## Related Issues
- #1297 — Subscription tiers (Pro pricing, Collaborator tier)
- storybuilder-org/storybuilder-miscellaneous#40 — External Discord community involvement
