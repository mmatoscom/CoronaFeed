﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoreLinq;

namespace CoronaFeed.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RssController : ControllerBase
    {
        private List<SyndicationItem> _postings;
        private readonly List<string> _urls;
        public RssController(List<string> urls)
        {
            _urls = urls;
            //_urls = new List<string>();
            //_urls.Add(@"");
            //_urls.Add(@"");
        }

        [ResponseCache(Duration =5)]
        [HttpGet]
        public IActionResult GetRSS()
        {
            _postings = new List<SyndicationItem>();
            var feed = new SyndicationFeed("CoronaFeed", "Feed para centralização de notícias sobre o ", new Uri("https://github.com/luccasmf"), "RSSUrl", DateTime.Now);
            feed.Copyright = new TextSyndicationContent($"{DateTime.Now.Year}");

            var items = new List<SyndicationItem>();

            var tasks = new List<Task>();

            foreach (string feedUrl in _urls)
            {
                tasks.Add(Task.Run(() => ReadRSS(feedUrl)));
            }

            Task t = Task.WhenAll(tasks);

            t.Wait();
            //var postings = GetFeedResults();
            //foreach (var item in _postings)
            //{
            //    //var postUrl = Url.Action("Article", "Blog", new { id = item.BaseUri }, HttpContext.Request.Scheme);
            //    //var title = item.Title;
            //    //var description = item.de;
            //    //items.Add(new SyndicationItem(title, description, new Uri(postUrl), item.BaseUri, item.PostDate));

            //    items.Add(item);
            //}
            feed.Items = _postings;


            feed.Items = feed.Items.DistinctBy(x => x.Title.Text).OrderByDescending(x => x.PublishDate).ToList();

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = true,
                Indent = true
            };
            using (var stream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(stream, settings))
                {
                    var rssFormatter = new Rss20FeedFormatter(feed, false);
                    rssFormatter.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                }
                return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
            }
        }


        private async void ReadRSS(string url)
        {
            try
            {
                using var reader = XmlReader.Create(url);
                var feed = SyndicationFeed.Load(reader);
                _postings.AddRange(feed.Items.Where(x => x.Title.Text.ToLower().Contains("covid") || x.Title.Text.ToLower().Contains("corona")));
            }
            //string[] filtro = new string[] { "Covid", "covid", "Corona", "corona" };
            catch
            {

            }
            
        }
    }
}