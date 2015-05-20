//Kellan Nealy, #1331068, info 344 PA3
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Stat : TableEntity
    {
        public Stat(int indexsize, string state, string last10, string errors)
        {
            this.PartitionKey = "crawler stats";
            this.RowKey = "stats";
            this.indexsize = indexsize;
            this.state = state;
            this.last10 = last10;
            this.errors = errors;
        }

        public Stat() { }

        public int queuesize { get; set; }
        public int indexsize { get; set; }
        public string state { get; set; }
        public string last10 { get; set; }
        public string errors { get; set; }
    }
}
