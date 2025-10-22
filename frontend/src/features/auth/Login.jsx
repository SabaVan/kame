import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './Form.css';
import { authService } from './authService';

export default function Login({ setIsLoggedIn }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    const result = await authService.login({ username, password });
    if (result.success) {
      setIsLoggedIn(true);
      navigate('/dashboard');
    } else {
      const message =
        typeof result.error === 'object' ? result.error.message || JSON.stringify(result.error) : result.error;

      alert(
        (message == 'User is not authorized'
          ? 'Incorrect password. Please try again.'
          : 'Incorrect username. Please try again.') || 'Login failed. Please try again.'
      );
    }
  };

  return (
    <div className="form-container">
      <h2>Login</h2>
      <form onSubmit={handleLogin} className="form">
        <input
          type="text"
          placeholder="Username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
        />
        <input
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        <button type="submit">Log In</button>
        <p>
          Don't have an account? Click <Link to="/register">here</Link> to register.
        </p>
      </form>
    </div>
  );
}
