-- Issue #1396 — Admin message sender: user groups and send records
-- Adds: user_groups, message_sends tables;
--       rebuilds spDeleteUser to include the user_groups cascade.
-- Apply order: after V003 (messaging, #1377).

USE StoryBuilder;

-- ---------------------------------------------------------------------------
-- Tables
-- ---------------------------------------------------------------------------

-- Named sets of users for audience filters, e.g. send_checkers, board.
CREATE TABLE IF NOT EXISTS user_groups
(
    group_name  VARCHAR(50) NOT NULL,
    user_id     INT         NOT NULL,
    added_at    DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (group_name, user_id),
    KEY ix_user (user_id),

    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- One row per test send, real send, or top-up. filters holds the audience as JSON,
-- so a top-up can run the same filters again.
CREATE TABLE IF NOT EXISTS message_sends
(
    id             INT AUTO_INCREMENT PRIMARY KEY,
    message_id     INT          NOT NULL,
    send_type      VARCHAR(10)  NOT NULL,
    filters        TEXT         NOT NULL,
    dry_run_count  INT          NOT NULL,
    sent_count     INT          NOT NULL,
    approved_by    VARCHAR(100) NOT NULL,
    sent_at        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    KEY ix_message (message_id),

    FOREIGN KEY (message_id) REFERENCES messages(message_id)
);

-- ---------------------------------------------------------------------------
-- Rebuild spDeleteUser to add the user_groups cascade.
-- Same body as V003, with the user_groups DELETE first.
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

        DELETE FROM user_groups        WHERE user_groups.user_id        = user_id;
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
VALUES ('V004', 'Admin message sender: user_groups, message_sends, spDeleteUser cascade')
ON DUPLICATE KEY UPDATE applied_on = CURRENT_TIMESTAMP;
