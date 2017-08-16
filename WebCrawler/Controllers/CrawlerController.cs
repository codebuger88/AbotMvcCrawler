using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Abot.Crawler;
using Abot.Poco;
using AbotX.Core;
using AbotX.Crawler;

namespace WebCrawler.Controllers
{
    public class CrawlerController : Controller
    {
        private List<string> _links = new List<string>();
        // GET: Crawler
        public async Task<ActionResult> Index(string url)
        {
            log4net.Config.XmlConfigurator.Configure();

            Uri uriToCrawl = GetSiteToCrawl(url);

            var crawler = GetManuallyConfiguredWebCrawler();

            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;

            CrawlResult result = await crawler.CrawlAsync(uriToCrawl); //This is synchronous, it will not go to the next line until the crawl has completed
            byte[] byteArray;

            using (var ms = new MemoryStream())
            {
                using (XmlTextWriter writer = new XmlTextWriter(ms, Encoding.UTF8))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("urlset");
                    writer.WriteAttributeString("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9");

                    foreach (var link in _links)
                    {
                        writer.WriteStartElement("url");
                        writer.WriteElementString("loc", link);
                        //writer.WriteElementString("lastmod", string.Format("{0:yyyy-MM-dd}", rdr[0]));
                        writer.WriteElementString("changefreq", "weekly");
                        //writer.WriteElementString("priority", "1.0");
                        writer.WriteEndElement();
                    }

                    writer.WriteEndDocument();
                    writer.Flush();
                }

                byteArray = ms.ToArray();
            }

            return File(byteArray, System.Net.Mime.MediaTypeNames.Application.Octet, "sitemap.xml");
        }

        private static Uri GetSiteToCrawl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new HttpException("Site url to crawl is as a required parameter");

            return new Uri(url);
        }

        private static CrawlerX GetManuallyConfiguredWebCrawler()
        {
            var config = AbotXConfigurationSectionHandler.LoadFromXml().Convert();

            return new CrawlerX(config);
        }

        void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        }

        void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            _links.Add(e.CrawledPage.Uri.AbsoluteUri);
            //Console.WriteLine(e.CrawledPage.Uri.AbsoluteUri);
            //e.CrawledPage.HttpWebResponse.Headers.AllKeys.ForEach(k => Console.WriteLine($"{k}: {e.CrawledPage.HttpWebResponse.Headers[k]}"));

            //Process data
            CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            else
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);

            var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser
            var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
        }

        static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
            CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);
        }

        static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }
    }
}