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
2. If messages exist, display them (see UX open question below)
3. User reads/dismisses a message → call `MarkMessageRead(userId, messageId)`
4. Messages with `expires_at` in the past are filtered out by the SP, not the app

### Message Queue

Messages accumulate until the user explicitly dismisses each one. Unread messages persist across sessions — if a user ignores a message on Monday, it's still there on Tuesday. A message stays in the queue until `read_at` is set.

If multiple messages are pending, the app displays them in some order (priority, then chronological — TBD). The user works through them one at a time or sees a list.

### Generalizing Existing In-App Communication

The existing new-user popup (links to help resources) is a special case of a message. The messaging system could replace it — the "welcome" content becomes a system-generated message delivered to new users. Similarly, donation reminders (open issue) become recurring system messages rather than a separate feature.

This means the system supports two sources of messages:
- **Admin-composed**: written by a human, sent via the admin tool
- **System-generated**: triggered by conditions (new user, time since last donation prompt, version change, etc.)

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

### Recipient Generation

The core admin-side problem is: **how do we generate `message_recipients` rows for a particular message?** Each use case is a different query against `users`/`preferences`/`versions`:

| Use Case | Query Basis |
|----------|-------------|
| All users | All rows in `users` |
| Users on a specific version | JOIN `preferences` on version |
| Users registered before/after a date | Filter `users.date_added` |
| New users (first session) | Triggered on first launch, not a query |
| Donation reminder | Users not messaged with donation template in X days |
| Version-specific announcement | JOIN `preferences` on version |
| Individual user | Direct user_id |

Some are **one-shot** (admin composes, selects criteria, sends). Others are **recurring/automated** (donation reminder fires on a schedule, new-user welcome triggers on first launch). The admin tool should support one-shot at minimum; automation can be layered on later.

**This is an open design area.** The use cases above are illustrative, not exhaustive. Need to think through what messages we actually want to send before finalizing the recipient generation design.

### Connection
- Connects directly to ScaleGrid MySQL (or local test DB)
- Uses same Doppler credentials mechanism or a separate admin config

---

## 7. Open Questions

1. **Rich content**: Plain text, markdown, or HTML in message body? Plain text is simplest; markdown adds formatting without security risk.
2. **UX**: Modal dialog on launch, notification panel, or toast? What if there are multiple unread messages? Current new-user popup is a model to consider.
3. **Templates**: Reusable templates for recurring messages (donation prompts, release announcements)? Deferred or MVP?
4. **Scheduling**: MVP or deferred? Schema supports it (`scheduled_at` column).
5. **Admin tool platform**: Desktop app, CLI, or web? Who uses it (Terry only, or Terry + Jake)?
6. **Platform column**: Does `preferences` need a platform field for group targeting by OS?
7. **Message retention**: Should old messages be purged, or kept indefinitely? They're small rows.
8. **Priority display treatment**: Does priority affect display order only, or also display style (e.g., urgent = modal, info = badge)? Or both?
9. **System-generated messages**: How are automated messages (new-user welcome, donation reminders) triggered? In-app on launch conditions? Scheduled MySQL EVENT? Separate automation process?
10. **Use case inventory**: What messages do we actually want to send? Need to enumerate before finalizing recipient generation and automation design.

---

## 8. Related Documents

- `devdocs/issue_1333_design.md` — Shared database infrastructure, usage statistics design
- `devdocs/issue_1333_tdd_guide.md` — TDD guide (testing patterns reusable for #1377)
- `storybuilder-miscellaneous/mysql/STORYBUILDER_CURRENT_SCHEMA.sql` — Current production schema

## Related Issues

- #1333 — Usage statistics (shared infrastructure, prerequisite)
- #1297 — Subscription tiers (Collaborator, shares ScaleGrid backend)
- #1332 — Discord server reorganization (Pro tier)
