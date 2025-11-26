import React from 'react';
import '../styles/sidebar.css';

const Sidebar = ({ users, onLeaveBar }) => {
  return (
    <div className="sidebar">
      <button className="leave-bar-btn" onClick={onLeaveBar}>
        Leave Bar
      </button>

      <h3>Users</h3>
      <div className="users-list">
        {users.length === 0
          ? 'No users in this bar.'
          : users.map((u) => (
              <div key={u.id} className="user-card">
                {u.username}
              </div>
            ))}
      </div>
    </div>
  );
};

export default Sidebar;
