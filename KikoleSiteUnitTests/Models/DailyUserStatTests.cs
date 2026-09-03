using System;
using System.Collections.Generic;
using FluentAssertions;
using KikoleSite.Models;
using KikoleSite.Models.Dtos;
using Xunit;

namespace KikoleSiteUnitTests.Models
{
    /// <summary>
    /// Statistique d'une journee pour un utilisateur. Deux constructeurs coexistent :
    /// celui du createur du jour, et celui du joueur ordinaire.
    /// </summary>
    public class DailyUserStatTests
    {
        private static readonly DateTime Day = new DateTime(2026, 9, 2);
        private const ulong Me = 7;

        private static LeaderDto Leader(ulong userId, ushort points, int minutes, bool sameDay = true)
        {
            return LeaderDtoBuilder.Valid().WithUserId(userId).WithPoints(points).WithTime(minutes).WithProposalDate(Day).WithCreationDate(sameDay ? Day.AddMinutes(minutes) : Day.AddDays(2)).Build();
        }

        private static DailyUserStat Stat(
            IEnumerable<LeaderDto> leaders, LeaderDto? mine, bool attempt = true, bool attemptDayOne = true)
        {
            return new DailyUserStat(Me, Day, "Zidane", attemptDayOne, attempt, leaders, mine);
        }

        // ------------------------------------------------------------- createur

        [Fact]
        public void TheCreatorVariantCarriesPointsWithoutCountingAsAnAttempt()
        {
            var stat = new DailyUserStat(Day, "Zidane", 1000);

            stat.Points.Should().Be(1000);
            stat.Date.Should().Be(Day);
            stat.Answer.Should().Be("Zidane");
            stat.Attempt.Should().BeFalse();
            stat.Success.Should().BeFalse();
            stat.Time.Should().BeNull();
        }

        // ------------------------------------------------------------- joueur

        [Fact]
        public void WhenTheAnswerWasFound_PointsTimeAndSuccessAreFilled()
        {
            var mine = Leader(Me, 800, 90);
            var stat = Stat(new[] { mine }, mine);

            stat.Success.Should().BeTrue();
            stat.SuccessDayOne.Should().BeTrue();
            stat.Points.Should().Be(800);
            stat.Time.Should().Be(new TimeSpan(1, 30, 0));
        }

        [Fact]
        public void WhenTheAnswerWasNotFound_EverythingStaysNull()
        {
            var stat = Stat(new List<LeaderDto>(), null);

            stat.Success.Should().BeFalse();
            stat.SuccessDayOne.Should().BeFalse();
            stat.Points.Should().BeNull();
            stat.Time.Should().BeNull();
        }

        [Fact]
        public void FindingOnALaterDayIsASuccessButNotASameDaySuccess()
        {
            var mine = Leader(Me, 800, 90, sameDay: false);
            var stat = Stat(new[] { mine }, mine);

            stat.Success.Should().BeTrue();
            stat.SuccessDayOne.Should().BeFalse();
        }

        [Fact]
        public void TheAttemptFlagsAreCarriedThrough()
        {
            var stat = Stat(new List<LeaderDto>(), null, attempt: true, attemptDayOne: false);

            stat.Attempt.Should().BeTrue();
            stat.AttemptDayOne.Should().BeFalse();
        }

        // ------------------------------------------------------------- positions

        [Fact]
        public void PositionsAreOneBasedAndComputedOnBothCriteria()
        {
            var mine = Leader(Me, 500, 30);
            var leaders = new[]
            {
                Leader(1, 900, 200),   // plus de points, plus lent
                mine,                  // moins de points, plus rapide
                Leader(2, 100, 300)
            };

            var stat = Stat(leaders, mine);

            stat.PointsPosition.Should().Be(2);  // 900 > 500 > 100
            stat.TimePosition.Should().Be(1);    // 30 < 200 < 300
        }

        [Fact]
        public void WhenTheUserIsAbsentFromTheBoard_PositionsAreNull()
        {
            var stat = Stat(new[] { Leader(1, 900, 200) }, null);

            stat.PointsPosition.Should().BeNull();
            stat.TimePosition.Should().BeNull();
        }

