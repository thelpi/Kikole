﻿using System.Threading.Tasks;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Abstractions
{
    public interface IClubRepository
    {
        Task<ulong> CreateClubAsync(ClubDto club);
    }
}
