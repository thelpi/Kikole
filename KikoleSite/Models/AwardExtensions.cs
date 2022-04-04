using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Localization;

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

        public static string GetValue(this IEnumerable<Api.BaseAward> awards, int position, IViewLocalizer localizer)
        {
            var awd = awards.First(pa => pa.Position == position);
            var type = awd.GetType();
            if (type == typeof(Api.PointsAward))
                return (awd as Api.PointsAward).GetValue(localizer);
            else if (type == typeof(Api.TimeAward))
                return (awd as Api.TimeAward).GetValue(localizer);
            else if (type == typeof(Api.CountAward))
                return (awd as Api.CountAward).GetValue(localizer);
            else if (type == typeof(Api.KikoleAward))
                return (awd as Api.KikoleAward).GetValue(localizer);
            else
                throw new System.NotImplementedException();
        }

        private static string GetValue(this Api.PointsAward awd, IViewLocalizer localizer)
        {
            return string.Format(localizer["Points"].Value, awd.Points);
        }

        private static string GetValue(this Api.TimeAward awd, IViewLocalizer localizer)
        {
            return $"{awd.Time.ToNaString()} ({awd.PlayerName})";
        }

        private static string GetValue(this Api.CountAward awd, IViewLocalizer localizer)
        {
            return string.Format(localizer["KikolesCount"].Value, awd.Count);
        }

        private static string GetValue(this Api.KikoleAward awd, IViewLocalizer localizer)
        {
            return string.Format(localizer["PtsByPlayer"].Value, awd.AveragePoints);
        }
    }
}
