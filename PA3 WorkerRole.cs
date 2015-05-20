//Kellan Nealy, #1331068, info 344 PA3
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Xml;
using System.Text.RegularExpressions;

namespace WebRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            CloudConfigurationManager.GetSetting("StorageConnectionString"));
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

        //queues
        private CloudQueue admin = queueClient.GetQueueReference("admin");
        private CloudQueue urls = queueClient.GetQueueReference("urls");
        //table
        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("pages");
        private static CloudTable statstable = tableClient.GetTableReference("stats");
        //other useful state
        private string crawlerState = "idle";
        private List<string> disallows = new List<string>();
        private HashSet<string> urlList = new HashSet<string>();
        private List<string> last10 = new List<string>(10);
        private List<string> errors = new List<string>();
        private int tablesize = 0;

        //Run Method
        public override void Run()
        {
            while (true)
            {
                string adminMessage = getMessage(admin);

                if (adminMessage != null)
                {
                    if (adminMessage.Equals("start"))
                    {
                        if (last10.Count > 0)
                        {
                            this.crawlerState = "crawling";
                        }
                        else
                        {
                            //set loading state, crawl robots.txt, begin crawl
                            this.crawlerState = "loading";
                        }
                    }
                    else if (adminMessage.Equals("stop"))
                    {
                        //set idle state, stop crawling
                        this.crawlerState = "idle";
                        urls.Clear();
                    }
                }

                updateStats();

                if (this.crawlerState.Equals("loading"))
                {
                    crawlRobotsXML("http://www.cnn.com/robots.txt", urls, false);
                    crawlRobotsXML("http://www.bleacherreport.com/robots.txt", urls, true);
                    this.crawlerState = "crawling";
                }
                else if (this.crawlerState.Equals("crawling"))
                {
                    crawlURLS();
                }
            }
        }

        //updates the stats of this Crawler
        private void updateStats()
        {
            try
            {
                Stat mystats = new Stat(tablesize, crawlerState, string.Join(" | ", last10.ToArray()), string.Join(" | ", errors.ToArray()));
                TableOperation insertOp = TableOperation.InsertOrReplace(mystats);
                statstable.Execute(insertOp);
            }
            catch { }
        }

        //my own overload to getMessage to take advantage of null return
        private string getMessage(CloudQueue queue)
        {
            try
            {
                CloudQueueMessage message = queue.GetMessage();
                string messageString = message.AsString;
                queue.DeleteMessage(message);
                return messageString;
            } catch {
                return null;
            }
        }

        //iterates through robots.txt URL and calls helper to recurse
        private void crawlRobotsXML(string url, CloudQueue urls, Boolean isBleacherReport)
        {
            //go through robots.txt and store disallowed URLS
            string[] lines = getPageContent(url);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] data = lines[i].Split(' ');
                if (data[0].Equals("Sitemap:"))
                {
                    crawlXMLHelper(data[1], isBleacherReport);
                }
                else if (data[0].Equals("Disallow:"))
                {
                    disallows.Add(data[1]);
                }
            }
        }

        //recursively crawls xml sitemaps for urls
        private void crawlXMLHelper(string url, Boolean isBleacherReport)
        {
            //try catch handles broken link errors so crawling can continue
            try
            {
                if (url.EndsWith(".xml"))
                {
                    XmlDocument xd = new XmlDocument();
                    xd.Load(url);
                    XmlNodeList nodelist = xd.DocumentElement.ChildNodes;
                    foreach (XmlNode node in nodelist)
                    {
                        if (!isBleacherReport && node.LastChild.Name.Equals("lastmod"))
                        {
                            string date = node.LastChild.InnerText;
                            if (date.StartsWith("2015-05") || date.StartsWith("2015-04"))
                            {
                                crawlXMLHelper(node.FirstChild.InnerText, isBleacherReport);
                            }
                        }
                        else
                        {
                            crawlXMLHelper(node.FirstChild.InnerText, isBleacherReport);
                        }
                    }
                }
                else
                {
                    urls.AddMessage(new CloudQueueMessage(url));
                }
            }
            catch { }
        }

        //recursively crawls html urls in queue
        private void crawlURLS()
        {
            crawlHelper(getMessage(urls));
        }

        //helper method to recursively crawl urls
        private void crawlHelper(string url)
        {
            if (url != null)
            {
                //try catch handles broken link errors so crawling can continue
                try
                {
                    //have to add this page to index before we crawl!
                    System.Net.WebClient wc = new System.Net.WebClient();
                    string content = wc.DownloadString(url);
                    string title = getTitle(content);
                    addToTable(url, title);

                    Regex regex = new Regex("(a href)=[\"|']?(.*?)[\"|'|>]+", RegexOptions.Singleline | RegexOptions.CultureInvariant);
                    if (regex.IsMatch(content))
                    {
                        foreach (Match match in regex.Matches(content))
                        {
                            string curUrl = match.Value.Substring(match.Value.IndexOf('\"') + 1);
                            if (isValid(curUrl))
                            {
                                urls.AddMessage(new CloudQueueMessage(url));
                            }
                        }
                    }
                    updateStats();
                }
                catch (Exception e) { errors.Add(url + " -- " + e.Message); }
            }

        }

        //checks for valid HTML urls
        private Boolean isValid(string url)
        {
            return !(url.EndsWith(".js") || url.EndsWith(".css") || url.EndsWith(".xml"))
                && (url.Contains("cnn.") || url.Contains("bleacherreport."));  
        }

        //gets the title for an HTML page
        private string getTitle(string pageContent)
        {
            Match m = Regex.Match(pageContent, @"<title>\s*(.+?)\s*</title>");
            if (m.Success)
            {
                return m.Groups[1].Value;
            } else
            {
                return null;
            }
        }

        //checks to see if this URL is allowed
        private Boolean isDisallow(string url)
        {
            foreach (string disallow in disallows)
            {
                if (url.EndsWith(disallow))
                    return true;
            }
            return false;
        }

        //Uses web client to download page content of passed URL as string
        private string[] getPageContent(string url)
        {
            try
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                return wc.DownloadString(url).Split('\n');
            }
            catch
            {
                //do nothing, this is an invalid url!
            }
            return null;
        }

        //Checks for dupes before adding to index
        private void addToTable(string url, string title)
        {
            try
            {            
                if (!urlList.Contains(url))
                {
                    urlList.Add(url);
                    WebPage page = new WebPage(url, title);
                    TableOperation insertOp = TableOperation.Insert(page);
                    table.Execute(insertOp);
                    tablesize++;
                    if (last10.Count == 10)
                    {
                        last10.RemoveAt(0);
                    }
                    last10.Add(url);
                }
            }
            catch { }
        }

        //-----Other Worker Role Methods

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(50);
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }
    }
}
