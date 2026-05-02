-- Seed data for local test database
-- 3 fake test users with preferences and version records

USE StoryBuilder;

-- Test users
INSERT INTO users (id, user_name, email, first_name, last_name, mailchimp_added, email_verified)
VALUES
    (1, 'Alice Test', 'alice@test.local', 'Alice', 'Test', 0, 1),
    (2, 'Bob Test', 'bob@test.local', 'Bob', 'Test', 0, 1),
    (3, 'Carol Test', 'carol@test.local', 'Carol', 'Test', 0, 0);

-- Preferences
INSERT INTO preferences (user_id, elmah_consent, newsletter_consent, version)
VALUES
    (1, 1, 1, '4.0.2'),
    (2, 1, 0, '4.0.1'),
    (3, 0, 0, '3.9.0');

-- Version history
INSERT INTO versions (user_id, current_version, previous_version)
VALUES
    (1, '4.0.2', '4.0.1'),
    (2, '4.0.1', '4.0.0'),
    (3, '3.9.0', '3.8.0');
