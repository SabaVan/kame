using backend.Models;
using Xunit;

namespace backend.Tests.Models
{
    public class CreditsTests
    {
        [Fact]
        public void Constructor_SetsInitialAmount_WhenProvided()
        {
            var credits = new Credits(250);
            Assert.Equal(250, credits.Total);
        }

        [Fact]
        public void Constructor_UsesDefaultInitialAmount_WhenNotProvided()
        {
            var credits = new Credits();
            Assert.Equal(100, credits.Total);
        }

        [Fact]
        public void TrySpend_Succeeds_WhenAmountIsAvailable()
        {
            var credits = new Credits(200);

            bool result = credits.TrySpend(50);

            Assert.True(result);
            Assert.Equal(150, credits.Total);
        }

        [Fact]
        public void TrySpend_Fails_WhenAmountExceedsTotal()
        {
            var credits = new Credits(80);

            bool result = credits.TrySpend(100);

            Assert.False(result);
            Assert.Equal(80, credits.Total); // unchanged
        }

        [Fact]
        public void TrySpend_Succeeds_WhenAmountEqualsTotal()
        {
            var credits = new Credits(40);

            bool result = credits.TrySpend(40);

            Assert.True(result);
            Assert.Equal(0, credits.Total);
        }

        [Fact]
        public void Add_IncreasesTotal()
        {
            var credits = new Credits(30);

            credits.Add(50);

            Assert.Equal(80, credits.Total);
        }

        [Fact]
        public void MultipleOperations_WorkCorrectly()
        {
            var credits = new Credits(100);

            credits.Add(40);               // 140
            credits.TrySpend(20);          // 120
            bool result = credits.TrySpend(200);

            Assert.False(result);
            Assert.Equal(120, credits.Total);
        }
    }
}