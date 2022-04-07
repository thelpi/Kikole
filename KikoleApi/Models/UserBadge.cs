using System;

namespace KikoleApi.Models
{
    public class UserBadge
    {
        private readonly Badge _badge;

        public ulong Id => _badge.Id;

        public string Name => _badge.Name;

        public string Description => _badge.Description;

        public int Users => _badge.Users;

        public bool Hidden => _badge.Hidden;

        public bool Unique => _badge.Unique;

        public ulong? SubBadgeId => _badge.SubBadgeId;

        public DateTime GetDate { get; }

        internal UserBadge(Badge badge, DateTime getDate)
        {
            _badge = badge;
            GetDate = getDate;
        }
    }
}
