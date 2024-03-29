﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using KikoleSite.Elite.Configurations;
using KikoleSite.Elite.Dtos;
using KikoleSite.Elite.Enums;
using KikoleSite.Elite.Extensions;
using Microsoft.Extensions.Options;

namespace KikoleSite.Elite.Repositories
{
    public class TheEliteWebSiteParser : ITheEliteWebSiteParser
    {
        private const char DatePartsSeparator = ' ';
        private const string UntiedTimeLabel = "(untied!)";
        private const string TimeNa = "N/A";
        private const char TimePartsSeparator = ':';
        private const char LinkSeparator = '-';
        private const string PlayerUrlPrefix = "/~";
        private const string EngineStringBeginString = "System:</strong>";
        private const string EngineStringEndString = "</li>";
        private const string TimeClass = "time";
        private const string ControlStylePattern = "uses the ";
        private const string RealNamePattern = "real name is ";
        private const string CountryPattern = "country: ";
        private const string AgePattern = "is currently ";
        private const string ColorPattern = "color:#";
        private const string PageNotFoundTitle = "<title>Page Not Found - The Elite Rankings</title>";
        private const string Top50PlayersUrl = "ajax/rankings/{0}/initial/1661098116";
        private const string AllPlayersUrl = "ajax/rankings/{0}/post50/1661097156";
        private const string PlayerHistoryUrl = "~{0}/{1}/history";
        private const string CookieHistoryName = "Set-Cookie";

        private static readonly IReadOnlyCollection<KeyValuePair<string, string>> StaticQueryParams =
            new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("date_start", ""),
                new KeyValuePair<string, string>("stage_id", ""),
                new KeyValuePair<string, string>("date_end", ""),
                new KeyValuePair<string, string>("difficulty-0", "0"),
                new KeyValuePair<string, string>("difficulty-1", "1"),
                new KeyValuePair<string, string>("difficulty-2", "2"),
                new KeyValuePair<string, string>("system-0", "NTSC"),
                new KeyValuePair<string, string>("system-1", "NTSC-J"),
                new KeyValuePair<string, string>("system-2", "PAL"),
                new KeyValuePair<string, string>("system-3", "Unknown"),
                new KeyValuePair<string, string>("current_pr", "0")
            };

        private static readonly IReadOnlyDictionary<string, int> MonthLabels =
            new Dictionary<string, int>
            {
                { "January", 1 },
                { "February", 2 },
                { "March", 3 },
                { "April", 4 },
                { "May", 5 },
                { "June", 6 },
                { "July", 7 },
                { "August", 8 },
                { "September", 9 },
                { "October", 10 },
                { "November", 11 },
                { "December", 12 }
            };

        private static readonly IReadOnlyDictionary<string, int> MonthShortLabels =
            new Dictionary<string, int>
            {
                { "Jan", 1 },
                { "Feb", 2 },
                { "Mar", 3 },
                { "Apr", 4 },
                { "May", 5 },
                { "Jun", 6 },
                { "Jul", 7 },
                { "Aug", 8 },
                { "Sep", 9 },
                { "Oct", 10 },
                { "Nov", 11 },
                { "Dec", 12 }
            };

        private readonly TheEliteWebsiteConfiguration _configuration;
        private readonly IClock _clock;

        public TheEliteWebSiteParser(
            IOptions<TheEliteWebsiteConfiguration> configuration,
            IClock clock)
        {
            _configuration = configuration.Value;
            _clock = clock;
        }

        public async Task<IReadOnlyCollection<EntryWebDto>> GetMonthPageTimeEntriesAsync(int year, int month)
        {
            var linksValues = new ConcurrentBag<EntryWebDto>();

            var uri = string.Format(_configuration.HistoryPage, year, month);

            var historyContent = await GetPageStringContentAsync(uri)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(historyContent))
            {
                return linksValues;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(historyContent);

            var links = htmlDoc.DocumentNode.SelectNodes("//a");
            foreach (var link in links)
            {
                if (link.Attributes.Contains("class")
                    && link.Attributes["class"].Value == TimeClass)
                {
                    var linkValues = ExtractTimeLinkDetails(link);
                    if (linkValues != null)
                    {
                        linksValues.Add(linkValues);
                    }
                }
            }

            return linksValues;
        }

