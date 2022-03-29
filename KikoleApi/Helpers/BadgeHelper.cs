using System;
using System.Collections.Generic;
using System.Linq;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;

namespace KikoleApi.Helpers
{
    internal static class BadgeHelper
    {
        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, IReadOnlyCollection<LeaderDto>, bool>> LeadersBasedBadgeCondition
            = new Dictionary<Badges, Func<LeaderDto, IReadOnlyCollection<LeaderDto>, bool>>
            {
                {
                    Badges.OverTheTopPart1,
                    (l, ls) => l.Time == ls.Min(_ => _.Time) && ls.Count(_ => _.Time == l.Time) == 1
                },
                {
                    Badges.OverTheTopPart2,
                    (l, ls) => l.Points == ls.Max(_ => _.Points) && ls.Count(_ => _.Points == l.Points) == 1
                }
            };

        internal static IReadOnlyDictionary<Badges, Func<LeaderDto, IReadOnlyCollection<LeaderDto>, bool>> LeadersBasedBadgeNonUniqueCondition
            = new Dictionary<Badges, Func<LeaderDto, IReadOnlyCollection<LeaderDto>, bool>>
            {
                {
                    Badges.OverTheTopPart1,
                    (l, ls) => l.Time == ls.Min(_ => _.Time)
                },
                {
                    Badges.OverTheTopPart2,
                    (l, ls) => l.Points == ls.Max(_ => _.Points)
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
                },
                {
                    Badges.CaptainTsubasa,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.CaptainTsubasa
                },
                {
                    Badges.KikolesCreatorFriend,
                    p => p.BadgeId.HasValue && p.BadgeId == (ulong)Badges.KikolesCreatorFriend
                }
            };

        internal static IReadOnlyDictionary<Badges, Func<IReadOnlyCollection<PlayerDto>, bool>> PlayersHistoryBadgeCondition
            = new Dictionary<Badges, Func<IReadOnlyCollection<PlayerDto>, bool>>
            {
                {
                    Badges.FourFourtwo,
                    ph => ph.Count(p => p.PositionId == (ulong)Positions.Goalkeeper) > 0
                        && ph.Count(p => p.PositionId == (ulong)Positions.Defender) > 3
                        && ph.Count(p => p.PositionId == (ulong)Positions.Midfielder) > 3
                        && ph.Count(p => p.PositionId == (ulong)Positions.Forward) > 1
                },
                {
                    Badges.AroundTheWorld,
                    ph => ph.Select(p => p.CountryId).Distinct().Count() >= 20
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
                    Badges.WoodenSpoon,
                    (l, lh) => l.Points == 0
                },
                {
                    Badges.YourFirstSuccess,
                    (l, lh) => true
                },
                {
                    Badges.ThreeInARow,
                    (l, lh) => lh.Count >= 2 && lh.Take(2).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                },
                {
                    Badges.AWeekInARow,
                    (l, lh) => lh.Count >= 6 && lh.Take(6).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                },
                {
                    Badges.LegendTier,
                    (l, lh) => lh.Count >= 29 && lh.Take(29).All(ol => ol.Select(_ => _.UserId).Contains(l.UserId))
                },
                {
                    Badges.MakeItDouble,
                    (l, lh) => l.Points == 1000 && lh.Take(1).All(ol => ol.Any(_ => _.UserId == l.UserId && _.Points == 1000))
                }
            };
    }
}
