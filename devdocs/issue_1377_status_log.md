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

### Next Steps
- Design review with Terry and Jake
- Resolve open questions (UX, admin tool platform, rich content format)
- Coordinate schema with #1333 for unified ScaleGrid migration
