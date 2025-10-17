import { useEffect, useState } from 'react';
import { getAllBars, getDefaultBar } from './dashboardService';

export default function Dashboard() {
  const [bars, setBars] = useState([]);
  const [defaultBar, setDefaultBar] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchBars() {
      try {
        const [allBars, defaultBarData] = await Promise.all([
          getAllBars(),
          getDefaultBar(),
        ]);

        setBars(allBars);
        setDefaultBar(defaultBarData);
      } catch (error) {
        console.error('Error fetching bars:', error);
      } finally {
        setLoading(false);
      }
    }

    fetchBars();
  }, []);

  if (loading) return <p>Loading bars...</p>;

  return (
    <div>
      <h2>Dashboard</h2>

      {defaultBar ? (
        <div>
          <h3>Default Bar</h3>
          <p>{defaultBar.name} : {defaultBar.state}</p>
          <ul>
            <li>Open At: {defaultBar.openAt}</li>
            <li>Close At: {defaultBar.closeAt}</li>
          </ul>
        </div>
      ) : (
        <p>No default bar found.</p>
      )}

      <h3>All Bars</h3>
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
