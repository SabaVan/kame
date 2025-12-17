import { useState, useEffect } from 'react';
import { Routes, Route, Link, Navigate, useNavigate } from 'react-router-dom';

import Register from '@/features/auth/Register';
import Login from '@/features/auth/Login';
import Home from '@/features/dashboard/Home';
import Dashboard from '@/features/dashboard/Dashboard';
import BarSession from '@/routes/BarSession';
import Profile from '@/features/profile/Profile';
import { authService } from '@/features/auth/authService';

import '@/App.css';
import Loading from '@/components/Loading';

function App() {
  const [isLoggedIn, setIsLoggedIn] = useState(() => {
    return sessionStorage.getItem('loggedIn') === 'true';
  });
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // Check server session on page load
  useEffect(() => {
    document.title = 'Kame';

    const savedLoggedIn = sessionStorage.getItem('loggedIn') === 'true';
    const savedProfile = sessionStorage.getItem('profile');

    // Show user as logged in immediately if we have sessionStorage data
    if (savedLoggedIn && savedProfile) {
      setIsLoggedIn(true);
    }

    const checkSession = async () => {
      try {
        const loggedIn = await authService.isUserLoggedIn();
        setIsLoggedIn(loggedIn);

        if (loggedIn) {
          sessionStorage.setItem('loggedIn', 'true');
        } else {
          setIsLoggedIn(false);
          sessionStorage.removeItem('loggedIn');
          sessionStorage.removeItem('profile');
        }
      } catch (err) {
        console.error('Session check failed:', err);
        // On network error, check if we have sessionStorage data
        const hasProfile = sessionStorage.getItem('profile') !== null;
        setIsLoggedIn(hasProfile);
      } finally {
        setLoading(false);
      }
    };

    checkSession();
  }, []);

  const handleLogout = async () => {
    try {
      await authService.logout();
      setIsLoggedIn(false);
      sessionStorage.clear();
      navigate('/home');
    } catch (err) {
      console.error('Logout failed:', err);
    }
  };

  if (loading) return <Loading fullScreen />;

  return (
    <div id="root">
      <header>
        <div className="logo-container">
          <img alt="kame" src="/kame.svg" className="logo" />
          <h1>
            <Link to="/home" className="logo-link">Kame</Link>
          </h1>
        </div>

        <nav>
          {/* Dashboard is now visible to everyone! */}
          <Link to="/dashboard">Dashboard</Link>

          {!isLoggedIn ? (
            <>
              <Link to="/login">Login</Link>
              <Link to="/register">Register</Link>
            </>
          ) : (
            <>
              <Link to="/profile">Profile</Link>
              <button onClick={handleLogout} className="logout-btn">Logout</button>
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

          {/* FIX: Dashboard is now accessible to all users */}
          <Route path="/dashboard" element={<Dashboard />} />

          {/* Profile remains protected */}
          <Route path="/profile" element={isLoggedIn ? <Profile /> : <Navigate to="/login" replace />} />

          {/* FIX: Protect the Bar Session - users must log in to join the party */}
          <Route path="/bar/:barId" element={isLoggedIn ? <BarSession /> : <Navigate to="/login" replace />} />
        </Routes>
      </main>
    </div>
  );
}
export default App;
