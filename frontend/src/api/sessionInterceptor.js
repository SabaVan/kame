import axios from 'axios';

axios.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Session expired - clear storage and redirect
      sessionStorage.clear();
      localStorage.clear();
      window.location.href = '/login?expired=true';
    }
    return Promise.reject(error);
  }
);
