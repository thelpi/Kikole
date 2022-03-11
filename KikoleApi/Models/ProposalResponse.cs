using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Helpers;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Requests;

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

        private ProposalResponse(ProposalType proposalType,
            string sourceValue,
            bool? success,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
        {
            ProposalType = proposalType;

            if (success.HasValue)
                Successful = success.Value;

            switch (proposalType)
            {
                case ProposalType.Name:
                    if (!success.HasValue)
                        Successful = player.AllowedNames.Contains(sourceValue.Sanitize());
                    Value = Successful
                        ? player.Name
                        : null;
                    break;

                case ProposalType.Club:
                    var c = clubs.FirstOrDefault(_ => _.AllowedNames.Contains(sourceValue.Sanitize()));
                    if (!success.HasValue)
                        Successful = c != null;
                    Value = Successful
                        ? new PlayerClub(c, playerClubs.First(_ => _.ClubId == c.Id))
                        : null;
                    break;

                case ProposalType.Country:
                    if (!success.HasValue)
                        Successful = player.CountryId == (ulong)Enum.Parse<Country>(sourceValue);
                    Value = Successful
                        ? player.CountryId
                        : default(ulong?);
                    break;

                case ProposalType.Position:
                    if (!success.HasValue)
                        Successful = player.PositionId == ulong.Parse(sourceValue);
                    Value = Successful
                        ? player.PositionId
                        : default(ulong?);
                    break;

                case ProposalType.Year:
                    if (!success.HasValue)
                        Successful = ushort.Parse(sourceValue) == player.YearOfBirth;
                    Value = Successful
                        ? player.YearOfBirth.ToString()
                        : null;
                    break;

                case ProposalType.Clue:
                    if (!success.HasValue)
                        Successful = true;
                    Value = player.Clue;
                    break;
            }
        }

        internal ProposalResponse(BaseProposalRequest request,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
            : this(request.ProposalType, request.Value, null, player, playerClubs, clubs)
        {
            TotalPoints = request.SourcePoints;
            LostPoints = Successful
                ? 0
                : ProposalChart.Default.ProposalTypesCost[ProposalType];
            Tip = request.GetTip(player);
        }

        internal ProposalResponse(ProposalDto dto,
            PlayerDto player,
            IReadOnlyList<PlayerClubDto> playerClubs,
            IReadOnlyList<ClubDto> clubs)
            : this((ProposalType)dto.ProposalTypeId, dto.Value, dto.Successful > 0, player, playerClubs, clubs)
        { }
    }
}
