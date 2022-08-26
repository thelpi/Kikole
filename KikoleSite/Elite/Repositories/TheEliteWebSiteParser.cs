using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
        private readonly TheEliteWebsiteConfiguration _configuration;

        public TheEliteWebSiteParser(IOptions<TheEliteWebsiteConfiguration> configuration)
        {
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IReadOnlyCollection<EntryWebDto>> GetMonthPageTimeEntriesAsync(int year, int month)
        {
            var linksValues = new ConcurrentBag<EntryWebDto>();

            string uri = string.Format(_configuration.HistoryPage, year, month);

            string historyContent = await GetPageStringContentAsync(uri)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(historyContent))
            {
                return linksValues;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(historyContent);

            const string timeClass = "time";

            var links = htmlDoc.DocumentNode.SelectNodes("//a");
            foreach (HtmlNode link in links)
            {
                if (link.Attributes.Contains("class")
                    && link.Attributes["class"].Value == timeClass)
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
            var logs = new List<string>();
            string realName = null;
            string controlStyle = null;

            var pageContent = await GetPageStringContentAsync($"/~{urlName.Replace(" ", "+")}", true)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent))
            {
                return null;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContent);

            var headFull = htmlDoc.DocumentNode.SelectNodes("//h1");
            var h1Node = headFull.Count > 1 ? headFull[1] : headFull.First();

            string surname = h1Node.InnerText.Trim().Replace("\r", "").Replace("\n", "").Replace("\t", "");
            if (string.IsNullOrWhiteSpace(surname))
            {
                surname = null;
            }

            string color = h1Node.Attributes["style"].Value.Replace("color:#", "").Trim();
            if (color.Length != 6)
            {
                color = null;
            }

            var indexofControlStyle = pageContent.IndexOf("uses the <strong>");
            if (indexofControlStyle >= 0)
            {
                var controlStyleTxt = pageContent[(indexofControlStyle + "uses the <strong>".Length)..];
                controlStyleTxt = controlStyleTxt.Split(new[] { "</strong>" }, StringSplitOptions.RemoveEmptyEntries).First().Trim().Replace("\r", "").Replace("\n", "").Replace("\t", "");
                if (!string.IsNullOrWhiteSpace(controlStyleTxt))
                {
                    controlStyle = controlStyleTxt;
                }
            }

            var indexofRealname = pageContent.IndexOf("real name is <strong>");
            if (indexofRealname >= 0)
            {
                var realnameTxt = pageContent[(indexofRealname + "real name is <strong>".Length)..];
                realnameTxt = realnameTxt.Split(new[] { "</strong>" }, StringSplitOptions.RemoveEmptyEntries).First().Trim().Replace("\r", "").Replace("\n", "").Replace("\t", "");
                if (!string.IsNullOrWhiteSpace(realnameTxt))
                {
                    realName = realnameTxt;
                }
            }

            var p = new PlayerDto
            {
                Color = color ?? defaultHexPlayer,
                ControlStyle = controlStyle,
                RealName = realName ?? (surname ?? urlName),
                SurName = surname ?? urlName,
                UrlName = urlName
            };

            return p;
        }

        public async Task<IReadOnlyCollection<EntryWebDto>> GetPlayerEntriesAsync(Game game, string playerUrlName)
        {
            var entries = new List<EntryWebDto>();

            var pageContent = await GetPlayerHistoryPageContentAsync(playerUrlName, game)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageContent)
                || pageContent.Contains("<title>Page Not Found - The Elite Rankings</title>"))
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

                var engine = ToEngine(rowDatas[4]);
                if (engine == Engine.UNK)
                {

                }

                var date = ParseDateFromString(rowDatas[0], out bool failToExtractDate, true);
                if (failToExtractDate)
                {

                }

                var time = ExtractTime(rowDatas[3], out bool failToExtractTime);
                if (failToExtractTime || !time.HasValue)
                {
                    continue;
                }

                entries.Add(new EntryWebDto
                {
                    Date = date,
                    Level = level.Value,
                    PlayerUrlName = playerUrlName,
                    Stage = stage.Value,
                    Engine = engine,
                    Time = time.Value
                });
            }

            return entries;
        }

        public async Task<Engine> GetTimeEntryEngineAsync(string url)
        {
            const string engineStringBeginString = "System:</strong>";
            const string engineStringEndString = "</li>";

            string pageContent = await GetPageStringContentAsync(url).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(pageContent))
            {
                int engineStringBeginPos = pageContent.IndexOf(engineStringBeginString);
                if (engineStringBeginPos >= 0)
                {
                    string pageContentAtBeginPos = pageContent[(engineStringBeginPos + engineStringBeginString.Length)..];
                    int engineStringEndPos = pageContentAtBeginPos.Trim().IndexOf(engineStringEndString);
                    if (engineStringEndPos >= 0)
                    {
                        string engineString = pageContentAtBeginPos.Substring(0, engineStringEndPos + 1);

                        return ToEngine(engineString);
                    }
                }
            }

            return Engine.UNK;
        }

        public async Task<IReadOnlyCollection<string>> GetPlayerUrlsAsync(Game game)
        {
            var top50Content = await GetPageStringContentAsync(
                    $"ajax/rankings/{(game == Game.GoldenEye ? "ge" : "pd")}/initial/1661098116")
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(top50Content))
            {
                return null;
            }

            var topFullContent = await GetPageStringContentAsync(
                    $"ajax/rankings/{(game == Game.GoldenEye ? "ge" : "pd")}/post50/1661097156")
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

        private static IEnumerable<string> ExtractPlayerUrlNames(IReadOnlyList<JsonElement> playersAr)
        {
            for (var i = AjaxRankingDto.UrlNamePosition; i < playersAr.Count; i += AjaxRankingDto.ValuesCountByPlayer)
            {
                yield return playersAr[i].GetString();
            }
        }

        private EntryWebDto ExtractTimeLinkDetails(HtmlNode link)
        {
            const char linkSeparator = '-';
            const string playerUrlPrefix = "/~";

            string[] linkParts = CleanString(link.InnerText)
                .Split(linkSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (linkParts.Length < 2)
            {
                return null;
            }

            DateTime? date = ExtractAndCheckDate(link, out bool exit);
            if (exit)
            {
                return null;
            }

            string stageName = linkParts[0].ToLowerInvariant().Replace(" ", string.Empty);
            if (!ModelExtensions.StageFormatedNames.ContainsKey(stageName))
            {
                if (stageName != ModelExtensions.PerfectDarkDuelStageFormatedName)
                {
                    throw new FormatException($"Unable to extract the stage ID.");
                }
                return null;
            }

            string playerUrl = link
                .ParentNode
                .ParentNode
                .ChildNodes[3]
                .ChildNodes
                .First()
                .Attributes["href"]
                .Value
                .Replace(playerUrlPrefix, string.Empty)
                .Replace("+", " ");

            if (string.IsNullOrWhiteSpace(playerUrl))
            {
                return null;
            }

            Level? level = SystemExtensions
                .Enumerate<Level>()
                .Select(l => (Level?)l)
                .FirstOrDefault(l =>
                    l.Value.GetLabel(Game.GoldenEye).Equals(linkParts[1], StringComparison.InvariantCultureIgnoreCase)
                    || l.Value.GetLabel(Game.PerfectDark).Equals(linkParts[1], StringComparison.InvariantCultureIgnoreCase));

            if (!level.HasValue)
            {
                return null;
            }

            long? time = ExtractTime(linkParts[2], out bool failToExtractTime);
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

        private static DateTime? ExtractAndCheckDate(HtmlNode link, out bool exit)
        {
            exit = false;

            string dateString = link.ParentNode.ParentNode.ChildNodes[1].InnerText;

            if (string.IsNullOrWhiteSpace(dateString))
            {
                exit = true;
                return null;
            }

            DateTime? date = ParseDateFromString(dateString, out bool failToExtractDate);
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

        private async Task<string> GetPlayerHistoryPageContentAsync(string playerUrlName, Game game)
        {
            string stringContent = null;
            var attemps = 0;
            const int MaxAttemps = 5;
            do
            {
                try
                {
                    var client = new HttpClient
                    {
                        BaseAddress = new Uri(_configuration.BaseUri)
                    };

                    var response = await client
                        .GetAsync(new Uri($"~{playerUrlName}/{game.GetGameUrlName()}/history", UriKind.Relative))
                        .ConfigureAwait(false);

                    var cookie = response.Headers.GetValues("Set-Cookie").First().Split(';').First().Split('=').ElementAt(1);

                    var queryParams = new List<KeyValuePair<string, string>>
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
                        new KeyValuePair<string, string>("current_pr", "0"),
                        new KeyValuePair<string, string>("sid", cookie),
                    };

                    response = await client
                        .PostAsync(
                            new Uri($"~{playerUrlName}/{game.GetGameUrlName()}/history", UriKind.Relative),
                            new FormUrlEncodedContent(queryParams))
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
            int attemps = 0;
            while (attemps < _configuration.PageAttemps)
            {
                using var webClient = new WebClient();
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

        private static string CleanString(string input)
        {
            return input.Replace("\t", "").Replace("\n", "").Replace("\r", "");
        }

        private static long? ExtractTime(string timeString, out bool failToExtractTime)
        {
            const string untiedString = "(untied!)";
            const string N_A = "N/A";
            const char separator = ':';

            failToExtractTime = false;

            timeString = timeString.Replace(untiedString, string.Empty).Trim();
            if (timeString.IndexOf(separator) >= 0)
            {
                string[] timeComponents = timeString.Split(separator);
                if (timeComponents.Length > 3)
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                int hours = 0;
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
                if (!int.TryParse(timeComponents[0], out int minutes))
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                if (!int.TryParse(timeComponents[1], out int seconds))
                {
                    //logs.Add("Invalid time value");
                    failToExtractTime = true;
                    return null;
                }
                return (hours * 60 * 60) + (minutes * 60) + seconds;
            }
            else if (timeString != N_A)
            {
                //logs.Add("Invalid time value");
                failToExtractTime = true;
                return null;
            }

            return null;
        }

        private static DateTime? ParseDateFromString(string dateString, out bool failToExtractDate, bool partialMonthName = false)
        {
            const char separator = ' ';

            var monthsLabel = new Dictionary<string, int>
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

            var monthsPartialLabel = new Dictionary<string, int>
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

            failToExtractDate = false;

            dateString = dateString?.Trim();

            if (dateString != ModelExtensions.DefaultLabel)
            {
                string[] dateComponents = dateString.Split(separator);
                if (dateComponents.Length != 3)
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (partialMonthName)
                {
                    if (!monthsPartialLabel.ContainsKey(dateComponents[1]))
                    {
                        //logs.Add("No date found !");
                        failToExtractDate = true;
                        return null;
                    }
                }
                else
                {
                    if (!monthsLabel.ContainsKey(dateComponents[1]))
                    {
                        //logs.Add("No date found !");
                        failToExtractDate = true;
                        return null;
                    }
                }
                if (!int.TryParse(dateComponents[0], out int day))
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                if (!int.TryParse(dateComponents[2], out int year))
                {
                    //logs.Add("No date found !");
                    failToExtractDate = true;
                    return null;
                }
                return new DateTime(year, partialMonthName ? monthsPartialLabel[dateComponents[1]] : monthsLabel[dateComponents[1]], day);
            }

            return null;
        }
    }
}
