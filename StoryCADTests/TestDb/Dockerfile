FROM mysql:8.0

# Single flattened schema file — all tables, procedures, and migrations
COPY STORYBUILDER_CURRENT_SCHEMA.sql /docker-entrypoint-initdb.d/01_schema.sql
COPY seed_test_data.sql /docker-entrypoint-initdb.d/02_seed.sql
