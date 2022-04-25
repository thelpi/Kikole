using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KikoleApi.Interfaces;
using KikoleApi.Interfaces.Repositories;
using KikoleApi.Interfaces.Services;
using KikoleApi.Models;
using KikoleApi.Models.Dtos;
using KikoleApi.Models.Enums;
using KikoleApi.Models.Requests;

namespace KikoleApi.Services
{
    /// <summary>
    /// Challenge service implementation.
    /// </summary>
    /// <seealso cref="IChallengeService"/>
    public class ChallengeService : IChallengeService
    {
        private readonly IChallengeRepository _challengeRepository;
        private readonly ILeaderRepository _leaderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IClock _clock;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="challengeRepository">Instance of <see cref="IChallengeRepository"/>.</param>
        /// <param name="leaderRepository">Instance of <see cref="ILeaderRepository"/>.</param>
        /// <param name="userRepository">Instance of <see cref="IUserRepository"/>.</param>
        /// <param name="playerRepository">Instance of <see cref="IPlayerRepository"/>.</param>
        /// <param name="clock">Clock service.</param>
        public ChallengeService(IChallengeRepository challengeRepository,
            ILeaderRepository leaderRepository,
            IUserRepository userRepository,
            IPlayerRepository playerRepository,
            IClock clock)
        {
            _challengeRepository = challengeRepository;
            _leaderRepository = leaderRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
            _clock = clock;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Challenge>> GetChallengesHistoryAsync(
            ulong userId,
            DateTime startDate,
            DateTime endDate)
        {
            var hostChallenges = await _challengeRepository
                .GetRequestedAcceptedChallengesAsync(userId, startDate, endDate)
                .ConfigureAwait(false);

            var guestChallenges = await _challengeRepository
                .GetResponseAcceptedChallengesAsync(userId, startDate, endDate)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>(hostChallenges.Count + guestChallenges.Count);
            var usersCache = new Dictionary<ulong, string>();

            await AddChallengesAsync(
                    challenges, usersCache, hostChallenges, c => c.GuestUserId, userId)
                .ConfigureAwait(false);

            await AddChallengesAsync(
                    challenges, usersCache, guestChallenges, c => c.HostUserId, userId)
                .ConfigureAwait(false);

            return challenges
                .OrderByDescending(c => c.ChallengeDate)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Challenge>> GetAcceptedChallengesAsync(ulong userId)
        {
            var dtos = await _challengeRepository
                .GetAcceptedChallengesAsync(_clock.Now, null)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>();
            foreach (var challenge in dtos.Where(c => c.GuestUserId == userId || c.HostUserId == userId))
            {
                var opponentUser = await _userRepository
                    .GetUserByIdAsync(userId == challenge.HostUserId
                        ? challenge.GuestUserId
                        : challenge.HostUserId)
                    .ConfigureAwait(false);

                challenges.Add(new Challenge(challenge, opponentUser.Login, userId));
            }

            return challenges
                .OrderBy(c => c.ChallengeDate)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Challenge>> GetRequestedChallengesAsync(ulong userId)
        {
            var dtos = await _challengeRepository
                .GetPendingChallengesByHostUserAsync(userId)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>(dtos.Count);
            foreach (var c in dtos)
            {
                var guestUser = await _userRepository
                    .GetUserByIdAsync(c.GuestUserId)
                    .ConfigureAwait(false);

                challenges.Add(new Challenge(c, guestUser.Login, userId));
            }

            return challenges;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<Challenge>> GetPendingChallengesAsync(ulong userId)
        {
            var dtos = await _challengeRepository
                .GetPendingChallengesByGuestUserAsync(userId)
                .ConfigureAwait(false);

            var challenges = new List<Challenge>(dtos.Count);
            foreach (var challenge in dtos)
            {
                var hostUser = await _userRepository
                    .GetUserByIdAsync(challenge.HostUserId)
                    .ConfigureAwait(false);

                challenges.Add(new Challenge(challenge, hostUser.Login, userId));
            }

            return challenges;
        }

        /// <inheritdoc />
        public async Task<ChallengeResponseError> RespondToChallengeAsync(ulong id, ulong userId, bool isAccepted)
        {
            if (id == 0)
                return ChallengeResponseError.InvalidChallengeId;

            if (userId == 0)
                return ChallengeResponseError.InvalidUser;

            var challenge = await _challengeRepository
                .GetChallengeByIdAsync(id)
                .ConfigureAwait(false);

            if (challenge == null)
                return ChallengeResponseError.ChallengeNotFound;

            var isCancel = challenge.HostUserId == userId;

            if (!isCancel && challenge.GuestUserId != userId)
                return ChallengeResponseError.CantAutoAcceptChallenge;

            if (isCancel && isAccepted)
                return ChallengeResponseError.BothAcceptedAndCancelledChallenge;

            if (isCancel && challenge.IsAccepted.HasValue)
                return ChallengeResponseError.ChallengeAlreadyAccepted;

            if (!isCancel && challenge.IsAccepted.HasValue)
                return ChallengeResponseError.ChallengeAlreadyAnswered;

            var hostUser = await _userRepository
                .GetUserByIdAsync(challenge.HostUserId)
                .ConfigureAwait(false);

            if (!isAccepted || hostUser == null)
            {
                // date is irrelevant in those cases
                await _challengeRepository
                   .RespondToChallengeAsync(id, false, _clock.Now)
                   .ConfigureAwait(false);

                return hostUser == null
                    ? ChallengeResponseError.InvalidOpponentAccount
                    : ChallengeResponseError.NoError;
            }

            var hostDates = await _challengeRepository
                .GetBookedChallengesAsync(challenge.HostUserId)
                .ConfigureAwait(false);

            var guestDates = await _challengeRepository
                .GetBookedChallengesAsync(challenge.GuestUserId)
                .ConfigureAwait(false);

            var challengeDate = await ComputeAvailableChallengeDateAsync(
                    challenge, hostDates, guestDates)
                .ConfigureAwait(false);

            await _challengeRepository
                .RespondToChallengeAsync(id, isAccepted, challengeDate)
                .ConfigureAwait(false);

            return ChallengeResponseError.NoError;
        }

        /// <inheritdoc />
        public async Task<ChallengeResponseError> CreateChallengeAsync(ChallengeRequest request, ulong userId)
        {
            var hostUser = await _userRepository
                .GetUserByIdAsync(userId)
                .ConfigureAwait(false);

            if (hostUser == null)
                return ChallengeResponseError.ChallengeHostIsInvalid;

            if (hostUser.UserTypeId == (ulong)UserTypes.Administrator)
                return ChallengeResponseError.ChallengeCreatorIsAdmin;

            var guestUser = await _userRepository
                .GetUserByIdAsync(request.GuestUserId)
                .ConfigureAwait(false);

            if (hostUser == null)
                return ChallengeResponseError.ChallengeOpponentIsInvalid;

            if (hostUser.UserTypeId == (ulong)UserTypes.Administrator)
                return ChallengeResponseError.ChallengeOpponentIsAdmin;

            var challengeAlready = await _challengeRepository
                .GetUsersFutureChallengesAsync(userId, request.GuestUserId)
                .ConfigureAwait(false);

            if (challengeAlready.Count > 0)
                return ChallengeResponseError.ChallengeAlreadyExist;

            await _challengeRepository
                .CreateChallengeAsync(request.ToDto(userId))
                .ConfigureAwait(false);

            return ChallengeResponseError.NoError;
        }

        private async Task AddChallengesAsync(List<Challenge> challenges,
            Dictionary<ulong, string> usersCache,
            IEnumerable<ChallengeDto> dtos,
            Func<ChallengeDto, ulong> getOpponentUserIdFunc,
            ulong userId)
        {
            foreach (var c in dtos)
            {
                // ChallengeDate.Value is safe here
                var leaders = await _leaderRepository
                    .GetLeadersAtDateAsync(c.ChallengeDate.Value)
                    .ConfigureAwait(false);

                var opponentUserId = getOpponentUserIdFunc(c);
                if (!usersCache.ContainsKey(opponentUserId))
                {
                    var user = await _userRepository
                        .GetUserByIdAsync(opponentUserId)
                        .ConfigureAwait(false);
                    usersCache.Add(opponentUserId, user.Login);
                }

                challenges.Add(new Challenge(c, usersCache[opponentUserId], userId, leaders));
            }
        }

        private async Task<DateTime> ComputeAvailableChallengeDateAsync(
            ChallengeDto challenge,
            IReadOnlyCollection<DateTime> hostDates,
            IReadOnlyCollection<DateTime> guestDates)
        {
            var challengeDate = _clock.Today;
            PlayerDto p;
            do
            {
                challengeDate = challengeDate.AddDays(1);

                p = await _playerRepository
                    .GetPlayerOfTheDayAsync(challengeDate)
                    .ConfigureAwait(false);
            }
            while (hostDates.Contains(challengeDate)
                || guestDates.Contains(challengeDate)
                || p.CreationUserId == challenge.GuestUserId
                || p.CreationUserId == challenge.HostUserId);

            return challengeDate;
        }
    }
}
