# Local Test Database

A Dockerized MySQL 8.0 with the current StoryCAD schema (including the usage
statistics tables from PR #1380) and three fake seed users. Point StoryCAD at
it to exercise backend code without hitting production ScaleGrid.

## Prerequisites

Install Docker Desktop (Windows, macOS) or Docker Engine (Linux) from
https://www.docker.com/products/docker-desktop/, then check:

```bash
docker --version
docker compose version
```

Launch Docker Desktop and wait for the whale icon in the tray/menu bar to
stop animating — usually 30-60 seconds. Then:

```bash
docker info
```

should print server info without errors. If you skip this and try to start
the container, you get:

```
unable to get image '...': failed to connect to the docker API at
npipe:////./pipe/dockerDesktopLinuxEngine
```

Start Docker Desktop and try again.

## Start the database

From this folder:

```bash
docker compose up -d
```

On first run, Compose builds from the `Dockerfile`, boots MySQL 8.0, and runs
the init scripts out of `/docker-entrypoint-initdb.d`:

- `STORYBUILDER_CURRENT_SCHEMA.sql` — tables, stored procedures, purge event
- `seed_test_data.sql` — Alice, Bob, Carol with preferences

Those scripts only run against an empty volume, so after the first boot the
data sticks. See **Reset** below when you need to reload the schema.

## Verify

```bash
docker ps                                              # container running
docker compose logs mysql | grep "ready for connections"
docker compose exec mysql mysql -ustbtest -p123 StoryBuilder -e "SHOW TABLES;"
```

`mysql` in those last two commands is the service name from
`docker-compose.yml`, not a container name. Compose maps it to the real
container (`testdb-mysql-1` by default) so you never have to look it up.

Expected tables: `users`, `preferences`, `versions`, `sessions`,
`outline_sessions`, `outline_metadata`, `feature_usage`, `schema_version`.

## Point StoryCAD at it

At startup, StoryCAD checks the `STORYCAD_TEST_CONNECTION` environment
variable. If set, it skips Doppler and uses the value directly. If unset, it
takes the normal production path.

Connection string:

```
server=127.0.0.1;port=3306;database=StoryBuilder;uid=stbtest;pwd=123;
```

### Windows (PowerShell, persistent for your user)

```powershell
[Environment]::SetEnvironmentVariable(
    "STORYCAD_TEST_CONNECTION",
    "server=127.0.0.1;port=3306;database=StoryBuilder;uid=stbtest;pwd=123;",
    "User")
```

Restart Visual Studio or Rider so it inherits the new variable.

### macOS / Linux

```bash
export STORYCAD_TEST_CONNECTION="server=127.0.0.1;port=3306;database=StoryBuilder;uid=stbtest;pwd=123;"
```

Drop that line in `~/.zshrc` or `~/.bashrc` to make it stick.

### Check it worked

Launch StoryCAD. The log should contain:

```
Using local test database connection
```

If it doesn't, the process didn't see the variable — usually because the IDE
was already running when you set it. Restart the IDE.

## Reset

The init scripts only fire on an empty volume. To reload the schema or start
clean:

```bash
docker compose down -v   # wipes the named volume storybuilder-data
docker compose up -d
```

Plain `down` (no `-v`) keeps the volume, so the next `up` reuses whatever
schema is already on disk.

## Stop

```bash
docker compose down      # keeps data
docker compose down -v   # deletes data
```

## Credentials

Local dev only. Don't reuse them anywhere.

| Field            | Value          |
|------------------|----------------|
| Root password    | `root`         |
| Application user | `stbtest`      |
| Application pass | `123`          |
| Database         | `StoryBuilder` |
| Host             | `localhost`    |
| Port             | `3306`         |

## Gotchas

- **Port 3306 in use.** If you already run MySQL locally, either stop it or
  remap the host side in `docker-compose.yml` (e.g. `"3307:3306"`) and match
  the port in the connection string.
- **Use `127.0.0.1`, not `localhost`.** With `Server=localhost`, the MySql.Data
  client has been observed to fail auth against this container on Windows
  (error: `Access denied for user 'stbtest'@'localhost'`). `127.0.0.1` works.
- **`STORYCAD_TEST_CONNECTION` is read once at process start.** Change it,
  restart the IDE and any running StoryCAD instance.
- **`docker compose down` keeps the volume.** `storybuilder-data` survives
  until you pass `-v`.
