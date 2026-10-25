using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace KikoleSiteUnitTests.Helpers;

/// <summary>
/// Une cle de traduction oubliee ne casse rien : la page affiche le nom brut de la cle. Ce test
/// lit les sources (vues et controleurs) et verifie que chaque cle litterale existe en francais
/// comme en anglais, et que les fichiers de ressources ne contiennent ni doublon ni entree parasite.
/// </summary>
public partial class ResourceKeysTests
{
    private static readonly string[] Languages = ["fr", "en"];

    private static string SiteDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "KikoleSite", "KikoleSite.csproj")))
            directory = directory.Parent;

        directory.Should().NotBeNull("le projet KikoleSite doit se trouver en remontant depuis le dossier des tests");
        return Path.Combine(directory!.FullName, "KikoleSite");
    }

    private static List<string> ReadKeys(string resxPath)
    {
        return XDocument.Load(resxPath).Root!
            .Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToList();
    }

    [GeneratedRegex("""\blocalizer\["([^"]+)"\]""")]
    private static partial Regex ViewKey();

    [GeneratedRegex("""\b_localizer\["([^"]+)"\]""")]
    private static partial Regex ControllerKey();

    [Fact]
    public void EveryKeyUsedByAViewExistsInFrenchAndEnglish()
    {
        var site = SiteDirectory();
        var missing = new List<string>();

        foreach (var view in Directory.GetFiles(Path.Combine(site, "Views"), "*.cshtml", SearchOption.AllDirectories))
        {
            var used = ViewKey().Matches(File.ReadAllText(view)).Select(m => m.Groups[1].Value).Distinct().ToList();
            if (used.Count == 0)
                continue;

            var relative = Path.GetRelativePath(Path.Combine(site, "Views"), view)[..^".cshtml".Length];
            foreach (var language in Languages)
            {
                var resx = Path.Combine(site, "Resources", "Views", $"{relative}.{language}.resx");
                if (!File.Exists(resx))
                {
                    missing.AddRange(used.Select(k => $"{relative}.{language}.resx (fichier absent) : {k}"));
                    continue;
                }

                var keys = ReadKeys(resx);
                missing.AddRange(used.Where(k => !keys.Contains(k)).Select(k => $"{relative}.{language}.resx : {k}"));
            }
        }

        missing.Should().BeEmpty();
    }

    [Fact]
    public void EveryKeyUsedByAControllerExistsInFrenchAndEnglish()
    {
        var site = SiteDirectory();
        var missing = new List<string>();

        foreach (var controller in Directory.GetFiles(Path.Combine(site, "Controllers"), "*Controller.cs"))
        {
            var used = ControllerKey().Matches(File.ReadAllText(controller)).Select(m => m.Groups[1].Value).Distinct().ToList();
            if (used.Count == 0)
                continue;

            var name = Path.GetFileNameWithoutExtension(controller);
            foreach (var language in Languages)
            {
                var resx = Path.Combine(site, "Resources", "Controllers", $"{name}.{language}.resx");
                if (!File.Exists(resx))
                {
                    missing.AddRange(used.Select(k => $"{name}.{language}.resx (fichier absent) : {k}"));
                    continue;
                }

                var keys = ReadKeys(resx);
                missing.AddRange(used.Where(k => !keys.Contains(k)).Select(k => $"{name}.{language}.resx : {k}"));
            }
        }

        missing.Should().BeEmpty();
    }

    [Fact]
    public void ResourceFilesHaveNoDuplicateKeyAndNoSingleLetterKey()
    {
        var problems = new List<string>();

        foreach (var resx in Directory.GetFiles(Path.Combine(SiteDirectory(), "Resources"), "*.resx", SearchOption.AllDirectories))
        {
            var keys = ReadKeys(resx);
            var name = Path.GetFileName(resx);

            problems.AddRange(keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => $"{name} : cle en double {g.Key}"));
            problems.AddRange(keys.Where(k => k.Length < 2).Select(k => $"{name} : cle parasite '{k}'"));
        }

        problems.Should().BeEmpty();
    }
}