# kame

## 📌 Prerequisites
Make sure you have these tools installed on your machine:

- [Node.js](https://nodejs.org) (LTS version recommended)
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com)
- [PostgreSQL](https://www.postgresql.org/download/)

Verify versions:

```bash
dotnet --version
node --version
npm --version
git --version
sudo -u postgres psql -c 'SELECT version();' # (if running form UNIX systems)
```

> Example output:
> ```
> git version 2.25.1  
> 8.0.414           # .NET SDK  
> v20.19.5          # Node.js  
> 10.8.2            # npm   
> PostgreSQL 12.22 (Ubuntu 12.22-0ubuntu0.20.04.4) on x86_64-pc-linux-gnu, compiled by gcc (Ubuntu 9.4.0-1ubuntu1~20.04.2) 9.4.0, 64-bit
> ``` 
#### NOTE: make sure the `.dotnet/tools` path is added to your `$PATH` (for EF Core CLI tools):

```bash
export PATH="$PATH:$HOME/.dotnet/tools:$PATH"
```
---

## 🚀 Getting Started

### 1️⃣ Clone the repository
```bash
git clone https://github.com/SabaVan/kame.git
cd kame
```

### 2️⃣ Install frontend dependencies
```bash
cd frontend
npm ci   # installs exact versions from package-lock.json
cd ..
```

### 3️⃣ Install backend dependencies
```bash
dotnet restore ./backend
```

### 4️⃣ Set up PostgreSQL locally

1. Switch to the postgres superuser:

```bash
sudo -i -u postgres
```
2. Open the PostgreSQL shell:
```bash
psql
```

3. Create the database:
```psql
CREATE DATABASE "BarDb";
```

4. Create a PostgreSQL role (user) matching your UNIX username or a custom one:
```psql
CREATE USER your_user WITH PASSWORD 'your_password';
```

5. Grant all privileges on the database to your user:
```psql
GRANT ALL PRIVILEGES ON DATABASE "BarDb" TO your_user;
```

At this point, the database is created, and your user can manage it.

### 5️⃣ Host PostgreSQL locally

1. Start the PostgreSQL server so your backend can connect:
```bash
sudo service postgresql start
```

2. Export your environment variables needed for backend connection:
```bash
export DB_HOST=localhost
export DB_USER=tomas
export DB_PASSWORD=tomas
export DB_NAME=BarDb
```

3. Apply migrations (to make the database structure accordingly to out project)

```bash
cd backend
dotnet ef database update
cd ..
```


### Make sure that the setup went OK:
a. Login to the database using your created user:
```bash
psql -U your_user -d BarDb -h localhost -W
# Enter the password you set (your_password).
```

b. List all tables:
```psql
\dt
```
Example output:
```
BarDb=> \dt
               List of relations
 Schema |         Name          | Type  | Owner 
--------+-----------------------+-------+-------
 public | BarUserEntries        | table | tomas
 public | Bars                  | table | tomas
 public | Playlist              | table | tomas
 public | Users                 | table | tomas
 public | __EFMigrationsHistory | table | tomas
(5 rows)
```

Seeing these tables confirms the database is hosted and migrations have been applied.

See [more database isntructions ](#❓-database-instructions) for instructions.

### 6️⃣ Build & run

**Frontend:**
```bash
cd frontend
npm run build   # production bundle (vite build)
npm run dev     # local dev server
cd ..
```

**Backend:**
```bash
dotnet build ./backend
dotnet run --project ./backend   # runs the API locally
```

### ❓ Database instructions

#### 1. When you modify your models (e.g., `Bar`), create a new migration:

```bash
dotnet ef migrations add <MigrationName>
```

This will generate a migration file under the `Migrations/` folder.

#### 2. Updating the Database

Apply the migration to update the database schema:

```bash
dotnet ef database update
```

#### 3. Inspecting Tables

List all tables:

```sql
\dt
```

Check the structure of a table:

```sql
\d "Bars"
```

#### 4. Inserting Data

Example insert into the `Bars` table (PostgreSQL):

```sql
CREATE EXTENSION IF NOT EXISTS "pgcrypto";  -- for gen_random_uuid()
ALTER TABLE "Bars"
ALTER COLUMN "Id" SET DEFAULT gen_random_uuid();

INSERT INTO "Bars" ("Name", "State", "OpenAtUtc", "CloseAtUtc")
VALUES (
    'Kame Bar',
    1,
    '2025-10-17 17:00:00+00',  -- UTC date and time
    '2025-10-17 22:00:00+00'
);
```

If you want a fresh database:

```bash
dotnet ef database drop
dotnet ef database update
```
---

## 🌱 Branch Naming Convention
1. Use one of these prefixes:
- `feature` `bugfix` `hotfix` `test` `docs`
2. Add the ticket number after a hyphen:
- `feature-nr` `bugfix-nr` `hotfix-nr` `test-nr` `docs-nr`
3. Add the ticket name after a dash (if ticket name has whitespaces, make them hyphens):
- `feature-nr/ticket-name` `bugfix-nr/ticket-name` `hotfix-nr/ticket-name` `test-nr/ticket-name` `docs-nr/ticket-name`

  Example: If the ticket is [this](https://github.com/SabaVan/kame/issues/11), then the branch name will be:
`docs-11/create-UML-diagram-for-the-C#-side`
---

## ✍️ Commit Naming Convention
1. Start commits with one of:

- `build` / `ci` / `chore` / `docs`  
- `feat` / `fix` / `perf` / `refactor`  
- `revert` / `style` / `test`
2. Add the ticket number after a hyphen and add a colon:
- `fix-11:`
3. Add the appropriate commit description:
- `fix-11: change the branch and commit naming conventions`