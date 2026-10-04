using System.Linq;
using System.Text;
using KikoleSite.Configuration;
using KikoleSite.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KikoleSite.Controllers;

/// <summary><c>robots.txt</c> et <c>sitemap.xml</c>, construits à partir de l'adresse canonique configurée.</summary>
public class SeoController : Controller
{
    private readonly string _baseUrl;

    public SeoController(IOptions<SeoOptions> options)
    {
        _baseUrl = options.Value.CanonicalBaseUrl.TrimEnd('/');
    }

    [HttpGet("/robots.txt")]
    public ContentResult Robots()
    {
        var content = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Disallow: /Admin")
            .AppendLine("Disallow: /Account")
            .AppendLine("Disallow: /Home/Contact")
            .AppendLine("Disallow: /Home/ErrorIndex")
            .AppendLine("Disallow: /*?")
            .AppendLine()
            .AppendLine($"Sitemap: {_baseUrl}/sitemap.xml")
            .ToString();

        return Content(content, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    public ContentResult Sitemap()
    {
        var urls = SeoPages.IndexablePaths
            .Select(path => $"  <url><loc>{_baseUrl}{path}</loc></url>");

        var content =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n" +
            string.Join("\n", urls) + "\n" +
            "</urlset>\n";

        return Content(content, "application/xml", Encoding.UTF8);
    }
}
