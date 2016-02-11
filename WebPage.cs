//Kellan Nealy, #1331068, info 344 PA3
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebRole1
{
    class WebPage : TableEntity
    {
        public WebPage(string url, string title)
        {
            string encoded = HttpUtility.UrlEncode(url);

            this.PartitionKey = encoded;
            this.RowKey = "title";
            this.title = title;
        }

        public WebPage() { }

        public string title { get; set; }
    }
}
