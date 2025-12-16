import React, { useState, useEffect } from 'react';
import '../styles/bidModal.css';

const BidModal = ({ visible, song, onClose, onSubmit, submitting, initialAmount = 1 }) => {
  const [amount, setAmount] = useState(String(initialAmount || 1));

  useEffect(() => {
    if (visible) setAmount(String(initialAmount || 1));
  }, [visible, song, initialAmount]);

  if (!visible || !song) return null;

  return (
    <div className="bidmodal-overlay" onMouseDown={onClose}>
      <div className="bidmodal" onMouseDown={(e) => e.stopPropagation()} role="dialog" aria-modal="true">
        <h3>Place bid for</h3>
        <div className="bidmodal-song">
          <strong>{song.title}</strong> — {song.artist}
        </div>

        <label className="bidmodal-label">Amount (credits)</label>
        <input
          className="bidmodal-input"
          type="number"
          min={initialAmount}
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          autoFocus
        />

        <div className="bidmodal-actions">
          <button className="bidmodal-cancel" onClick={onClose} disabled={submitting}>
            Cancel
          </button>
          <button
            className="bidmodal-submit"
            onClick={() => onSubmit(parseInt(amount, 10))}
            disabled={submitting || isNaN(parseInt(amount, 10)) || parseInt(amount, 10) < Number(initialAmount)}
          >
            {submitting ? 'Placing...' : `Place Bid (min ${initialAmount})`}
          </button>
        </div>
      </div>
    </div>
  );
};

export default BidModal;
