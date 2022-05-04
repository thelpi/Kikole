using KikoleSite.Api.Models.Dtos;

namespace KikoleSite.Api.Models
{
    public class Badge
    {
        public ulong Id { get; }

        public string Name { get; }

        public string Description { get; }

        public int Users { get; }

        public bool Hidden { get; }

        internal Badge(BadgeDto dto, int usersCount, string description)
        {
            Id = dto.Id;
            Name = dto.Name;
            Description = description ?? dto.Description;
            Users = usersCount;
            Hidden = dto.Hidden > 0;
        }
    }
}
