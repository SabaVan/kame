import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './Form.css';
import { authService } from './authService';

export default function Register({ setIsLoggedIn }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();

  const handleRegister = (e) => {
    e.preventDefault();
    const result = authService.register({ username, password });
    if (result.success) {
      setIsLoggedIn(true);
      navigate('/dashboard');
    } else {
      alert(result.error);
    }
  };

  return (
    <div className="form-container">
      <h2>Register</h2>
      <form onSubmit={handleRegister} className="form">
        <input type="text" placeholder="Choose a username" value={username} onChange={e => setUsername(e.target.value)} required />
        <input type="password" placeholder="Choose a password" value={password} onChange={e => setPassword(e.target.value)} required />
        <button type="submit">Register</button>
        <p>
          Already have an account? Click <Link to="/login">here</Link> to log in.
        </p>
      </form>
    </div>
  );
}
