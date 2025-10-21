// src/main.jsx
import { StrictMode, createContext } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import * as signalR from "@microsoft/signalr";
import App from "@/App.jsx";
import "@/styles/index.css";

// Create a context to provide the SignalR connection
export const SignalRContext = createContext(null);

// Initialize SignalR connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5023/barHub")
  .withAutomaticReconnect()
  .build();

connection.start().catch(err => console.error("SignalR Connection Error:", err));

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <BrowserRouter>
      <SignalRContext.Provider value={connection}>
        <App />
      </SignalRContext.Provider>
    </BrowserRouter>
  </StrictMode>
);
