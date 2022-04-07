using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Badge
    {
        public ulong Id { get; }

        public string Name { get; }

        public string Description { get; }

        public int Users { get; }

        public bool Hidden { get; }

        public bool Unique { get; }

        public ulong? SubBadgeId { get; }

        internal Badge(BadgeDto dto, int usersCount, string description)
        {
            Id = dto.Id;
            Name = dto.Name;
            Description = description ?? dto.Description;
            Users = usersCount;
            Hidden = dto.Hidden > 0;
            Unique = dto.IsUnique > 0;
            SubBadgeId = dto.SubBadgeId;
        }
    }
}
