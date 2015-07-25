/*
 * EXAMPLE URLS *
 *  Hotel URL
 *      http://www.tripadvisor.com.au/Hotel_Review-g255100-d549485-Reviews-The_Langham_Melbourne-Melbourne_Victoria.html
 *  Next Page
 *      http://www.tripadvisor.com.au/Hotel_Review-g255100-d549485-Reviews-or10-The_Langham_Melbourne-Melbourne_Victoria.html
 *  Individual Review URL
 *      http://www.tripadvisor.com.au/ShowUserReviews-g255100-d549485-r291335185-The_Langham_Melbourne-Melbourne_Victoria.html
*/

using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TripAdvisorScraper
{
    partial class TAScraper : TAScraperURL
    {
        private const byte REVIEWS_PER_PAGE = 10;
        private const byte NUMBER_OF_SECTIONS = 5;

        public struct Section
        {
            public const byte Date = 0;
            public const byte Rating = 1;
            public const byte Title = 2;
            public const byte Text = 3;
            public const byte AspectReviews = 4;
        }

        public string Name;
        public string Location;
        public string StarRating;
        public string AverageRating;
        public int NumReviews;

        public List<string[]> Reviews = new List<string[]>();

        public TAScraper(string URL)
            : base(URL)
        {
            processHotel(URL);
        }

        private void processHotel(string url)
        {
            string tempInput = "";
            var web = new HtmlWeb();
            var doc = web.Load(urlFull);

            if (web.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var docNode = doc.DocumentNode;
                string fullHotelName = docNode.CssSelect("#HEADING").Single().InnerText.Trim();
                getHotelLocation(docNode, fullHotelName);

                StarRating = docNode.CssSelect("img[title='Hotel class']").Single().GetAttributeValue("alt", "null").Substring(0, 3);
                AverageRating = docNode.CssSelect("img[property='ratingValue']").Single().GetAttributeValue("content", "null");

                tempInput = docNode.CssSelect("a[property='reviewCount']").Single().GetAttributeValue("content", "null");
                if (int.TryParse(tempInput, out NumReviews) == false) NumReviews = -1;

                if (NumReviews > 0)
                {
                    Reviews = new List<string[]>(NumReviews);
                    byte numPages = (byte)(NumReviews / REVIEWS_PER_PAGE);

                    HtmlNode[] allPagesForHotel = new HtmlNode[numPages];
                    for (int pageCount = 0; pageCount < numPages; pageCount++)
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();

                        string urlPage = urlListHead + "or" + (pageCount + 1) * 10 + "-" + urlTail;
                        doc = web.Load(urlPage);

                        allPagesForHotel[pageCount] = doc.DocumentNode.CssSelect("#REVIEWS").Single();

                        stopwatch.Stop();
                        Console.WriteLine(stopwatch.ElapsedMilliseconds + "ms/PageLoad");
                    }

                    for (int pageCount = 0; pageCount < numPages; pageCount++)
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        processPage(allPagesForHotel[pageCount]);
                        stopwatch.Stop();

                        Console.WriteLine(stopwatch.ElapsedMilliseconds / NumReviews + "ms/Rev");
                        Console.WriteLine((pageCount + 1) * 10);
                    }
                }
                else
                {
                    throw new Exception("No reviews found!");
                }
            }
            else
            {
                throw404(urlFull);
            }
        }

        private void processPage(HtmlNode nodePage)
        {
            var allReviewsOnPage = nodePage.CssSelect(".review").ToArray();

            byte onPage = (byte)allReviewsOnPage.Length;

            Task[] revTasks = new Task[onPage];
            List<string[]> revOut = new List<string[]>(onPage);
            for (int revCount = 0; revCount < onPage; revCount += 1)
            {
                var currentRev = allReviewsOnPage[revCount];
                revTasks[revCount] = Task.Factory.StartNew(() =>
                {
                    revOut.Add(processReview(currentRev));
                });
            }
            Task.WaitAll(revTasks);

            Reviews.AddRange(revOut);
        }

        private string[] processReview(HtmlNode nodeReview)
        {
            string[] result = new string[NUMBER_OF_SECTIONS];

            // DATE
            var dateNode = nodeReview.CssSelect(".ratingDate").Single();
            result[Section.Date] = dateNode.GetAttributeValue("title", dateNode.InnerText.Trim().Substring("Reviewed ".Length));

            // RATING
            result[Section.Rating] = nodeReview.CssSelect(".rating_s > img").Single().GetAttributeValue("alt", "null").Substring(0, 1);

            // TITLE
            result[Section.Title] = nodeReview.CssSelect(".quote > a > span").Single().InnerText.Trim();

            // TEXT
            var moreButton = nodeReview.CssSelect(".partial_entry .moreLink").FirstOrDefault();
            if (moreButton != default(HtmlNode))
            {
                string reviewID = nodeReview.ParentNode.GetAttributeValue("id", "null").Substring("review_".Length);
                if (reviewID != null)
                {
                    string urlReview = urlReviewHead + "-r" + reviewID + "-" + urlTail;
                    var innerWeb = new HtmlWeb();
                    var innerDoc = innerWeb.Load(urlReview);
                    if (innerWeb.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result[Section.Text] = innerDoc.DocumentNode.CssSelect("p[property='reviewBody']").Single().InnerText.Trim();
                    }
                    else
                    {
                        throw404(urlReview);
                    }
                }
            }
            else
            {
                result[Section.Text] = nodeReview.CssSelect(".partial_entry").Single().InnerText.Trim();
            }

            // ASPECTS
            result[Section.AspectReviews] = "??";

            return result;
        }

        private void getHotelLocation(HtmlNode node, string name)
        {
            int comma = name.IndexOf(',');
            if (comma != -1)
            {
                Name = name.Substring(0, comma);
                Location = name.Substring(comma + 1);
                return;
            }

            string[] Capitals = new string[] { "Adelaide", "Brisbane", "Canberra", "Darwin", "Hobart", "Melbourne", "Sydney", "Perth" };
            foreach (string s in Capitals)
            {
                int loc = name.ToUpper().IndexOf(s.ToUpper());
                if (loc != -1)
                {
                    Name = name.Substring(0, loc);
                    Location = name.Substring(loc);
                    return;
                }
            }

            Name = name;
            Location = node.CssSelect("a.breadcrumb_link[onclick*='City'] > span").Single().InnerText.Trim();
            return;
        }
    }
}
