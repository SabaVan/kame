export async function getAllBars() {
  try {
    const response = await fetch('/api/bar/all'); // relative path!
    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || 'Failed to fetch bars');
    }
    return await response.json();
  } catch (error) {
    console.error('Error fetching bars:', error);
    return [];
  }
}
export async function getDefaultBar() {
  try {
    const response = await fetch('/api/bar/default'); // relative path!
    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || 'Failed to fetch bars');
    }
    return await response.json();
  } catch (error) {
    console.error('Error fetching bars:', error);
    return [];
  }
}
export async function joinBar(barId) {
  try {
    const response = await fetch(`/api/bar/${barId}/join`, {
      method: 'POST',
      credentials: 'include', // send cookies if JWT/session stored in cookie
    });
    if (!response.ok) {
      const msg = await response.text();
      throw new Error(msg || 'Failed to join bar');
    }
    return await response.text();
  } catch (err) {
    console.error(err);
    throw err;
  }
}

export async function leaveBar(barId) {
  try {
    const response = await fetch(`/api/bar/${barId}/leave`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!response.ok) {
      const msg = await response.text();
      throw new Error(msg || 'Failed to leave bar');
    }
    return await response.text();
  } catch (err) {
    console.error(err);
    throw err;
  }
}
export async function getIsJoined(barId) {
  try {
    const response = await fetch(`/api/bar/${barId}/isJoined`);
    if (!response.ok) throw new Error((await response.text()) || 'Failed to check join status');
    return await response.json(); // true/false or { isJoined: true }
  } catch (error) {
    console.error('Error checking join status:', error);
    return false;
  }
}
