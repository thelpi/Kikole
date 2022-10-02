using KikoleSite.Models.Dtos;

namespace KikoleSite.Models
{
    public class User
    {
        public ulong Id { get; }

        public string Login { get; }

        internal User(UserDto user)
        {
            Id = user.Id;
            Login = user.Login;
        }
    }
}
