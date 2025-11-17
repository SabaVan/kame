using System;
using backend.Models;
using backend.Shared.Enums;
using backend.Common;
using Xunit;

namespace backend.Tests.Models
{
    public class BarTests
    {
        [Fact]
        public void SetState_ChangesState_WhenDifferent()
        {
            var bar = new Bar();
            var result = bar.SetState(BarState.Open);

            Assert.True(result.IsSuccess);
            Assert.Equal(BarState.Open, result.Value);
            Assert.Equal(BarState.Open, bar.State);
        }

        [Fact]
        public void SetState_Fails_WhenSameState()
        {
            var bar = new Bar();
            var result = bar.SetState(BarState.Closed);

            Assert.False(result.IsSuccess);
            Assert.Equal("BAR_ALREADY_IN_STATE", result.Error?.Code);
            Assert.Equal(BarState.Closed, bar.State);
        }

        [Fact]
        public void SetSchedule_Succeeds_WhenOpenBeforeClose()
        {
            var bar = new Bar();
            DateTime open = new DateTime(2025, 11, 17, 9, 0, 0, DateTimeKind.Utc);
            DateTime close = new DateTime(2025, 11, 17, 17, 0, 0, DateTimeKind.Utc);

            var result = bar.SetSchedule(open, close);

            Assert.True(result.IsSuccess);
            Assert.Equal(open, bar.OpenAtUtc);
            Assert.Equal(close, bar.CloseAtUtc);
        }

        [Fact]
        public void SetSchedule_Fails_WhenOpenAfterClose()
        {
            var bar = new Bar();
            DateTime open = new DateTime(2025, 11, 17, 18, 0, 0, DateTimeKind.Utc);
            DateTime close = new DateTime(2025, 11, 17, 17, 0, 0, DateTimeKind.Utc);

            var result = bar.SetSchedule(open, close);

            Assert.False(result.IsSuccess);
            Assert.Equal("INVALID_SCHEDULE", result.Error?.Code);
        }

        [Theory]
        [InlineData(8, 20, 10, true)]
        [InlineData(8, 20, 7, false)]
        [InlineData(22, 2, 23, true)]
        [InlineData(22, 2, 1, true)]
        [InlineData(22, 2, 21, false)]
        public void ShouldBeOpen_ReturnsCorrectly(int openHour, int closeHour, int testHour, bool expected)
        {
            var bar = new Bar();

            DateTime openUtc = new DateTime(2025, 11, 17, openHour, 0, 0, DateTimeKind.Utc);
            DateTime closeUtc = closeHour > openHour
                ? new DateTime(2025, 11, 17, closeHour, 0, 0, DateTimeKind.Utc)  // same day
                : new DateTime(2025, 11, 18, closeHour, 0, 0, DateTimeKind.Utc); // next day if overnight

            bar.SetSchedule(openUtc, closeUtc);

            DateTime testTime = new DateTime(2025, 11, 17, testHour, 0, 0, DateTimeKind.Utc);
            bool isOpen = bar.ShouldBeOpen(testTime);

            Assert.Equal(expected, isOpen);
        }
    }
}