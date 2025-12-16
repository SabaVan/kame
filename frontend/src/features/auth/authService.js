import { API_URL } from '@/api/client';

export const authService = {
  register: async ({ username, password }) => {
    try {
      const res = await fetch(`${API_URL}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
        credentials: 'include', // important for session cookies
      });
      const data = await res.json();
      return res.ok ? { success: true, user: data } : { success: false, error: data };
    } catch (err) {
      return { success: false, error: err.message };
    }
  },

  login: async ({ username, password }) => {
    try {
      const res = await fetch(`${API_URL}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
        credentials: 'include', // important for session cookies
      });
      const data = await res.json();
      return res.ok ? { success: true, user: data } : { success: false, error: data };
    } catch (err) {
      return { success: false, error: err.message };
    }
  },

  logout: async () => {
    try {
      const res = await fetch(`${API_URL}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
      });
      return res.ok ? { success: true } : { success: false };
    } catch (err) {
      return { success: false, error: err.message };
    }
  },

  isUserLoggedIn: async () => {
    try {
      const res = await fetch(`${API_URL}/api/auth/current-user-id`, {
        method: 'GET',
        credentials: 'include',
      });
      const data = await res.json();
      if (data.userId == null) {
        return false;
      }
      return true;
    } catch (err) {
      console.log('Error | ', err.message);
      return false;
    }
  },

  getCurrentUserId: async () => {
    try {
      const res = await fetch(`${API_URL}/api/auth/current-user-id`, {
        method: 'GET',
        credentials: 'include',
      });
      const data = await res.json();
      return res.ok ? { success: true, userId: data } : { success: false, error: data };
    } catch (err) {
      return { success: false, error: err.message };
    }
  },
};
