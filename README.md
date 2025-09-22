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
Use one of these prefixes:

- `feature/…`
- `bugfix/…`
- `hotfix/…`
- `test/…`

> More info: [Branch naming gist](https://gist.github.com/Zekfad/f51cb06ac76e2457f11c80ed705c95a3)

---

## ✍️ Commit Naming Convention
Start commits with one of:

- `build` / `ci` / `chore` / `docs`  
- `feat` / `fix` / `perf` / `refactor`  
- `revert` / `style` / `test`

> More info: [Conventional commits gist](https://gist.github.com/qoomon/5dfcdf8eec66a051ecd85625518cfd13)
