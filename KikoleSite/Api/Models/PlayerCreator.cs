using KikoleSite.Api.Models.Dtos;
using KikoleSite.Api.Models.Enums;

namespace KikoleSite.Api.Models
{
    public class PlayerCreator
    {
        public ulong PlayerId { get; }

        public string Name { get; }

        public string Login { get; }

        public bool CanDisplayCreator { get; }

        internal PlayerCreator(ulong userId, PlayerDto p, UserDto u)
        {
            PlayerId = p.Id;
            Login = u.UserTypeId == (ulong)UserTypes.PowerUser
                ? u.Login
                : null;
            Name = p.CreationUserId == userId
                ? p.Name
                : null;
            CanDisplayCreator = p.HideCreator == 0;
        }

        protected PlayerCreator(UserDto u, PlayerDto p)
        {
            PlayerId = p.Id;
            Login = u.Login;
            Name = p.Name;
        }
    }
}
