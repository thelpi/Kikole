using System;
using KikoleApi.Interfaces;

namespace KikoleApi.Helpers
{
    public class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
