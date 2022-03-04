using System;
using KikoleApi.Abstractions;

namespace KikoleApi
{
    public class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
