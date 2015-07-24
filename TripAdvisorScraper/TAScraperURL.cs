using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripAdvisorScraper
{
    class TAScraperURL
    {
        protected string urlFull;
        protected string urlListHead;
        protected string urlReviewHead;
        protected string urlTail;

        public TAScraperURL(string URL)
        {
            urlFull = URL;
            string[] splitUrl = URL.Split(new string[] { "-Reviews-" }, StringSplitOptions.None);
            urlListHead = splitUrl[0] + "-Reviews-";
            urlReviewHead = splitUrl[0].Replace("Hotel_Review", "ShowUserReviews");
            urlTail = splitUrl[1];
        }
    }
}
