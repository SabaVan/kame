import axios from 'axios';
import { API_URL } from '@/api/client';

export async function getLoggedInState() {
  try {
    const res = await axios.get(`${API_URL}/api/auth/current-user-id`, { withCredentials: true });
    return res.status === 200 && !!res.data;
  } catch {
    return false;
  }
}
