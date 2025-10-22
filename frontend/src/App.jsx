import { useState, useEffect } from 'react';
import { Routes, Route, Link, Navigate, useNavigate } from 'react-router-dom';
import Register from '@/features/auth/Register';
import Login from '@/features/auth/Login';
import Home from '@/features/dashboard/Home';
import Dashboard from '@/features/dashboard/Dashboard';
import BarSession from '@/routes/BarSession.jsx';
import Profile from '@/features/profile/Profile';
import { authService } from '@/features/auth/authService';

import '@/App.css';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(localStorage.getItem('loggedIn') === 'true');
  const navigate = useNavigate();

  useEffect(() => {
    document.title = 'Kame Bar';
  }, []);

  const handleLogout = () => {
    authService.logout();
    setIsLoggedIn(false);
    navigate('/home');
  };

  return (
    <div id="root">
      <header>
        <div className="logo-container">
          <img alt="kame" src="/kame.svg" className="logo" />
          <h1>
            <Link to="/home" className="logo-link">
              Kame Bar
            </Link>
          </h1>
        </div>

        <nav>
          {!isLoggedIn ? (
            <>
              <Link to="/login">Login</Link>
              <Link to="/register">Register</Link>
            </>
          ) : (
            <>
              <Link to="/dashboard">Dashboard</Link>
              <Link to="/profile">Profile</Link> {/* 🔹 Added Profile link */}
              <button onClick={handleLogout}>Logout</button>
            </>
          )}
        </nav>
      </header>

      <main>
        <Routes>
          <Route path="/" element={<Navigate to="/home" replace />} />
          <Route path="/home" element={<Home />} />
          <Route path="/login" element={<Login setIsLoggedIn={setIsLoggedIn} />} />
          <Route path="/register" element={<Register setIsLoggedIn={setIsLoggedIn} />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/bar/:barId" element={<BarSession />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
