using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;

namespace KikoleApi.Helpers
{
    internal static class BadgeHelper
    {
        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, IReadOnlyCollection<LeaderDto>, bool>> LeadersBasedBadgeCondition
            = new Dictionary<Badges, Func<LeaderDto, IReadOnlyCollection<LeaderDto>, bool>>
            {
                {
                    Badges.OverTheTopPart1,
                    (l, ls) => l.Time == ls.Min(_ => _.Time)
                },
                {
                    Badges.OverTheTopPart2,
                    (l, ls) => l.Points == ls.Min(_ => _.Points)
                }
            };

        internal static IReadOnlyDictionary<Badges, Func<PlayerDto, bool>> PlayerBasedBadgeCondition
            = new Dictionary<Badges, Func<PlayerDto, bool>>
            {
                {
                    Badges.Archaeology,
                    p => p.YearOfBirth < 1970
                },
                {
                    Badges.WorldWarTwo,
                    p => p.YearOfBirth < 1940
                },
                {
                    Badges.ThirdWaveFeminism,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.ThirdWaveFeminism
                },
                {
                    Badges.ItsAFuckingDisgrace,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.ItsAFuckingDisgrace
                }
            };

        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, IReadOnlyCollection<IReadOnlyCollection<LeaderDto>>, bool>> LeaderBasedBadgeCondition
            = new Dictionary<Badges, Func<LeaderDto, IReadOnlyCollection<IReadOnlyCollection<LeaderDto>>, bool>>
            {
                {
                    Badges.CacaCaféClopeKikolé,
                    (l, lh) => new TimeSpan(0, l.Time, 0).Hours >= 5 && new TimeSpan(0, l.Time, 0).Hours < 8
                },
                {
                    Badges.HalfwayToTheTop,
                    (l, lh) => l.Points >= 500
                },
                {
                    Badges.ItsOver900,
                    (l, lh) => l.Points >= 900
                },
                {
                    Badges.SavedByTheBell,
                    (l, lh) => new TimeSpan(0, l.Time, 0).Hours == 23
                },
                {
                    Badges.StayUpLate,
                    (l, lh) => new TimeSpan(0, l.Time, 0).Hours < 2
                },
                {
                    Badges.YourActualFirstSuccess,
                    (l, lh) => l.Points > 0
                },
                {
                    Badges.YourFirstSuccess,
                    (l, lh) => true
                },
                {
                    Badges.ThreeInARow,
                    (l, lh) => lh.Take(2).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                },
                {
                    Badges.AWeekInARow,
                    (l, lh) => lh.Take(6).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                },
                {
                    Badges.LegendTier,
                    (l, lh) => lh.Count >= 29 && lh.Take(29).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                }
            };
    }
}
