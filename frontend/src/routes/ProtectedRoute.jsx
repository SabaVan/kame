import { Navigate } from 'react-router-dom';

export default function ProtectedRoute({ children }) {
  const isLoggedIn = localStorage.getItem('loggedIn') === 'true';

  if (!isLoggedIn) {
    // not logged in → go to login
    return <Navigate to="/login" replace />;
  }

  // logged in → show the protected component
  return children;
}
