// Simple fake auth service
export const authService = {
  login: ({ username, password }) => {
    if (username && password) {
      localStorage.setItem('loggedIn', 'true');
      return { success: true };
    }
    return { success: false, error: 'Username and password required' };
  },

  register: ({ username, password }) => {
    if (username && password) {
      localStorage.setItem('loggedIn', 'true');
      return { success: true };
    }
    return { success: false, error: 'Username and password required' };
  },

  logout: () => {
    localStorage.removeItem('loggedIn');
  },

  isLoggedIn: () => localStorage.getItem('loggedIn') === 'true',
};
