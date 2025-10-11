# kame

## 📌 Prerequisites
Make sure you have these tools installed on your machine:

- [Node.js](https://nodejs.org) (LTS version recommended)
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com)

Verify versions:

```bash
dotnet --version
node --version
npm --version
git --version
```

> Example output:
> ```
> git version 2.25.1  
> 8.0.414           # .NET SDK  
> v20.19.5          # Node.js  
> 10.8.2            # npm
> ```

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

### 4️⃣ Build & run

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
