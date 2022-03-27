using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class PlayerCreator
    {
        public string Name { get; }

        public string Login { get; }

        internal PlayerCreator(ulong userId, PlayerDto p, UserDto u)
        {
            Login = u.UserTypeId == (ulong)UserTypes.PowerUser
                ? u.Login
                : null;
            Name = p.CreationUserId == userId
                ? p.Name
                : null;
        }

        protected PlayerCreator(UserDto u, PlayerDto p)
        {
            Login = u.Login;
            Name = p.Name;
        }
    }
}
