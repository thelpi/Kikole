using System;

namespace KikoleApi.Interfaces
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
