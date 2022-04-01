using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Models
{
    public static class AwardExtensions
    {
        public static bool HasPosition(this IEnumerable<Api.BaseAward> awards, int position)
        {
            return awards.Any(pa => pa.Position == position);
        }

        public static string ToListOfNames(this IEnumerable<Api.BaseAward> awards, int position)
        {
            return string.Join(" / ", awards.Where(pa => pa.Position == position).Select(pa => pa.Name));
        }

        public static string GetValue(this IEnumerable<Api.BaseAward> awards, int position)
        {
            var type = awards.First().GetType();
            if (type == typeof(Api.PointsAward))
                return awards.Cast<Api.PointsAward>().GetValue(position);
            else if (type == typeof(Api.TimeAward))
                return awards.Cast<Api.TimeAward>().GetValue(position);
            else if (type == typeof(Api.CountAward))
                return awards.Cast<Api.CountAward>().GetValue(position);
            else if (type == typeof(Api.KikoleAward))
                return awards.Cast<Api.KikoleAward>().GetValue(position);
            else
                throw new System.NotImplementedException();
        }

        private static string GetValue(this IEnumerable<Api.PointsAward> awards, int position)
        {
            return $"{awards.First(pa => pa.Position == position).Points} points";
        }

        private static string GetValue(this IEnumerable<Api.TimeAward> awards, int position)
        {
            var firstPos = awards.First(pa => pa.Position == position);
            return $"{firstPos.Time.ToString(Helper.TimeSpanPattern)} ({firstPos.PlayerName})";
        }

        private static string GetValue(this IEnumerable<Api.CountAward> awards, int position)
        {
            return $"{awards.First(pa => pa.Position == position).Count} kikolés";
        }

        private static string GetValue(this IEnumerable<Api.KikoleAward> awards, int position)
        {
            return $"{awards.First(pa => pa.Position == position).AveragePoints} pts / player";
        }
    }
}
