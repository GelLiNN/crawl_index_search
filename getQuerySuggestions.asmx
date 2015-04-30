using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for getQuerySuggestions
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class getQuerySuggestions : System.Web.Services.WebService
    {

        const string azureLocalResourceNameFromServiceDefinition =
        "SomeLocationForCache";
        
        static Trie titles;
        private PerformanceCounter memProcess;

        //DownloadWiki downloads the wiki page titles from blob storage
        [WebMethod]
        public void DownloadWiki()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("wikipedia");

            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        var azureLocalResource = RoleEnvironment.GetLocalResource(
                            azureLocalResourceNameFromServiceDefinition);
                        var filepath = azureLocalResource.RootPath + "downloadedblob.txt";
                        //Blob has been acquired, time to download!
                        using (var fileStream = System.IO.File.OpenWrite(filepath))
                        {
                            blob.DownloadToStream(fileStream);
                        }
                    }
                }
            }
        }

        //Builds a Trie with wiki page data, downloaded from blob storage
        [WebMethod]
        public void BuildTrie()
        {
            memProcess = new PerformanceCounter("Memory", "Available MBytes");

            var azureLocalResource = RoleEnvironment.GetLocalResource(
                azureLocalResourceNameFromServiceDefinition);
            var filepath = azureLocalResource.RootPath + "downloadedblob.txt";

            TextReader tr = new StreamReader(filepath);
            //Read through file and build trie
            string s = tr.ReadLine();
            titles = new Trie();

            int count = 0;
            float memUsage = memProcess.NextValue();

            try
            {
                while (s != null && s.Length != 0 && memUsage > 50)
                {
                    //addTitle with lowercase characters to minimize hassle
                    titles.AddTitle(s.ToLower());
                    s = tr.ReadLine();

                    if (count == 1000)
                    {
                        memUsage = memProcess.NextValue();
                    }
                }
            }
            catch (OutOfMemoryException e)
            {
                //Do nothing when we run out of memory!
            }

            tr.Close();
            memProcess.Close();
        }

        //Search the Trie quickly for JSON Ajax return to index.html
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string SearchTrie(string input)
        {
            List<string> results = titles.SearchForPrefix(input);
            return new JavaScriptSerializer().Serialize(results);
        }
    }
}
