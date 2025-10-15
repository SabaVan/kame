import { useEffect, useState } from 'react';
import { getAllBars } from './dashboardService';

export default function Dashboard() {
  const [bars, setBars] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchBars() {
      setLoading(true);
      const barsData = await getAllBars();
      setBars(barsData);
      setLoading(false);
    }
    fetchBars();
  }, []);

  if (loading) return <p>Loading bars...</p>;

  return (
    <div>
      <h2>Dashboard</h2>
      {bars.length === 0 ? (
        <p>No bars found.</p>
      ) : (
        <ul>
          {bars.map((bar) => (
            <li key={bar.id}>
              {bar.name} : {bar.state}
              <ul>
                <li>Open At: {bar.openAt}</li>
                <li>Close At: {bar.closeAt}</li>
              </ul>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
