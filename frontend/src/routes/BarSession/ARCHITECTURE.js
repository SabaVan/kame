import React from 'react';
import Sidebar from './components/Sidebar';
import CurrentSong from './components/CurrentSong';
import SearchSection from './components/SearchSection';
import PlaylistSection from './components/PlaylistSection';

/**
 * VISUAL COMPONENT TREE
 * 
 * BarSession (Container)
 * ├── State Management (useState)
 * ├── Custom Hooks (5 hooks)
 * │   ├── useSignalRConnection
 * │   ├── useSignalRListeners
 * │   ├── usePlaylistData
 * │   ├── useBarActions
 * │   └── useSongSearch
 * │
 * └── Render
 *     ├── <Sidebar />
 *     │   ├── User cards
 *     │   └── Leave button
 *     │
 *     ├── <CurrentSong />
 *     │   └── Now playing info
 *     │
 *     ├── <SearchSection />
 *     │   ├── Search form
 *     │   └── Results list
 *     │
 *     └── <PlaylistSection />
 *         ├── Song cards
 *         │   ├── Song info
 *         │   ├── Bid input
 *         │   └── Bid button
 *         └── Empty state
 */

/**
 * DATA FLOW DIAGRAM
 * 
 * API (Backend)
 *   ↓
 * Custom Hooks (fetch, update)
 *   ↓
 * Component State (useState)
 *   ↓
 * Components (pure, render only)
 *   ↓
 * User Interactions (callbacks)
 *   ↓
 * API Calls (via hooks)
 *   ↓
 * [Loop]
 */

/**
 * HOOK RESPONSIBILITIES
 * 
 * useSignalRConnection
 * └─ Creates/destroys SignalR connection
 * 
 * useSignalRListeners
 * └─ Manages event subscriptions
 * └─ Handles reconnection logic
 * 
 * usePlaylistData
 * └─ fetch playlist
 * └─ sort by position
 * 
 * useBarActions
 * └─ fetch users
 * └─ add song
 * └─ place bid
 * └─ leave bar
 * 
 * useSongSearch
 * └─ manage search state
 * └─ filter duplicates
 */

/**
 * COMPONENT RESPONSIBILITIES
 * 
 * Sidebar
 * └─ Display: users list, leave button
 * └─ Callbacks: onLeaveBar
 * 
 * CurrentSong
 * └─ Display: current song info
 * └─ Props: currentSong
 * 
 * SearchSection
 * └─ Display: search form, results
 * └─ Callbacks: onSearch, onAddSong
 * 
 * PlaylistSection
 * └─ Display: playlist songs, bid controls
 * └─ Callbacks: onPlaceBid, onBidAmountChange
 */

// This file is for visual reference only
export const ARCHITECTURE_GUIDE = 'See component tree above';
