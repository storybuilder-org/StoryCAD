-- StoryBuilder complete database schema
-- Includes: baseline (production 2026-04-14) + V001 + V002 (usage statistics)
-- Target: MySQL 8.0+

CREATE DATABASE IF NOT EXISTS StoryBuilder;
USE StoryBuilder;

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

-- --------------------------------------------------------
-- Core tables (from production baseline)
-- --------------------------------------------------------

DROP TABLE IF EXISTS `users`;
CREATE TABLE `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user_name` varchar(128) DEFAULT NULL,
  `email` varchar(128) NOT NULL,
  `date_added` datetime DEFAULT CURRENT_TIMESTAMP,
  `mailchimp_added` tinyint(1) DEFAULT '0',
  `first_name` varchar(64) DEFAULT NULL,
  `last_name` varchar(64) DEFAULT NULL,
  `email_verified` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`id`),
  UNIQUE KEY `id` (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

DROP TABLE IF EXISTS `preferences`;
CREATE TABLE `preferences` (
  `user_id` int(11) NOT NULL,
  `elmah_consent` tinyint(1) DEFAULT NULL,
  `newsletter_consent` tinyint(1) DEFAULT NULL,
  `version` varchar(64) DEFAULT NULL,
  `last_update` datetime DEFAULT CURRENT_TIMESTAMP,
  `usage_consent` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`user_id`),
  CONSTRAINT `preferences_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

DROP TABLE IF EXISTS `versions`;
CREATE TABLE `versions` (
  `user_id` int(11) NOT NULL,
  `last_update` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `current_version` varchar(64) DEFAULT NULL,
  `previous_version` varchar(64) DEFAULT NULL,
  PRIMARY KEY (`user_id`,`last_update`),
  CONSTRAINT `versions_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------
-- Usage statistics tables (V002)
-- --------------------------------------------------------

DROP TABLE IF EXISTS `sessions`;
CREATE TABLE `sessions` (
  `session_id` INT NOT NULL AUTO_INCREMENT,
  `usage_id` CHAR(36) NOT NULL,
  `session_start` DATETIME NOT NULL,
  `session_end` DATETIME NOT NULL,
  `clock_time_seconds` INT NOT NULL,
  PRIMARY KEY (`session_id`),
  INDEX `idx_sessions_start` (`session_start`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DROP TABLE IF EXISTS `outline_sessions`;
CREATE TABLE `outline_sessions` (
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

DROP TABLE IF EXISTS `outline_metadata`;
CREATE TABLE `outline_metadata` (
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

DROP TABLE IF EXISTS `feature_usage`;
CREATE TABLE `feature_usage` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `session_id` INT NOT NULL,
  `feature_name` VARCHAR(50) NOT NULL,
  `use_count` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `idx_feature_usage_session` (`session_id`),
  CONSTRAINT `fk_feature_usage_session` FOREIGN KEY (`session_id`) REFERENCES `sessions` (`session_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------
-- Schema version tracking (V001)
-- --------------------------------------------------------

DROP TABLE IF EXISTS `schema_version`;
CREATE TABLE `schema_version` (
  `version` VARCHAR(10) NOT NULL,
  `description` VARCHAR(200) NOT NULL,
  `applied_on` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`version`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

INSERT INTO schema_version (version, description) VALUES
    ('V000', 'Baseline schema: users, preferences, versions tables + stored procedures'),
    ('V001', 'Schema version tracking table'),
    ('V002', 'Usage statistics tables, spRecordSessionData, usage_consent in preferences');

-- --------------------------------------------------------
-- Stored Procedures
-- --------------------------------------------------------

--
-- AddUser (legacy — no upsert)
--

DROP PROCEDURE IF EXISTS `AddUser`;
DELIMITER ;;
CREATE PROCEDURE `AddUser`(
    IN name VARCHAR(255),
    IN email VARCHAR(255),
    OUT user_id INT
)
BEGIN
    START TRANSACTION;

    INSERT INTO users(user_name, email)
    VALUES(name, email);

    SET user_id = LAST_INSERT_ID();

    IF user_id > 0 THEN
      COMMIT;
    ELSE
      ROLLBACK;
    END IF;
END ;;
DELIMITER ;

--
-- spAddUser (current — with upsert on duplicate email)
--

DROP PROCEDURE IF EXISTS `spAddUser`;
DELIMITER ;;
CREATE PROCEDURE `spAddUser`(
    IN name VARCHAR(255),
    IN email VARCHAR(255),
    OUT user_id INT
)
BEGIN
    START TRANSACTION;

    INSERT INTO users(user_name, email)
        VALUES(name, email)
    ON DUPLICATE KEY UPDATE
        user_name = name,
        email = email,
        id = LAST_INSERT_ID(id);

    SET user_id = LAST_INSERT_ID();

    IF user_id > 0 THEN
      COMMIT;
    ELSE
      ROLLBACK;
    END IF;
END ;;
DELIMITER ;

--
-- spAddOrUpdatePreferences (includes usage_consent from V002)
--

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

--
-- spAddOrUpdateVersion
--

DROP PROCEDURE IF EXISTS `spAddOrUpdateVersion`;
DELIMITER ;;
CREATE PROCEDURE `spAddOrUpdateVersion`(
    IN user_id INT,
    IN current_ver VARCHAR(64),
    IN previous_ver VARCHAR(64)
)
BEGIN
    INSERT INTO versions
        (user_id, current_version, previous_version)
    VALUES (user_id, current_ver, previous_ver)
    ON DUPLICATE KEY UPDATE
        current_version = current_ver,
        previous_version = previous_ver;
END ;;
DELIMITER ;

--
-- spDeleteUser (cascade delete for Apple 5.1.1(v) compliance)
--

DROP PROCEDURE IF EXISTS `spDeleteUser`;
DELIMITER ;;
CREATE PROCEDURE `spDeleteUser`(
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

        DELETE FROM versions WHERE versions.user_id = user_id;
        DELETE FROM preferences WHERE preferences.user_id = user_id;
        DELETE FROM users WHERE id = user_id;

        COMMIT;
        SET deleted = TRUE;
    END IF;
END ;;
DELIMITER ;

--
-- spRecordSessionData (single round-trip usage data flush, V002)
-- Requires MySQL 8.0+ for JSON_TABLE
--

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

    -- 1. Insert session row
    INSERT INTO sessions (usage_id, session_start, session_end, clock_time_seconds)
    VALUES (p_usage_id, p_session_start, p_session_end, p_clock_time_seconds);

    SET v_session_id = LAST_INSERT_ID();

    -- 2. Outline sessions
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

    -- 3. Outline metadata (upsert)
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

    -- 4. Feature usage
    INSERT INTO feature_usage (session_id, feature_name, use_count)
    SELECT v_session_id, jt.feature_name, jt.use_count
    FROM JSON_TABLE(p_features, '$[*]' COLUMNS (
        feature_name VARCHAR(50) PATH '$.feature_name',
        use_count    INT         PATH '$.use_count'
    )) AS jt;

    COMMIT;
END ;;
DELIMITER ;

-- --------------------------------------------------------
-- Scheduled Events (data retention)
-- --------------------------------------------------------

-- Purge usage data older than 90 days (nightly at 02:00 UTC)
-- Requires EVENT_SCHEDULER=ON on the MySQL server
-- Batched DELETEs (LIMIT 5000) to avoid long lock waits

DROP EVENT IF EXISTS `ev_purge_usage_data`;
DELIMITER ;;
CREATE EVENT `ev_purge_usage_data`
  ON SCHEDULE EVERY 1 DAY
  STARTS '2026-01-01 02:00:00'
  DO
  BEGIN
    -- Purge outline_sessions and feature_usage first, then their parent sessions.
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

    -- Purge outline_metadata separately on created_at
    DELETE FROM outline_metadata
    WHERE created_at < NOW() - INTERVAL 90 DAY
    LIMIT 5000;
  END ;;
DELIMITER ;

-- --------------------------------------------------------
-- Restore settings
-- --------------------------------------------------------

/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;
/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;