        [Fact]
        public void TiedScoresDoNotShareAPosition()
        {
            // COMPORTEMENT DOCUMENTE, different de CollectionHelper.SetPositions qui,
            // lui, gere les ex aequo. Ici la position est un simple rang dans une liste
            // triee : a points egaux, l'ordre depend de l'ordre d'arrivee.
            var mine = Leader(Me, 900, 60);
            var leaders = new[] { Leader(1, 900, 30), mine };

            var stat = Stat(leaders, mine);

            stat.PointsPosition.Should().Be(2);
        }
    }

    /// <summary>Agregats sur l'ensemble des journees d'un utilisateur.</summary>
    public class UserStatTests
    {
        private static readonly DateTime Day = new DateTime(2026, 9, 2);
        private const ulong Me = 7;

        private static DailyUserStat Played(ushort points, int minutes, bool sameDay = true)
        {
            var mine = LeaderDtoBuilder.Valid().WithUserId(Me).WithPoints(points).WithTime(minutes).WithProposalDate(Day).WithCreationDate(sameDay ? Day.AddMinutes(minutes) : Day.AddDays(2)).Build();

            return new DailyUserStat(Me, Day, "Zidane", sameDay, true, new[] { mine }, mine);
        }

        private static DailyUserStat Missed()
        {
            return new DailyUserStat(Me, Day, "Zidane", true, true, new List<LeaderDto>(), null);
        }

        private static DailyUserStat Untouched()
        {
            return new DailyUserStat(Me, Day, "Zidane", false, false, new List<LeaderDto>(), null);
        }

        private static DailyUserStat Created(int points)
        {
            return new DailyUserStat(Day, "Zidane", points);
        }

        private static UserStat Build(params DailyUserStat[] stats)
        {
            return new UserStat(stats, "joueur", Day.AddYears(-1));
        }

        [Fact]
        public void WithoutAnyDay_TheAggregatesAreEmptyRatherThanThrowing()
        {
            var stat = Build();

            stat.Attempts.Should().Be(0);
            stat.Successes.Should().Be(0);
            stat.TotalPoints.Should().Be(0);
            stat.BestTime.Should().BeNull();
            stat.BestPoints.Should().BeNull();
            stat.AverageTime.Should().BeNull();
        }

        [Fact]
        public void AttemptsCountEveryDayTouchedEvenWithoutSuccess()
        {
            var stat = Build(Played(800, 60), Missed(), Untouched());

            stat.Attempts.Should().Be(2);
            stat.Successes.Should().Be(1);
        }

        [Fact]
        public void BestPointsAndBestTimeTakeTheExtremes()
        {
            var stat = Build(Played(500, 200), Played(900, 30), Played(100, 400));

            stat.BestPoints.Should().Be(900);
            stat.BestTime.Should().Be(new TimeSpan(0, 30, 0));
        }

        [Fact]
        public void AverageTimeOnlyConsidersTheDaysActuallyFound()
        {
            var stat = Build(Played(500, 60), Played(900, 120), Missed());

            stat.AverageTime.Should().Be(new TimeSpan(1, 30, 0));
        }

        [Fact]
        public void TheSameDayAggregatesExcludeCatchUpAnswers()
        {
            var stat = Build(Played(800, 60), Played(400, 120, sameDay: false));

            stat.Successes.Should().Be(2);
            stat.SuccessesDayOne.Should().Be(1);
            stat.BestPoints.Should().Be(800);
            stat.BestPointsDayOne.Should().Be(800);
            stat.TotalPoints.Should().Be(1200);
            stat.TotalPointsDayOne.Should().Be(800);
        }

        [Fact]
        public void SubmittedPlayersCountInTheTotalButNotInTheSuccesses()
        {
            // le commentaire du code est explicite : "player creation NOT included"
            // pour les compteurs, "included" pour les totaux de points
            var stat = Build(Played(800, 60), Created(1000));

            stat.Attempts.Should().Be(1);
            stat.Successes.Should().Be(1);
            stat.TotalPoints.Should().Be(1800);
        }

        [Fact]
        public void TheLoginAndRegistrationDateAreCarriedThrough()
        {
            var stat = Build(Played(800, 60));

            stat.Login.Should().Be("joueur");
            stat.RegistrationDate.Should().Be(Day.AddYears(-1));
            stat.Stats.Should().HaveCount(1);
        }
    }
}
