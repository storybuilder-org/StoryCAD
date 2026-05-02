-- V002: Usage statistics tables, spRecordSessionData, usage_consent in preferences
-- Applied to: StoryBuilder database (production + local test)
-- Prerequisite: MySQL 8.0.19+ (JSON_TABLE, row alias ON DUPLICATE KEY UPDATE)

-- 1. Add usage_consent column to preferences (idempotent via IF NOT EXISTS check)
SET @col_exists = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'preferences'
      AND COLUMN_NAME = 'usage_consent'
);
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE preferences ADD COLUMN usage_consent TINYINT(1) DEFAULT 0',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Usage statistics tables

CREATE TABLE IF NOT EXISTS `sessions` (
  `session_id` INT NOT NULL AUTO_INCREMENT,
  `usage_id` CHAR(36) NOT NULL,
  `session_start` DATETIME NOT NULL,
  `session_end` DATETIME NOT NULL,
  `clock_time_seconds` INT NOT NULL,
  PRIMARY KEY (`session_id`),
  INDEX `idx_sessions_start` (`session_start`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `outline_sessions` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `session_id` INT NOT NULL,
  `outline_guid` CHAR(36) NOT NULL,
  `open_time` DATETIME NOT NULL,
  `close_time` DATETIME NULL,
  `elements_added` INT NOT NULL DEFAULT 0,
  `elements_deleted` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `idx_outline_sessions_session` (`session_id`),
  CONSTRAINT `fk_outline_sessions_session` FOREIGN KEY (`session_id`) REFERENCES `sessions` (`session_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `outline_metadata` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `usage_id` CHAR(36) NOT NULL,
  `outline_guid` CHAR(36) NOT NULL,
  `genre` VARCHAR(50) NULL,
  `story_form` VARCHAR(50) NULL,
  `element_count` INT NOT NULL DEFAULT 0,
  `last_updated` DATETIME NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_outline_metadata` (`usage_id`, `outline_guid`),
  INDEX `idx_outline_metadata_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `feature_usage` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `session_id` INT NOT NULL,
  `feature_name` VARCHAR(50) NOT NULL,
  `use_count` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `idx_feature_usage_session` (`session_id`),
  CONSTRAINT `fk_feature_usage_session` FOREIGN KEY (`session_id`) REFERENCES `sessions` (`session_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Update spAddOrUpdatePreferences to include usage_consent

DROP PROCEDURE IF EXISTS `spAddOrUpdatePreferences`;
DELIMITER ;;
CREATE PROCEDURE `spAddOrUpdatePreferences`(
    IN user_id INT,
    IN elmah BOOL,
    IN newsletter BOOL,
    IN ver VARCHAR(64),
    IN usage_stats BOOL
)
BEGIN
    INSERT INTO preferences
        (user_id, elmah_consent, newsletter_consent, version, usage_consent)
    VALUES (user_id, elmah, newsletter, ver, usage_stats)
    ON DUPLICATE KEY UPDATE
        elmah_consent = elmah,
        newsletter_consent = newsletter,
        version = ver,
        usage_consent = usage_stats;
END ;;
DELIMITER ;

-- 4. spRecordSessionData (single round-trip usage data flush)

DROP PROCEDURE IF EXISTS `spRecordSessionData`;
DELIMITER ;;
CREATE PROCEDURE `spRecordSessionData`(
    IN p_usage_id CHAR(36),
    IN p_session_start DATETIME,
    IN p_session_end DATETIME,
    IN p_clock_time_seconds INT,
    IN p_outlines JSON,
    IN p_features JSON
)
BEGIN
    DECLARE v_session_id INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    INSERT INTO sessions (usage_id, session_start, session_end, clock_time_seconds)
    VALUES (p_usage_id, p_session_start, p_session_end, p_clock_time_seconds);

    SET v_session_id = LAST_INSERT_ID();

    INSERT INTO outline_sessions (session_id, outline_guid, open_time, close_time,
                                   elements_added, elements_deleted)
    SELECT v_session_id, jt.outline_guid, jt.open_time, jt.close_time,
           jt.elements_added, jt.elements_deleted
    FROM JSON_TABLE(p_outlines, '$[*]' COLUMNS (
        outline_guid     CHAR(36)  PATH '$.outline_guid',
        open_time        DATETIME  PATH '$.open_time',
        close_time       DATETIME  PATH '$.close_time',
        elements_added   INT       PATH '$.elements_added',
        elements_deleted INT       PATH '$.elements_deleted'
    )) AS jt;

    INSERT INTO outline_metadata (usage_id, outline_guid, genre, story_form,
                                   element_count, last_updated)
    SELECT p_usage_id, jt.outline_guid, jt.genre, jt.story_form,
           jt.element_count, jt.last_updated
    FROM JSON_TABLE(p_outlines, '$[*]' COLUMNS (
        outline_guid  CHAR(36)    PATH '$.outline_guid',
        genre         VARCHAR(50) PATH '$.genre',
        story_form    VARCHAR(50) PATH '$.story_form',
        element_count INT         PATH '$.element_count',
        last_updated  DATETIME    PATH '$.last_updated'
    )) AS jt
    ON DUPLICATE KEY UPDATE
        genre         = VALUES(genre),
        story_form    = VALUES(story_form),
        element_count = VALUES(element_count),
        last_updated  = VALUES(last_updated);

    INSERT INTO feature_usage (session_id, feature_name, use_count)
    SELECT v_session_id, jt.feature_name, jt.use_count
    FROM JSON_TABLE(p_features, '$[*]' COLUMNS (
        feature_name VARCHAR(50) PATH '$.feature_name',
        use_count    INT         PATH '$.use_count'
    )) AS jt;

    COMMIT;
END ;;
DELIMITER ;

-- 5. Purge event for 90-day data retention

DROP EVENT IF EXISTS `ev_purge_usage_data`;
DELIMITER ;;
CREATE EVENT `ev_purge_usage_data`
  ON SCHEDULE EVERY 1 DAY
  STARTS '2026-01-01 02:00:00'
  DO
  BEGIN
    -- MySQL disallows LIMIT inside an IN-subquery, so the SELECT is wrapped
    -- in a derived table.
    DELETE FROM outline_sessions
    WHERE session_id IN (
        SELECT session_id FROM (
            SELECT session_id FROM sessions
            WHERE session_start < NOW() - INTERVAL 90 DAY
            LIMIT 5000
        ) AS s
    );

    DELETE FROM feature_usage
    WHERE session_id IN (
        SELECT session_id FROM (
            SELECT session_id FROM sessions
            WHERE session_start < NOW() - INTERVAL 90 DAY
            LIMIT 5000
        ) AS s
    );

    DELETE FROM sessions
    WHERE session_start < NOW() - INTERVAL 90 DAY
    LIMIT 5000;

    DELETE FROM outline_metadata
    WHERE created_at < NOW() - INTERVAL 90 DAY
    LIMIT 5000;
  END ;;
DELIMITER ;

-- 6. Record migration

INSERT IGNORE INTO schema_version (version, description) VALUES
    ('V002', 'Usage statistics tables, spRecordSessionData, usage_consent in preferences');
