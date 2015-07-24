using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TripAdvisorScraper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TAScraper hotel = new TAScraper(@"http://www.tripadvisor.com.au/Hotel_Review-g255100-d256779-Reviews-Adelphi_Hotel-Melbourne_Victoria.html");

            txtOutput.Text = getHotelString(hotel);
            txtOutput.Text += getReviewStrings(hotel);
        }

        private string getHotelString(TAScraper hotel)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name: " + hotel.Name);
            sb.AppendLine("Location: " + hotel.Location);
            sb.AppendLine("Number of Stars: " + hotel.StarRating);
            sb.AppendLine("Average Rating: " + hotel.AverageRating);
            sb.AppendLine("Number of Reviews: " + hotel.NumReviews.ToString());
            return sb.ToString();
        }

        private string getReviewStrings(TAScraper hotel)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string[] s in hotel.Reviews)
            {
                sb.AppendLine("===");
                sb.AppendLine("Date: " + s[TAScraper.Section.Date]);
                sb.AppendLine("Rating: " + s[TAScraper.Section.Rating]);
                sb.AppendLine("Title: " + s[TAScraper.Section.Title]);
                sb.AppendLine("Text: " + s[TAScraper.Section.Text]);
                sb.AppendLine("AspectReveiws: " + s[TAScraper.Section.AspectReviews]);
            }
            return sb.ToString();
        }
    }
}
