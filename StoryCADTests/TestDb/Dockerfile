FROM mysql:8.0

# Flattened schema file: tables, procedures, and migrations through V002
COPY STORYBUILDER_CURRENT_SCHEMA.sql /docker-entrypoint-initdb.d/01_schema.sql
COPY seed_test_data.sql /docker-entrypoint-initdb.d/02_seed.sql
# Migrations after V002 are not in the flattened schema file.
COPY V003__messaging_tables.sql /docker-entrypoint-initdb.d/03_messaging.sql
COPY V004__admin_sends.sql /docker-entrypoint-initdb.d/04_admin_sends.sql
COPY V005__add_user_to_group.sql /docker-entrypoint-initdb.d/05_add_user_to_group.sql
