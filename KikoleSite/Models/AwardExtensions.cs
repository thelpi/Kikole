using System.Collections.Generic;
using System.Linq;
using KikoleSite.Api.Models;
using Microsoft.AspNetCore.Mvc.Localization;

namespace KikoleSite.Models
{
    public static class AwardExtensions
    {
        public static bool HasPosition(this IEnumerable<BaseAward> awards, int position)
        {
            return awards.Any(pa => pa.Position == position);
        }

        public static string ToListOfNames(this IEnumerable<BaseAward> awards, int position)
        {
            return string.Join(" / ", awards.Where(pa => pa.Position == position).Select(pa => pa.Name));
        }

        public static string GetValue(this IEnumerable<BaseAward> awards, int position, IViewLocalizer localizer)
        {
            var awd = awards.First(pa => pa.Position == position);
            var type = awd.GetType();
            if (type == typeof(PointsAward))
                return (awd as PointsAward).GetValue(localizer);
            else if (type == typeof(TimeAward))
                return (awd as TimeAward).GetValue(localizer);
            else if (type == typeof(CountAward))
                return (awd as CountAward).GetValue(localizer);
            else if (type == typeof(KikoleAward))
                return (awd as KikoleAward).GetValue(localizer);
            else
                throw new System.NotImplementedException();
        }

        private static string GetValue(this PointsAward awd, IViewLocalizer localizer)
        {
            return string.Format(localizer["Points"].Value, awd.Points);
        }

        private static string GetValue(this TimeAward awd, IViewLocalizer localizer)
        {
            return $"{awd.Time.ToNaString()} ({awd.PlayerName})";
        }

        private static string GetValue(this CountAward awd, IViewLocalizer localizer)
        {
            return string.Format(localizer["KikolesCount"].Value, awd.Count);
        }

        private static string GetValue(this KikoleAward awd, IViewLocalizer localizer)
        {
            return string.Format(localizer["PtsByPlayer"].Value, awd.AveragePoints);
        }
    }
}
