using System;

namespace KikoleApi.Abstractions
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
