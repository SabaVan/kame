import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './Form.css';
import { authService } from './authService';

export default function Login({ setIsLoggedIn }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();

  const handleLogin = (e) => {
    e.preventDefault();
    const result = authService.login({ username, password });
    if (result.success) {
      setIsLoggedIn(true);
      navigate('/dashboard');
    } else {
      alert(result.error);
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
