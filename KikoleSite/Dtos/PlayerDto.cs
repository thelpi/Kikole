using System;

namespace KikoleSite.Dtos
{
    public class PlayerDto
    {
        public uint Id { get; set; }
        public string UrlName { get; set; }
        public string RealName { get; set; }
        public string SurName { get; set; }
        public string ControlStyle { get; set; }
        public string Color { get; set; }
        public bool IsBanned { get; set; }
        public string Country { get; set; }
        public int? MinYearOfBirth { get; set; }
        public int? MaxYearOfBirth { get; set; }

        public bool IsSame(string url)
        {
            return UrlName.Equals(url, StringComparison.InvariantCultureIgnoreCase);
        }

        public PlayerDto WithRealYearOfBirth(PlayerDto former)
        {
            if (former != null
                && MinYearOfBirth.HasValue && MaxYearOfBirth.HasValue
                && former.MinYearOfBirth.HasValue && former.MaxYearOfBirth.HasValue)
            {
                if (former.MaxYearOfBirth == MinYearOfBirth)
                {
                    MaxYearOfBirth = null;
                }
                else if (former.MinYearOfBirth == MaxYearOfBirth)
                {
                    MinYearOfBirth = MaxYearOfBirth;
                    MaxYearOfBirth = null;
                }
            }

            return this;
        }
    }
}
