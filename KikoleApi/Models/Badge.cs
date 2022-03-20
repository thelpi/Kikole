using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class Badge
    {
        public ulong Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ulong Users { get; set; }

        internal Badge(BadgeDto dto)
        {
            Id = dto.Id;
            Name = dto.Name;
            Description = dto.Description;
            Users = dto.Users;
        }
    }
}
