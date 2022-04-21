﻿using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using Microsoft.Extensions.Localization;

namespace KikoleApi.Models.Requests
{
    /// <summary>
    /// Base class for each type of proposal request
    /// </summary>
    public abstract class BaseProposalRequest
    {
        private protected BaseProposalRequest()
        {
            ProposalDate = DateTime.Now;
        }

        /// <summary>
        /// Index of the day to substract from now to get the player ralated to this proposal
        /// </summary>
        public uint DaysBeforeNow { get; set; }

        /// <summary>
        /// The submitted value.
        /// </summary>
        public string Value { get; set; }

        internal bool IsTodayPlayer => DaysBeforeNow == 0;

        internal abstract ProposalTypes ProposalType { get; }

        internal DateTime ProposalDate { get; }

        internal DateTime PlayerSubmissionDate => ProposalDate.AddDays(-DaysBeforeNow);

        internal virtual string GetTip(PlayerDto player, IStringLocalizer resources)
        {
            return null;
        }

        internal virtual string IsValid(IStringLocalizer resources)
        {
            if (string.IsNullOrWhiteSpace(Value))
                return resources["InvalidValue"];

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
                ProposalTypeId = (ulong)ProposalType
            };
        }

        internal bool MatchAny(IEnumerable<ProposalDto> proposals)
        {
            // Assume date and user OK
            return proposals.Any(p =>
                p.ProposalTypeId == (ulong)ProposalType
                && p.Value == Value);
        }
    }
}
