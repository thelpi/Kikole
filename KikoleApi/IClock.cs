using System;

namespace KikoleApi
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
