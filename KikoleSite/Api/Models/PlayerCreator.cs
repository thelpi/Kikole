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

        internal PlayerCreator(UserDto requestUser, PlayerDto player, UserDto creatorUser)
        {
            PlayerId = player.Id;
            Login = creatorUser.UserTypeId == (ulong)UserTypes.PowerUser
                ? creatorUser.Login
                : null;
            Name = player.CreationUserId == requestUser.Id || requestUser.UserTypeId == (ulong)UserTypes.Administrator
                ? player.Name
                : null;
            CanDisplayCreator = player.HideCreator == 0;
        }

        protected PlayerCreator(UserDto u, PlayerDto p)
        {
            PlayerId = p.Id;
            Login = u.Login;
            Name = p.Name;
        }
    }
}
