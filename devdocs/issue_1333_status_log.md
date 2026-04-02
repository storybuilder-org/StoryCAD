# Issue 1333: Usage Statistics — Status Log

## 2026-04-02

### Session Summary
- Created branch `issue-1333-usage-statistics` off `dev`
- Researched OpenTelemetry as suggested by Jake (Rarisma) in issue comment
- Produced research document: `devdocs/issue_1333_opentelemetry_research.md`

### Key Decisions

**OpenTelemetry: Not recommended.** Core mismatch — OTel produces aggregated metrics and distributed trace spans; StoryCAD needs per-session, per-outline rows in MySQL. No MySQL exporter exists, and standing up an OTel Collector + trace backend is unjustified for ~2,000 users. Custom telemetry using existing `IMySqlIo`/stored procedure pattern is the right approach.

**Integer user_id vs GUID: No privacy difference.** Both are pseudonymous — as long as the `users` table maps either to an email, the user is identifiable. Consent gating is the real privacy mechanism, not identifier format.

**Unlinkable usage_id for true anonymity (if desired later):**
- App generates a random GUID locally on opt-in, stored only in local preferences
- All usage data sent to MySQL uses this `usage_id` — not the `user_id` from the `users` table
- No foreign key, no lookup table connecting the two in the database
- Database cannot join usage data back to user identity, even for admins
- Tradeoff: loses ability to correlate usage with user demographics, delete per-user usage data (GDPR), or debug individual user issues
- Still supports aggregate analytics (feature popularity, genre distribution, session duration patterns)

### Artifacts
- `devdocs/issue_1333_opentelemetry_research.md` — Full research document (committed f885b1c7)

### Next Steps
- Design phase planning for issue 1333 (schema, consent model, implementation approach)
