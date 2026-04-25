-- V001: Schema version tracking table
-- Applied to: StoryBuilder database (production + local test)
-- Idempotent: CREATE TABLE IF NOT EXISTS

CREATE TABLE IF NOT EXISTS `schema_version` (
  `version` VARCHAR(10) NOT NULL,
  `description` VARCHAR(200) NOT NULL,
  `applied_on` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`version`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

INSERT IGNORE INTO schema_version (version, description) VALUES
    ('V000', 'Baseline schema: users, preferences, versions tables + stored procedures'),
    ('V001', 'Schema version tracking table');
