import { useState, useEffect } from 'react';
import { Routes, Route, Link, Navigate } from 'react-router-dom';
import { useNavigate } from 'react-router-dom';
import Register from '@/features/auth/Register';
import Login from '@/features/auth/Login';
import Home from '@/features/dashboard/Home';
import Dashboard from '@/features/dashboard/Dashboard';
import { authService } from '@/features/auth/authService';

import '@/App.css';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(localStorage.getItem('loggedIn') === 'true');

  useEffect(() => {
    document.title = 'Kame Bar';
  }, []);

  const navigate = useNavigate();
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
        </Routes>
      </main>
    </div>
  );
}

export default App;
