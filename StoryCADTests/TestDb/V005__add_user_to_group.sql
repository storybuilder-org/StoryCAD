-- Issue #1628 — Collaborator beta testers in the collab_beta user group
-- Adds: spAddUserToGroup. The app calls it after Join and at startup for beta testers.
-- Apply order: after V004 (user_groups, #1396).

USE StoryBuilder;

DROP PROCEDURE IF EXISTS spAddUserToGroup;
DELIMITER $$

-- INSERT IGNORE: the app calls this at each startup, so a second call for the same
-- user and group must do nothing.
CREATE PROCEDURE spAddUserToGroup(
    IN in_user_id INT,
    IN in_group_name VARCHAR(50)
)
BEGIN
    INSERT IGNORE INTO user_groups (group_name, user_id)
    VALUES (in_group_name, in_user_id);
END$$

DELIMITER ;
