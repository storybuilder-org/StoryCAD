-- Issue #1377 — Admin-to-user messaging
-- Adds: messages, message_recipients tables; spGetUnreadMessages, spMarkMessageRead procs;
--       rebuilds spDeleteUser to include message_recipients cascade.
-- Apply order: after V002 (usage statistics, #1333). Lands as part of #1388 cut-over to MySQL 8.4.4.

USE StoryBuilder;

-- ---------------------------------------------------------------------------
-- Tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS messages
(
    message_id    INT AUTO_INCREMENT PRIMARY KEY,
    subject       VARCHAR(200) NOT NULL,
    body          TEXT         NOT NULL,
    link_url      VARCHAR(2048) NULL,
    link_text     VARCHAR(100)  NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    scheduled_at  DATETIME     NULL,
    expires_at    DATETIME     NULL,
    created_by    VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS message_recipients
(
    id          INT AUTO_INCREMENT PRIMARY KEY,
    message_id  INT NOT NULL,
    user_id     INT NOT NULL,
    read_at     DATETIME NULL,

    UNIQUE KEY uq_message_user (message_id, user_id),
    KEY ix_user_unread (user_id, read_at),

    FOREIGN KEY (message_id) REFERENCES messages(message_id),
    FOREIGN KEY (user_id)    REFERENCES users(id)
);

-- ---------------------------------------------------------------------------
-- Stored procedures
-- ---------------------------------------------------------------------------

DROP PROCEDURE IF EXISTS spGetUnreadMessages;
DELIMITER $$

CREATE PROCEDURE spGetUnreadMessages(
    IN in_user_id INT
)
BEGIN
    SELECT m.message_id,
           m.subject,
           m.body,
           m.link_url,
           m.link_text,
           m.created_at
      FROM messages m
      JOIN message_recipients r ON r.message_id = m.message_id
     WHERE r.user_id  = in_user_id
       AND r.read_at IS NULL
       AND (m.scheduled_at IS NULL OR m.scheduled_at <= NOW())
       AND (m.expires_at   IS NULL OR m.expires_at   >  NOW())
     ORDER BY m.created_at;
END$$

DELIMITER ;

DROP PROCEDURE IF EXISTS spMarkMessageRead;
DELIMITER $$

CREATE PROCEDURE spMarkMessageRead(
    IN in_user_id    INT,
    IN in_message_id INT
)
BEGIN
    UPDATE message_recipients
       SET read_at = NOW()
     WHERE user_id    = in_user_id
       AND message_id = in_message_id
       AND read_at IS NULL;
END$$

DELIMITER ;

-- ---------------------------------------------------------------------------
-- Rebuild spDeleteUser to add the message_recipients cascade.
-- The pre-existing body (V000 baseline) cascades versions + preferences before
-- deleting the user row; we add the message_recipients DELETE in the same spot.
-- Apple Guideline 5.1.1(v) — user deletion must remove all user-linked rows.
-- ---------------------------------------------------------------------------

DROP PROCEDURE IF EXISTS spDeleteUser;
DELIMITER $$

CREATE PROCEDURE spDeleteUser(
    IN user_id INT,
    OUT deleted BOOL
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        SET deleted = FALSE;
    END;

    SET deleted = FALSE;

    IF user_id IS NOT NULL AND user_id > 0 THEN
        START TRANSACTION;

        DELETE FROM message_recipients WHERE message_recipients.user_id = user_id;
        DELETE FROM versions           WHERE versions.user_id           = user_id;
        DELETE FROM preferences        WHERE preferences.user_id        = user_id;
        DELETE FROM users              WHERE id                          = user_id;

        COMMIT;
        SET deleted = TRUE;
    END IF;
END$$

DELIMITER ;

-- ---------------------------------------------------------------------------
-- Schema version tracking
-- ---------------------------------------------------------------------------

INSERT INTO schema_version (version, description)
VALUES ('V003', 'Admin-to-user messaging: messages, message_recipients, related procs')
ON DUPLICATE KEY UPDATE applied_on = CURRENT_TIMESTAMP;
