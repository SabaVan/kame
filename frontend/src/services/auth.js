// src/services/auth.js
import axios from 'axios';

/**
 * Check if the current user is logged in.
 * @returns {Promise<boolean>} true if logged in, false otherwise
 */
export async function getLoggedInState() {
  try {
    const res = await axios.get('/api/auth/current-user-id', { withCredentials: true });
    return res.status === 200 && !!res.data;
  } catch {
    return false;
  }
}
