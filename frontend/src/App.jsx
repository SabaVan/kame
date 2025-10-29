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
  const [loading, setLoading] = useState(true); // 🔹 new state
  const navigate = useNavigate();

  useEffect(() => {
    document.title = 'Kame Bar';
  }, []);

  // 🔹 Check login state on app start
  useEffect(() => {
    const checkSession = async () => {
      try {
        const res = await authService.isUserLoggedIn();
        if (res) {
          setIsLoggedIn(true);
          localStorage.setItem('loggedIn', 'true');
        } else {
          setIsLoggedIn(false);
          localStorage.removeItem('loggedIn');
        }
      } catch (err) {
        console.error('Session check failed:', err);
        setIsLoggedIn(false);
        localStorage.removeItem('loggedIn');
      } finally {
        setLoading(false);
      }
    };

    checkSession();
  }, []);

  const handleLogout = async () => {
    await authService.logout();
    setIsLoggedIn(false);
    localStorage.removeItem('loggedIn');
    navigate('/home');
  };

  if (loading) {
    // 🔹 optional: render nothing or a spinner while checking session
    return <div>Loading...</div>;
  }

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
              <Link to="/profile">Profile</Link>
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
          <Route
            path="/dashboard"
            element={isLoggedIn ? <Dashboard /> : <Navigate to="/login" replace />}
          />
          <Route
            path="/profile"
            element={isLoggedIn ? <Profile /> : <Navigate to="/login" replace />}
          />
          <Route path="/bar/:barId" element={<BarSession />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
