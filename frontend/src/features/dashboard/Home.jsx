import React from 'react';
import { Link } from 'react-router-dom';
import './home.css';

export default function Home() {
  return (
    <div className="home-container">
      <section className="hero">
        <div className="hero-content">
          <span className="badge">Welcome to Kame</span>
          <h1>
            Your favorite tracks, <br /> <span className="highlight">shared with friends.</span>
          </h1>
          <p className="subtext">
            Kame is a simple way to influence the music at your favorite spots. Join a bar, see what's playing, and bid
            a few credits to hear your song next.
          </p>

          <div className="hero-btns">
            <Link to="/register" className="btn-primary">
              Create Account
            </Link>
            <Link to="/dashboard" className="btn-secondary">
              View Bars
            </Link>
          </div>

          <div className="promo-text">
            <span>
              New members start with <strong>100 credits</strong> ✨
            </span>
          </div>
        </div>
      </section>

      <section className="features-grid">
        <div className="feature-card">
          <h4>Stay in Sync</h4>
          <p>Real-time updates mean you're always listening together.</p>
        </div>
        <div className="feature-card">
          <h4>Support your Song</h4>
          <p>Use credits to move your favorites higher in the queue.</p>
        </div>
      </section>
    </div>
  );
}
