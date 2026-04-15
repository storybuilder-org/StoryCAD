# Issue 1377: Admin-to-User Messaging — Design Document

**Issue**: https://github.com/storybuilder-org/StoryCAD/issues/1377
**Branch**: `issue-1377-admin-messaging`
**Based on**: `issue-1333-usage-statistics` (shared database infrastructure)
**Last Updated**: 2026-04-15

---

## 1. Overview

Build a notification/messaging system allowing StoryCAD administrators to send messages to users within the app. Messages can target a single user or a group of users. Users see messages on launch or via a notification indicator.

This issue depends on shared infrastructure from #1333:
- Local test database (Docker-based MySQL 8.0)
- Environment switching (production vs test)
- Schema migration strategy (numbered SQL scripts)
- ScaleGrid MySQL 8.0 upgrade

---

## 2. Relationship to ScaleGrid Migration

The ScaleGrid migration to MySQL 8.0 should include all schema changes at once:

| Source | Tables |
|--------|--------|
| Existing (migrated) | `users` (INT→GUID PK), `preferences`, `versions` |
| #1333 (usage statistics) | `sessions`, `outline_sessions`, `outline_metadata`, `feature_usage` |
| #1377 (messaging) | `messages`, `message_recipients` |
| Infrastructure | `schema_version` |

The `users.id` change from INT AUTO_INCREMENT to GUID cascades to:
- `preferences.user_id`
- `versions.user_id`
- `message_recipients.user_id` (new, #1377)

Usage tables (#1333) are unaffected — they use `usage_id`, not `user_id`.

---

## 3. Proposed Schema

### `messages`

| Column | Type | Notes |
|--------|------|-------|
| message_id | INT AUTO_INCREMENT | PK |
| subject | VARCHAR(200) | Message title/subject line |
| body | TEXT | Message content — plain text or markdown TBD |
| priority | ENUM('info','important','urgent') | Display treatment in app |
| created_at | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP | When composed |
| scheduled_at | DATETIME NULL | NULL = send immediately; otherwise send after this time |
| expires_at | DATETIME NULL | NULL = no expiry; otherwise stop showing after this time |
| created_by | VARCHAR(100) | Admin identifier (name or email) |

### `message_recipients`

| Column | Type | Notes |
|--------|------|-------|
| id | INT AUTO_INCREMENT | PK |
| message_id | INT | FK → messages.message_id, INDEX |
| user_id | CHAR(36) | FK → users.id (GUID after migration), INDEX |
| read_at | DATETIME NULL | NULL = unread; timestamp when read/dismissed |

**UNIQUE constraint**: `(message_id, user_id)` — a user can only be a recipient of a given message once.

### Group targeting

Rather than a groups table, groups are resolved at send time:
- "All users" → INSERT into `message_recipients` for every user_id in `users`
- "By version" → JOIN on `preferences.version` to select matching user_ids
- "By platform" → TBD (may need a platform column in `preferences`)
- "By registration date" → filter on `users.date_added`

This keeps the schema simple. The admin tool runs the query and populates `message_recipients` at send time. No ongoing group membership to maintain.

---

## 4. DAL Read Capability

This is the first feature that requires `IMySqlIo` to **read** from the database. The app needs to fetch unread messages on launch.

### New methods on `IMySqlIo`

```csharp
/// Fetches unread messages for a user. Returns message content + metadata.
Task<List<UserMessage>> GetUnreadMessages(string userId);

/// Marks a message as read/dismissed for a user.
Task MarkMessageRead(string userId, int messageId);
```

### New stored procedures

**`spGetUnreadMessages`** — SELECT from `messages` JOIN `message_recipients` WHERE `user_id = ?` AND `read_at IS NULL` AND (`scheduled_at IS NULL OR scheduled_at <= NOW()`) AND (`expires_at IS NULL OR expires_at > NOW()`).

**`spMarkMessageRead`** — UPDATE `message_recipients` SET `read_at = NOW()` WHERE `user_id = ?` AND `message_id = ?`.

### `spDeleteUser` cascade

Add `DELETE FROM message_recipients WHERE user_id = ?` to the existing cascade in `spDeleteUser` (Apple Guideline 5.1.1(v) compliance).

---

## 5. App-Side Flow

1. On launch, after `BackendService.StartupRecording()`, call `GetUnreadMessages(userId)`
2. If messages exist, show notification indicator or display messages
3. User reads/dismisses a message → call `MarkMessageRead(userId, messageId)`
4. Messages with `expires_at` in the past are filtered out by the SP, not the app

---

## 6. Admin Tooling

### Options
- **Lightweight desktop app** (WPF/WinForms) — simplest for the small admin audience
- **CLI tool** — simpler to build, less convenient for group management
- **Web dashboard** — most flexible, most effort

### Minimum viable admin tool
- Compose a message (subject, body, priority)
- Select recipients: all users, by version, by date range, or individual user
- Send (populate `message_recipients`) or schedule (set `scheduled_at`)
- View message status (sent, read counts)

### Connection
- Connects directly to ScaleGrid MySQL (or local test DB)
- Uses same Doppler credentials mechanism or a separate admin config

---

## 7. Open Questions

1. **Rich content**: Plain text, markdown, or HTML in message body? Plain text is simplest; markdown adds formatting without security risk.
2. **UX**: Modal dialog on launch, notification panel, or toast? What if there are multiple unread messages?
3. **Templates**: Reusable templates for recurring messages (donation prompts, release announcements)? Deferred or MVP?
4. **Scheduling**: MVP or deferred? Schema supports it (`scheduled_at` column).
5. **Admin tool platform**: Desktop app, CLI, or web? Who uses it (Terry only, or Terry + Jake)?
6. **Platform column**: Does `preferences` need a platform field for group targeting by OS?
7. **Message retention**: Should old messages be purged, or kept indefinitely? They're small rows.

---

## 8. Related Documents

- `devdocs/issue_1333_design.md` — Shared database infrastructure, usage statistics design
- `devdocs/issue_1333_tdd_guide.md` — TDD guide (testing patterns reusable for #1377)
- `storybuilder-miscellaneous/mysql/STORYBUILDER_CURRENT_SCHEMA.sql` — Current production schema

## Related Issues

- #1333 — Usage statistics (shared infrastructure, prerequisite)
- #1297 — Subscription tiers (Collaborator, shares ScaleGrid backend)
- #1332 — Discord server reorganization (Pro tier)
