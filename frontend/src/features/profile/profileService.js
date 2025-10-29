const API_URL = 'http://localhost:5023/api';

export const profileService = {
  getUserProfile: async () => {
    try {
      const res = await fetch(`${API_URL}/users/profile`, {
        credentials: 'include',
      });
      const data = await res.json();
      return res.ok ? { success: true, profile: data } : { success: false, error: data };
    } catch (err) {
      return { success: false, error: err.message };
    }
  },
};