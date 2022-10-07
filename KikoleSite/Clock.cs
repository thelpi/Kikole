using System;
using KikoleSite.Interfaces;

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
        public DateTime FirstOfMonth => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        /// <inheritdoc />
        public bool IsTomorrowIn(int minutes)
        {
            return Now.AddMinutes(minutes) >= Tomorrow;
        }
    }
}
