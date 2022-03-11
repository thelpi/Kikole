using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Models
{
    public class ProposalResponse
    {
        private int _lostPoints;

        public bool Successful { get; set; }

        public object Value { get; set; }

        public string Tip { get; set; }

        public int LostPoints
        {
            get { return _lostPoints; }
            set
            {
                _lostPoints = value;
                TotalPoints -= _lostPoints;
                TotalPoints = System.Math.Max(TotalPoints, 0);
            }
        }

        public int TotalPoints { get; internal set; }

        public ProposalType ProposalType { get; set; }

        internal ProposalResponse() { }

        internal ProposalResponse(ProposalDto dto,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            bool? successful = dto.Successful > 0;
            ProposalType = (ProposalType)dto.ProposalTypeId;
            Value = GetValueFromProposalType(ProposalType, dto.Value, ref successful, player, playerClubs, clubs);
            Successful = successful.Value;
        }

        internal static object GetValueFromProposalType(ProposalType proposalType,
            string sourceValue,
            ref bool? success,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            switch (proposalType)
            {
                case ProposalType.Name:
                    if (!success.HasValue)
                        success = player.AllowedNames.Contains(sourceValue.Sanitize());
                    return success.Value
                        ? player.Name
                        : null;

                case ProposalType.Club:
                    var c = clubs.FirstOrDefault(_ => _.AllowedNames.Contains(sourceValue.Sanitize()));
                    if (!success.HasValue)
                        success = c != null;
                    return success.Value
                        ? new PlayerClub(c, playerClubs.First(_ => _.ClubId == c.Id))
                        : null;

                case ProposalType.Country:
                    if (!success.HasValue)
                        success = player.CountryId == (ulong)Enum.Parse<Country>(sourceValue);
                    return success.Value
                        ? player.CountryId
                        : default(ulong?);

                case ProposalType.Position:
                    if (!success.HasValue)
                        success = player.PositionId == ulong.Parse(sourceValue);
                    return success.Value
                        ? player.PositionId
                        : default(ulong?);

                case ProposalType.Year:
                    if (!success.HasValue)
                        success = ushort.Parse(sourceValue) == player.YearOfBirth;
                    return success.Value
                        ? player.YearOfBirth.ToString()
                        : null;

                case ProposalType.Clue:
                    if (!success.HasValue)
                        success = true;
                    return player.Clue;
            }

            return null;
        }
    }
}
