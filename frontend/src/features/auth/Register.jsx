import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './Form.css';
import { authService } from './authService';

export default function Register({ setIsLoggedIn }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();

  const handleRegister = async (e) => {
    e.preventDefault();

    // Username: 3-20 chars, letters/numbers/underscores
    const usernameRegex = /^[a-zA-Z0-9_]{3,20}$/;

    // Password: min 8 chars, at least 1 uppercase, 1 number, symbols optional
    const passwordRegex = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

    if (!usernameRegex.test(username)) {
      alert('Username must be 3-20 characters and only contain letters, numbers, or underscores.');
      return;
    }

    if (!passwordRegex.test(password)) {
      alert('Password must be at least 8 characters long, include at least one uppercase letter and one number.');
      return;
    }

    const result = await authService.register({ username, password });
    if (result.success) {
      // store minimal profile locally as fallback
      sessionStorage.setItem('profile', JSON.stringify(result.user));
      sessionStorage.setItem('loggedIn', 'true');
      setIsLoggedIn(true);
      navigate('/dashboard');
    } else {
      alert('Registration failed');
    }
  };

  return (
    <div className="form-container">
      <h2>Register</h2>
      <form onSubmit={handleRegister} className="form">
        <input
          type="text"
          placeholder="Choose a username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          required
        />
        <input
          type="password"
          placeholder="Choose a password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        <button type="submit">Register</button>
        <p>
          Already have an account? Click <Link to="/login">here</Link> to log in.
        </p>
      </form>
    </div>
  );
}
