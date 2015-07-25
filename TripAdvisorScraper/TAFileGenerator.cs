using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace TripAdvisorScraper
{
    class TAFileGenerator
    {
        string sitemapUrlListPath = @".\sitemapUrlList.txt";
        string reviewUrlListPath = @".\reviewUrlList.txt";

        public TAFileGenerator()
        {
            string sitemapIndexUrl;
            if (File.Exists(sitemapUrlListPath) == false)
            {
                const string robotsFile = @"http://www.tripadvisor.com.au/robots.txt";
                sitemapIndexUrl = getSitemapIndexURL(robotsFile);

                generateSitemapList(sitemapIndexUrl);
            }

            if (File.Exists(reviewUrlListPath) == false)
            {
                generateReviewList();
            }
        }

        /// <summary>
        /// Reads robots.txt to get the url of the Sitemap Index file and returns it as a string.
        /// </summary>
        /// <param name="robotsUrl">Url of the Site's robots.txt file.</param>
        /// <returns>Url of the Sitemap Index file.</returns>
        private string getSitemapIndexURL(string robotsUrl)
        {
            string robotsFile;
            using (var web = new CompressedWebClient())
            {
                robotsFile = web.DownloadString(robotsUrl);
            }

            string sitemapUrl;
            using (StringReader sr = new StringReader(robotsFile))
            {
                string lineIn = sr.ReadLine();
                while (lineIn != null && lineIn.StartsWith("Sitemap: ") == false)
                {
                    lineIn = sr.ReadLine();
                }
                if (lineIn == null) { throw new Exception("Sitelist is malformed!"); }
                else
                {
                    sitemapUrl = lineIn.Substring("Sitemap: ".Length);
                    robotsFile = null;
                }
            }
            return sitemapUrl;
        }

        /// <summary>
        /// Generates a list of XML files containing links to User reviews and writes them to a file.
        /// </summary>
        /// <param name="sitemapIndexUrl">Url of the Sitemap Index file.</param>
        private void generateSitemapList(string sitemapIndexUrl)
        {
            string sitemapBaseFile;
            using (var web = new CompressedWebClient())
            {
                sitemapBaseFile = web.DownloadString(sitemapIndexUrl);
            }

            List<string> sitemapIndex = new List<string>();
            using (XmlReader xr = XmlReader.Create(new StringReader(sitemapBaseFile)))
            {
                while (xr.ReadToFollowing("loc"))
                {
                    string currentLine = xr.ReadElementContentAsString();
                    if (currentLine.StartsWith("http://www.tripadvisor.com.au/sitemap/en_AU/sitemap_sur_en_AU"))
                    {
                        sitemapIndex.Add(currentLine);
                    }
                }
            }

            fileChunkWriter(sitemapUrlListPath, sitemapIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        private void generateReviewList()
        {
            string[] sitemapUrlList = fileReader(sitemapUrlListPath);

            List<string> reviewUrlList = new List<string>();
            foreach (string file in sitemapUrlList)
            {
                using (var web = new CompressedWebClient())
                {
                    string currentFile = web.DownloadString(file);
                    string[] urlGroup = sortReviewUrls(currentFile);
                    reviewUrlList.AddRange(urlGroup);
                }
            }
            sitemapUrlList = null;

            fileChunkWriter(reviewUrlListPath, reviewUrlList);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileContents"></param>
        /// <returns></returns>
        private string[] sortReviewUrls(string fileContents)
        {
            List<string> currentReviewUrls = new List<string>();
            using (XmlReader xr = XmlReader.Create(new StringReader(fileContents)))
            {
                //                                      { "Adelaide", "Brisbane", "Canberra", "Darwin", "Hobart", "Melbourne", "Sydney", "Perth" };
                string[] cityIdentifiers = new string[] { "g255093", "g255068", "g255057", "g255066", "g255097", "g255100", "g255060", "g255103" };
                while (xr.ReadToFollowing("loc"))
                {
                    string currentLine = xr.ReadElementContentAsString();
                    foreach (string city in cityIdentifiers)
                    {
                        if (currentLine.Contains(city))
                        {
                            currentReviewUrls.Add(currentLine);
                        }
                    }
                }
            }
            return currentReviewUrls.ToArray();
        }

        /// <summary>
        /// Writes records from a collection to a file in groups.
        /// </summary>
        /// <typeparam name="T">An Enumerable collection.</typeparam>
        /// <param name="filePath">The path to write the File out to.</param>
        /// <param name="collection">The collection to write to file.</param>
        /// <param name="chunkSize">How many records should be written to file each time.</param>
        private void fileChunkWriter(string filePath, IEnumerable collection, int chunkSize = 10)
        {
            using (StreamWriter sw = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                StringBuilder sb = new StringBuilder();
                byte chunkCounter = 0;
                foreach (string s in collection)
                {
                    sb.AppendLine(s);
                    chunkCounter++;
                    if (chunkCounter == chunkSize)
                    {
                        sw.Write(sb.ToString());
                        sb.Clear();
                        chunkCounter = 0;
                    }
                }
                sw.Write(sb.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string[] fileReader(string filePath)
        {
            List<string> fileOutput = new List<string>();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true))
                {
                    string s = String.Empty;
                    while ( (s = sr.ReadLine()) != null )
                    {
                        fileOutput.Add( s );
                    }
                }
            }

            return fileOutput.ToArray();
        }
    }
}
