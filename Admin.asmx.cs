//Kellan Nealy, #1331068, info 344 PA3
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

        //queues
        private CloudQueue admin = queueClient.GetQueueReference("admin");
        private CloudQueue urls = queueClient.GetQueueReference("urls");

        //table
        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("pages");
        private static CloudTable statstable = tableClient.GetTableReference("stats");
        //other useful state
        private PerformanceCounter cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter mem = new PerformanceCounter("Memory", "Available MBytes");

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StartCrawler()
        {
            admin.CreateIfNotExists();
            urls.CreateIfNotExists();
            table.CreateIfNotExists();
            statstable.CreateIfNotExists();

            CloudQueueMessage adminMessage = new CloudQueueMessage("start");
            admin.AddMessage(adminMessage);
            return "Crawler Started!";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string StopCrawler()
        {
            CloudQueueMessage adminMessage = new CloudQueueMessage("stop");
            admin.AddMessage(adminMessage);
            return "Crawler Stopped!";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ClearIndex()
        {
            table.DeleteIfExists();
            return "index cleared!";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetPageTitle(string url)
        {
            try
            {
                string encoded = HttpUtility.UrlEncode(url);
                TableQuery<WebPage> titleQuery = new TableQuery<WebPage>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, encoded));
                foreach (WebPage page in table.ExecuteQuery(titleQuery))
                {  
                    return page.title;
                }
                return "Page not found!";
            }
            catch
            {
                return "Query failed!";
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string GetStats()
        {

            List<string> statsToSend = new List<string>();
            try
            {
                urls.FetchAttributes();
                string size = "URL Queue Size: " + urls.ApproximateMessageCount;
                statsToSend.Add(size);
                statsToSend.Add("Available Memory: " + mem.NextValue().ToString());
                statsToSend.Add("CPU Usage %: " + cpu.NextValue().ToString());
                TableQuery<Stat> statQuery = new TableQuery<Stat>();
                foreach (Stat elem in statstable.ExecuteQuery(statQuery))
                {
                    statsToSend.Add("Index Size: " + elem.indexsize.ToString());
                    statsToSend.Add("Crawler State: " + elem.state);
                    statsToSend.Add("Last 10 URLS crawled: " + elem.last10);
                    statsToSend.Add(elem.errors);
                }
            }
            catch
            {
                statsToSend.Add("Stats Could Not Be Retrieved!");
            }
            return new JavaScriptSerializer().Serialize(statsToSend);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string ClearAll()
        {
            try
            {
                admin.Clear();
                urls.Clear();
                table.Delete();
                statstable.Delete();
                return "All Cloud Data Cleared!";
            }
            catch
            {
                return "Reset Failed!  Uh-Oh!";
            }
        }
    }
}
/*
 * project greenlight
State of each worker role web crawler (loading, crawling, idle)
Machine counters (CPU Utilization%, RAM available)
#URLs crawled
Last 10 URLs crawled
Size of queue (#urls left in pipeline to be crawled)
Size of index (Table storage with our crawled data)
Any errors and their URLs
*/
