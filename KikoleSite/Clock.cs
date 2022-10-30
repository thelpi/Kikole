using System;

namespace KikoleSite
{
    /// <summary>
    /// Clock implementation.
    /// </summary>
    /// <seealso cref="IClock"/>
    public class Clock : IClock
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;

        /// <inheritdoc />
        public DateTime Today => DateTime.Today;

        /// <inheritdoc />
        public DateTime Tomorrow => Today.AddDays(1);

        /// <inheritdoc />
        public DateTime Yesterday => Today.AddDays(-1);

        /// <inheritdoc />
        public DateTime FirstOfMonth => new DateTime(Now.Year, Now.Month, 1);

        /// <inheritdoc />
        public DateTime TomorrowEnd => Today.AddDays(2).AddSeconds(-1);

        /// <inheritdoc />
        public DateTime NowSeconds => Now.AddMilliseconds(-Now.Millisecond);

        /// <inheritdoc />
        public bool IsTomorrowIn(int minutes)
        {
            return Now.AddMinutes(minutes) >= Tomorrow;
        }
    }
}