        public async Task<PlayerDto> GetPlayerInformationAsync(string urlName, string defaultHexPlayer)
        {
            var pageContent = await GetPageStringContentAsync($"{PlayerUrlPrefix}{HttpUtility.UrlEncode(urlName)}", true)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent))
            {
                return null;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContent);

            var headFull = htmlDoc.DocumentNode.SelectNodes("//h1");
            var h1Node = headFull.Count > 1 ? headFull[1] : headFull.First();

            var surname = h1Node.InnerText.Sanitize();
            if (string.IsNullOrWhiteSpace(surname))
            {
                surname = null;
            }

            var color = h1Node.Attributes["style"].Value.Replace(ColorPattern, "").Trim();
            if (color.Length != 6)
            {
                color = null;
            }

            var controlStyle = ExtractPlayerProperty(pageContent, ControlStylePattern);
            var realName = ExtractPlayerProperty(pageContent, RealNamePattern);
            var country = ExtractPlayerProperty(pageContent, CountryPattern);
            var ageString = ExtractPlayerProperty(pageContent, AgePattern);

            int? maxYob = null, minYob = null;
            if (!string.IsNullOrWhiteSpace(ageString) && int.TryParse(ageString, out var age))
            {
                maxYob = _clock.Today.Year - age;
                minYob = maxYob - 1;
            }

            return new PlayerDto
            {
                Color = color ?? defaultHexPlayer,
                ControlStyle = controlStyle,
                RealName = realName ?? (surname ?? urlName),
                SurName = surname ?? urlName,
                UrlName = urlName,
                Country = country,
                MaxYearOfBirth = maxYob,
                MinYearOfBirth = minYob
            };
        }

        public async Task<IReadOnlyCollection<EntryWebDto>> GetPlayerEntriesAsync(Game game, string playerUrlName)
        {
            var entries = new List<EntryWebDto>();

            var pageContent = await GetPlayerHistoryPageContentAsync(playerUrlName, game)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent) || pageContent.Contains(PageNotFoundTitle))
            {
                // Do not return an empty list here.
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            foreach (var row in doc.DocumentNode.SelectNodes("//tr[td]") ?? Enumerable.Empty<HtmlNode>())
            {
                var rowDatas = row.SelectNodes("td").Select(td => td.InnerText).ToArray();

                var stage = ModelExtensions.GetStageFromLabel(rowDatas[1]);
                if (stage == null)
                {
                    continue;
                }

                var level = ModelExtensions.GetLevelFromLabel(rowDatas[2]);
                if (!level.HasValue)
                {
                    continue;
                }

                var time = ExtractTime(rowDatas[3], out var failToExtractTime);
                if (failToExtractTime || !time.HasValue)
                {
                    continue;
                }

                entries.Add(new EntryWebDto
                {
                    Date = ParseDateFromString(rowDatas[0], out _, true),
                    Level = level.Value,
                    PlayerUrlName = playerUrlName,
                    Stage = stage.Value,
                    Engine = ToEngine(rowDatas[4]),
                    Time = time.Value
                });
            }

            return entries;
        }

        public async Task<Engine> GetTimeEntryEngineAsync(string url)
        {
            var pageContent = await GetPageStringContentAsync(url).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(pageContent))
            {
                var engineStringBeginPos = pageContent.IndexOf(EngineStringBeginString);
                if (engineStringBeginPos >= 0)
                {
                    var pageContentAtBeginPos = pageContent[(engineStringBeginPos + EngineStringBeginString.Length)..];
                    var engineStringEndPos = pageContentAtBeginPos.Trim().IndexOf(EngineStringEndString);
                    if (engineStringEndPos >= 0)
                    {
                        var engineString = pageContentAtBeginPos.Substring(0, engineStringEndPos + 1);

                        return ToEngine(engineString);
                    }
                }
            }

            return Engine.UNK;
        }

        public async Task<IReadOnlyCollection<string>> GetPlayerUrlsAsync(Game game)
        {
            var top50Content = await GetPageStringContentAsync(
                    string.Format(Top50PlayersUrl, game == Game.GoldenEye ? "ge" : "pd"))
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(top50Content))
            {
                return null;
            }

            var topFullContent = await GetPageStringContentAsync(
                    string.Format(AllPlayersUrl, game == Game.GoldenEye ? "ge" : "pd"))
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(topFullContent))
            {
                return null;
            }

            var top50datas = JsonSerializer.Deserialize<AjaxRankingDto>(top50Content);
            var otherDatas = JsonSerializer.Deserialize<AjaxRankingDto>(topFullContent);

            var urls = new List<string>(50 + otherDatas.TValue.Count / AjaxRankingDto.ValuesCountByPlayer);

            urls.AddRange(ExtractPlayerUrlNames(top50datas.TValue));
            urls.AddRange(ExtractPlayerUrlNames(otherDatas.TValue));

            return urls;
        }

        private async Task<string> GetPlayerHistoryPageContentAsync(string playerUrlName, Game game)
        {
            string stringContent = null;
            var attemps = 0;
            const int MaxAttemps = 5;
            do
            {
                try
                {
                    var urlEncodedPlayerUrl = HttpUtility.UrlEncode(playerUrlName);

                    var httpClientHandler = new HttpClientHandler
                    {
                        Proxy = GetProxy()
                    };

                    var client = new HttpClient(httpClientHandler)
                    {
                        BaseAddress = new Uri(_configuration.BaseUri)
                    };

                    var historyUrl = string.Format(PlayerHistoryUrl, urlEncodedPlayerUrl, game.GetGameUrlName());

                    var response = await client
                        .GetAsync(new Uri(historyUrl, UriKind.Relative))
                        .ConfigureAwait(false);

                    var cookie = response.Headers.GetValues(CookieHistoryName).First().Split(';').First().Split('=').ElementAt(1);

                    response = await client
                        .PostAsync(
                            new Uri(historyUrl, UriKind.Relative),
                            new FormUrlEncodedContent(
                                StaticQueryParams.Concat(
                                    new KeyValuePair<string, string>("sid", cookie).Yield())))
                        .ConfigureAwait(false);

                    stringContent = await response.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);
                    attemps = MaxAttemps;
                }
                catch
                {
                    if (attemps < MaxAttemps)
                        attemps++;
                    else
                        throw;
                }
            }
            while (attemps < MaxAttemps);

            return stringContent;
        }

        private async Task<string> GetPageStringContentAsync(string partUri, bool ignoreNotFound = false)
        {
            var uri = new Uri(string.Concat(_configuration.BaseUri, partUri));

            string data = null;
            var attemps = 0;
            while (attemps < _configuration.PageAttemps)
            {
                using var webClient = new WebClient();
                var proxy = GetProxy();
                if (proxy != null)
                    webClient.Proxy = proxy;
                try
                {
                    data = await webClient
                        .DownloadStringTaskAsync(uri)
                        .ConfigureAwait(false);
                    attemps = _configuration.PageAttemps;
                }
                catch (Exception ex)
                {
                    if (ignoreNotFound && ex.IsWebNotFound())
                    {
                        return data;
                    }
                    attemps++;
                    if (attemps == _configuration.PageAttemps)
                    {
                        throw;
                    }
                }
            }

            return data;
        }

        private static long? ExtractTime(string timeString, out bool failToExtractTime)
        {
            failToExtractTime = false;

            timeString = timeString.Replace(UntiedTimeLabel, string.Empty).Trim();
            if (timeString.IndexOf(TimePartsSeparator) >= 0)
            {
                var timeComponents = timeString.Split(TimePartsSeparator);
                if (timeComponents.Length > 3)
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                var hours = 0;
                if (timeComponents.Length > 2)
                {
                    if (!int.TryParse(timeComponents[0], out hours))
                    {
                        //logs.Add("Invalid time value");
                        failToExtractTime = true;
                        return null;
                    }
                    timeComponents[0] = timeComponents[1];
                    timeComponents[1] = timeComponents[2];
                }
                if (!int.TryParse(timeComponents[0], out var minutes))
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                if (!int.TryParse(timeComponents[1], out var seconds))
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                return (hours * 60 * 60) + (minutes * 60) + seconds;
            }
            else if (timeString != TimeNa)
            {
                //logs.Add("Invalid time value");
                failToExtractTime = true;
                return null;
            }

            return null;
        }

        private static DateTime? ParseDateFromString(string dateString, out bool failToExtractDate, bool partialMonthName = false)
        {
            failToExtractDate = false;

            dateString = dateString?.Trim();

            if (dateString != ModelExtensions.DefaultLabel)
            {
                var dateComponents = dateString.Split(DatePartsSeparator);
                if (dateComponents.Length != 3)
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (partialMonthName)
                {
                    if (!MonthShortLabels.ContainsKey(dateComponents[1]))
                    {
                        //logs.Add("No date found !");
                        failToExtractDate = true;
                        return null;
                    }
                }
                else
                {
                    if (!MonthLabels.ContainsKey(dateComponents[1]))
                    {
                        //logs.Add("No date found !");
                        failToExtractDate = true;
                        return null;
                    }
                }
                if (!int.TryParse(dateComponents[0], out var day))
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (!int.TryParse(dateComponents[2], out var year))
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                return new DateTime(year, partialMonthName ? MonthShortLabels[dateComponents[1]] : MonthLabels[dateComponents[1]], day);
            }

            return null;
        }

        private static IEnumerable<string> ExtractPlayerUrlNames(IReadOnlyList<JsonElement> playersAr)
        {
            for (var i = AjaxRankingDto.UrlNamePosition; i < playersAr.Count; i += AjaxRankingDto.ValuesCountByPlayer)
            {
                yield return playersAr[i].GetString();
            }
        }

        private static DateTime? ExtractAndCheckDate(HtmlNode link, out bool exit)
        {
            exit = false;

            var dateString = link.ParentNode.ParentNode.ChildNodes[1].InnerText;

            if (string.IsNullOrWhiteSpace(dateString))
            {
                exit = true;
                return null;
            }

            var date = ParseDateFromString(dateString, out var failToExtractDate);
            if (failToExtractDate)
            {
                exit = true;
                return null;
            }

            return date;
        }

        private static Engine ToEngine(string engineString)
        {
            return Enum.TryParse<Engine>(engineString.Trim().Replace("-", "_"), true, out var engine)
                ? engine
                : Engine.UNK;
        }

        private static EntryWebDto ExtractTimeLinkDetails(HtmlNode link)
        {
            var linkParts = link.InnerText
                .Sanitize()
                .Split(LinkSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (linkParts.Length < 2)
            {
                return null;
            }

            var date = ExtractAndCheckDate(link, out var exit);
            if (exit)
            {
                return null;
            }

            var stageName = linkParts[0].ToLowerInvariant().Replace(" ", string.Empty);
            if (!ModelExtensions.StageFormatedNames.ContainsKey(stageName))
            {
                if (stageName != ModelExtensions.PerfectDarkDuelStageFormatedName)
                {
                    throw new FormatException($"Unable to extract the stage ID.");
                }
                return null;
            }

            var playerUrl = HttpUtility.UrlDecode(link
                .ParentNode
                .ParentNode
                .ChildNodes[3]
                .ChildNodes
                .First()
                .Attributes["href"]
                .Value
                .Replace(PlayerUrlPrefix, string.Empty));

            if (string.IsNullOrWhiteSpace(playerUrl))
            {
                return null;
            }

            var level = SystemExtensions
                .Enumerate<Level>()
                .Select(l => (Level?)l)
                .FirstOrDefault(l =>
                    l.Value.GetLabel(Game.GoldenEye).Equals(linkParts[1], StringComparison.InvariantCultureIgnoreCase)
                    || l.Value.GetLabel(Game.PerfectDark).Equals(linkParts[1], StringComparison.InvariantCultureIgnoreCase));

            if (!level.HasValue)
            {
                return null;
            }

            var time = ExtractTime(linkParts[2], out var failToExtractTime);
            if (failToExtractTime || !time.HasValue)
            {
                return null;
            }

            return new EntryWebDto
            {
                Date = date,
                Level = level.Value,
                PlayerUrlName = playerUrl,
                Stage = ModelExtensions.StageFormatedNames[stageName],
                Engine = Engine.UNK,
                EngineUrl = link.Attributes["href"].Value,
                Time = time.Value
            };
        }

        private WebProxy GetProxy()
        {
            if (string.IsNullOrWhiteSpace(_configuration.ProxyServerUrl))
                return null;

            return new WebProxy(_configuration.ProxyServerUrl);
        }

        private static string ExtractPlayerProperty(string pageContent, string prefix)
        {
            string propertyValue = null;
            var indexOfPropertyValue = pageContent.IndexOf($"{prefix}<strong>");
            if (indexOfPropertyValue >= 0)
            {
                var realnameTxt = pageContent[(indexOfPropertyValue + $"{prefix}<strong>".Length)..];
                realnameTxt = realnameTxt.Split(new[] { "</strong>" }, StringSplitOptions.RemoveEmptyEntries).First().Sanitize();
                if (!string.IsNullOrWhiteSpace(realnameTxt))
                {
                    propertyValue = realnameTxt;
                }
            }

            return propertyValue;
        }
    }
}
