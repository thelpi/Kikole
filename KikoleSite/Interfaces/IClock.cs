using System;

namespace KikoleSite.Interfaces
{
    /// <summary>
    /// Clock interface.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Current date and hour.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Current date at midnight.
        /// </summary>
        DateTime Today { get; }

        /// <summary>
        /// Tomorrow at midnight.
        /// </summary>
        DateTime Tomorrow { get; }

        /// <summary>
        /// The first day of the current month.
        /// </summary>
        DateTime FirstOfMonth { get; }

        /// <summary>
        /// Gets it the current date plus specified minutes is tomorrow.
        /// </summary>
        /// <param name="minutes">Minutes to add.</param>
        /// <returns><c>True</c> if tomorrow.</returns>
        bool IsTomorrowIn(int minutes);
    }
}
