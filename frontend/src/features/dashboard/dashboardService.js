export async function getAllBars() {
  try {
    const response = await fetch("/api/bar"); // relative path!
    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || "Failed to fetch bars");
    }
    return await response.json();
  } catch (error) {
    console.error("Error fetching bars:", error);
    return [];
  }
}
