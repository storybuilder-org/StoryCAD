FROM mysql:8.0

# Flattened schema file: tables, procedures, and migrations through V002
COPY STORYBUILDER_CURRENT_SCHEMA.sql /docker-entrypoint-initdb.d/01_schema.sql
COPY seed_test_data.sql /docker-entrypoint-initdb.d/02_seed.sql
# Messaging tables (#1377) are not in the flattened schema file.
COPY V003__messaging_tables.sql /docker-entrypoint-initdb.d/03_messaging.sql
