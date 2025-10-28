namespace backend.Models
{
    public class Credits
    {
        public int Total { get; set; }
        protected Credits() { }
        public Credits(int initialAmount = 0)
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