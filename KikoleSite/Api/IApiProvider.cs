﻿using System;
using System.Threading.Tasks;

namespace KikoleSite.Api
{
    public interface IApiProvider
    {
        Task<(bool success, string value)> CreateAccountAsync(string login,
            string password, string question, string answer);

        Task<ProposalResponse> SubmitProposalAsync(DateTime proposalDate,
            string value, int daysBefore, ProposalType proposalType, string authToken);

        Task<(bool success, string value)> LoginAsync(string login, string password);
    }
}