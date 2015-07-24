using System;
using System.Web;

namespace TripAdvisorScraper
{
    partial class TAScraper
    {
        private void throw404(string url)
        {
            throw new HttpException(404, string.Format("Webpage did not respond [{0}]", url));
        }
    }
}
