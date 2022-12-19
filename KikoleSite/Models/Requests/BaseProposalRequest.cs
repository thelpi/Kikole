using System;
using System.Collections.Generic;
using System.Linq;
using KikoleSite.Models.Dtos;
using KikoleSite.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleSite.Models.Requests
{
    /// <summary>
    /// Base class for each type of proposal request
    /// </summary>
    public abstract class BaseProposalRequest
    {
        /// <summary>
        /// Index of the day to substract from now to get the player related to this proposal
        /// </summary>
        public uint DaysBeforeNow { get; private set; }

        /// <summary>
        /// The submitted value.
        /// </summary>
        public string Value { get; private set; }

        internal string Ip { get; private set; }

        internal DateTime ProposalDateTime { get; private set; }

        internal bool IsTodayPlayer => DaysBeforeNow == 0;

        internal abstract ProposalTypes ProposalType { get; }

        internal DateTime PlayerSubmissionDate => ProposalDateTime.AddDays(-DaysBeforeNow).Date;

        internal virtual string GetTip(PlayerDto player, IStringLocalizer resources)
        {
            return null;
        }

        internal ProposalDto ToDto(ulong userId, bool successful)
        {
            return new ProposalDto
            {
                ProposalDate = PlayerSubmissionDate,
                Successful = (byte)(successful ? 1 : 0),
                UserId = userId,
                Value = Value?.ToString(),
                ProposalTypeId = (ulong)ProposalType,
                Ip = Ip
            };
        }

        internal bool MatchAny(IEnumerable<ProposalDto> proposals)
        {
            // Assume date and user OK
            return proposals.Any(p =>
                p.ProposalTypeId == (ulong)ProposalType
                && p.Value == Value);
        }

        /// <summary>
        /// Static ctor.
        /// </summary>
        /// <param name="now">Date of the proposal.</param>
        /// <param name="value">Proposal value.</param>
        /// <param name="daysBeforeNow">Days span between the player's date and the proposal's date.</param>
        /// <param name="proposalType">Type of proposal.</param>
        /// <param name="ip">User IP.</param>
        /// <returns>A proposal request.</returns>
        public static BaseProposalRequest Create(DateTime now, string value, uint daysBeforeNow, ProposalTypes proposalType, string ip)
        {
            BaseProposalRequest request = null;
            switch (proposalType)
            {
                case ProposalTypes.Club:
                    request = new ClubProposalRequest();
                    break;
                case ProposalTypes.Clue:
                    request = new ClueProposalRequest();
                    break;
                case ProposalTypes.Leaderboard:
                    request = new LeaderboardProposalRequest();
                    break;
                case ProposalTypes.Country:
                    request = new CountryProposalRequest();
                    break;
                case ProposalTypes.Continent:
                    request = new ContinentProposalRequest();
                    break;
                case ProposalTypes.Position:
                    request = new PositionProposalRequest();
                    break;
                case ProposalTypes.Name:
                    request = new NameProposalRequest();
                    break;
                case ProposalTypes.Year:
                    request = new YearProposalRequest();
                    break;
            }
            request.Value = value;
            request.DaysBeforeNow = daysBeforeNow;
            request.Ip = ip;
            request.ProposalDateTime = now;
            return request;
        }
    }
}
