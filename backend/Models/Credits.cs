namespace backend.Models
{
    public struct Credits
    {
        public int Total { get; set; }
        public Credits(int initialAmount = 10)
        {
            Total = initialAmount;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= Total)
            {
                Total -= amount;
                return true;
            }
            return false;
        }

        public void Add(int amount)
        {
            Total += amount;
        }
    }
}