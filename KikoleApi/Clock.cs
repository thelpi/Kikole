using System;

namespace KikoleApi
{
    public class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
