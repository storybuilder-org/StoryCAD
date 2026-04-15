# Issue 1377: Admin-to-User Messaging — Status Log

## 2026-04-15

### Session Summary
- Created branch `issue-1377-admin-messaging` from `issue-1333-usage-statistics`
- Created initial design document: `devdocs/issue_1377_design.md`
- Schema sketched: `messages` table (content, priority, scheduling, expiry) and `message_recipients` junction table (user_id FK, read/dismissed state)
- Group targeting resolved at send time via queries, not stored group membership
- DAL read capability: first feature requiring SELECT from backend — new `GetUnreadMessages` and `MarkMessageRead` methods on `IMySqlIo`
- `spDeleteUser` cascade: `message_recipients` must be included
- Identified relationship to ScaleGrid migration: all new tables (#1333 + #1377) plus users.id INT→GUID should go in together

### Prerequisites (from #1333 shared infrastructure)
- ScaleGrid MySQL 5.7 → 8.0 upgrade
- users.id INT → GUID migration
- Local test database (Docker-based MySQL 8.0)
- Environment switching (production vs test)

### Design Discussion (later 2026-04-15)
- Messages are a queue: unread messages persist across sessions until user explicitly dismisses each one
- Existing new-user popup and donation reminders are special cases of messages — the messaging system generalizes them
- Two message sources: admin-composed (via admin tool) and system-generated (triggered by conditions)
- Recipient generation is the core admin-side problem — each use case is a different query (all users, by version, by date, new users, periodic reminders)
- One-shot messages (admin sends now) vs recurring/automated (triggers on schedule or condition) — MVP is one-shot, automation layered later
- Priority display treatment is an open design question (order only, or also UX style?)
- Use case inventory needed before finalizing recipient generation design

### Decisions
- Admin tool: Uno Platform desktop app (same stack as StoryCAD, reuses StoryCADLib DAL)

### Next Steps
- Design review with Terry and Jake
- Resolve open questions (UX, admin tool platform, rich content format, priority treatment)
- Enumerate message use cases to drive recipient generation design
- Coordinate schema with #1333 for unified ScaleGrid migration
