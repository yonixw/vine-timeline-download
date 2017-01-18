using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VineDownlaodJSONParser
{
    public class VineJSONDownload
    {

        string mySessionID = "";
        DirectoryInfo mySaveFolder;
        FileInfo myErrorFile;
        public VineJSONDownload(string sessionID, string saveDirectory, string errorFile)
        {
            mySessionID = sessionID;
            mySaveFolder = new DirectoryInfo(saveDirectory);
            myErrorFile = new FileInfo(errorFile);
        }

        string DateTimeString()
        {
            return DateTime.Now.ToLongTimeString();
        }

        void onError(string error)
        {
            string exInfo = DateTimeString() + "\n" + error + "\n";
            File.AppendAllText(myErrorFile.FullName, exInfo);
            Console.WriteLine(exInfo);
        }

        void onError(Exception ex)
        {
            string exInfo = DateTimeString() + "\n" + ex.Message + "\n\n" + ex.StackTrace + "\n";
            File.AppendAllText(myErrorFile.FullName, exInfo + "\n");
            Console.WriteLine(exInfo);
        }

        WebClient mainClient = new WebClient();
        JavaScriptSerializer json = new JavaScriptSerializer();

        public void Start()
        {
            mainClient.Headers.Add("Referer", "https://vine.co/u/1135413680662843392");
            mainClient.Headers.Add("Host", "vine.co");
            mainClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36");
            mainClient.Headers.Add("X-Requested-With", "XMLHttpRequest");
            mainClient.Headers.Add("x-vine-client", "vinewww/2.1");
            mainClient.Headers.Add("x-vine-client-ip", "79.180.173.21");

            long currentPage = 1;
            long totalDownloaded = 0;
            long allVines = 0;
            string currentAnchor = "1398143727133999104"; // find by back of page 2



            Console.WriteLine("* Downloading page " + currentPage);
            mySaveFolder.CreateSubdirectory("page-" + currentPage);
            string pageSavePath = Path.Combine(mySaveFolder.FullName, "page-" + currentPage);
            try
            {
                mainClient.DownloadFile(
                        "https://vine.co/api/timelines/users/1135413680662843392",
                        Path.Combine(pageSavePath, "pageinfo.json.txt")

                    );
            }
            catch (Exception ex)
            {
                onError(ex);
            }

            Console.WriteLine("* Getting json data page " + currentPage);
            string jsonpageString = File.ReadAllText(Path.Combine(pageSavePath, "pageinfo.json.txt"));
            VineJSON jsonObj = json.Deserialize<VineJSON>(jsonpageString);

            
            totalDownloaded += jsonObj.data.size;
            allVines = jsonObj.data.count; // Set once
            Console.WriteLine("* GOT " + jsonObj.data.size + " vines. Total: " + totalDownloaded + "/" + jsonObj.data.count);

            while (totalDownloaded < allVines)
            {
                long recordindex = 0;
                foreach (VineJSON_data_record record in jsonObj.data.records)
                {
                    Console.WriteLine("\t* (" + recordindex + ") Vine id: " + record.postId);

                    // Find max
                    long maxRate = record.videoUrls.Max(url => url.rate);
                    string maxrateVideoPath = record.videoUrls.First(url => url.rate == maxRate).videoUrl;

                    // Download it.
                    Console.WriteLine("\t* download vine file with rate " + maxRate);
                    try
                    {
                        mainClient.DownloadFile(
                                       maxrateVideoPath,
                                       Path.Combine(pageSavePath, recordindex + ".mp4")
                                       );
                    }
                    catch (Exception ex)
                    {
                        onError(ex);
                    }

                    recordindex++;
                }

                currentAnchor = jsonObj.data.anchorStr;
                currentPage++;

                System.Threading.Thread.Sleep(1000); // to avoid ddos radar

                // Download next page


                Console.WriteLine("* Downloading page " + currentPage);
                mySaveFolder.CreateSubdirectory("page-" + currentPage);
                pageSavePath = Path.Combine(mySaveFolder.FullName, "page-" + currentPage);
                try
                {
                    mainClient.DownloadFile(
                                "https://vine.co/api/timelines/users/1135413680662843392?page=" + currentPage + "&anchor=" + currentAnchor + "&size=10",
                                Path.Combine(pageSavePath, "pageinfo.json.txt")

                            );
                }
                catch (Exception ex)
                {
                    onError(ex);
                }

                Console.WriteLine("* Getting json data page " + currentPage);
                jsonpageString = File.ReadAllText(Path.Combine(pageSavePath, "pageinfo.json.txt"));
                jsonObj = json.Deserialize<VineJSON>(jsonpageString);


                totalDownloaded += jsonObj.data.size;
                Console.WriteLine("* GOT " + jsonObj.data.size + " vines. Total: " + totalDownloaded + "/" + jsonObj.data.count);
            }

        }


        public class VineJSON
        {
            public bool success;
            public string code;
            public string error;
            public VineJSON_data data;
            
        }

        public class VineJSON_data
        {
            public long count; // TOTAL vines in timeline
            public string anchorStr; // anchor for next call
            public long size; // how much vines we got
            public VineJSON_data_record[] records;
        }

        public class VineJSON_data_record
        {
            public long postId;
            public VineJSON_data_record_videourl[] videoUrls;
        }


        public class VineJSON_data_record_videourl
        {
            public long rate;
            public string format;
            public string videoUrl;
        }
    }
}
