
---

## Key Concepts

### Features
- Organized by domain (e.g., `auth`, `dashboard`) to keep related components, services, and styles together.
- Each feature folder can contain React components, CSS, and service files.

### Services
- `services/api.js` contains generic API configuration (base URL, interceptors, etc.).
- Each feature can have its own service file (like `authService.js` or `dashboardService.js`) for specific API calls.

### Routes
- `routes/ProtectedRoute.jsx` ensures only authenticated users can access certain pages.

### Styling
- Global styles in `src/styles/index.css`.
- Feature-specific styles within each feature folder (like `Form.css` in `auth`).

### Vite Configuration
- `vite.config.js` sets up module aliases (`@` → `src`) and dev server proxy for API calls.

---

## Running the Project

1. Install dependencies:

```bash
npm install
```
