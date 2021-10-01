﻿using System;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Blep.Backend;

namespace Blep
{
    internal static class BlepApp
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
#warning forced culture
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var argl = args?.ToList() ?? new List<string>();
            Wood.SetNewPathAndErase(Path.Combine(Directory.GetCurrentDirectory(), "BOILOG.txt"));
            Wood.WriteLine($"BOI {BlepOut.VersionNumber} starting {DateTime.Now}");
            if (File.Exists("showConsole.txt") || argl.Contains("-nc") || argl.Contains("--new-console"))
            {
                Backend.BoiCustom.AllocConsole();
                Console.WriteLine("Launching BOI with output to a new console window.");
                Console.WriteLine("Reminder: you can always select text in console and then copy it by pressing enter. It also pauses the app.\n");
            }
            else if (argl.Contains("-ac") || argl.Contains("--attach-console"))
            {
                Backend.BoiCustom.AttachConsole(-1);
                Console.WriteLine("\nLaunching BOI and attempting to attach parent process console.");
            }
            //enter the form
            try
            {
                BlepOut Currblep = new BlepOut();
                Application.Run(Currblep);
            }
            catch (Exception e)
            {
                Wood.IndentLevel = 0;
                Wood.WriteLine("Unhandled exception in the application loop:");
                Wood.WriteLine(e);
            }
            //form is dead
            if (File.Exists("changelog.txt")) File.Delete("changelog.txt");
            if (argl.Contains("-nu") || argl.Contains("--no-update") || File.Exists("neverUpdate.txt")) 
                Wood.WriteLine("Skipping self update.");
            else TrySelfUpdate();
            Wood.Lifetime = 5;
        }

        public const string REPOADDRESS = "https://api.github.com/repos/Rain-World-Modding/BOI/releases/latest";

        //self updates from GH stable releases.
        private static async void TrySelfUpdate()
        {
            var start = DateTime.Now;
            Wood.WriteLine($"Starting self-update: {start}");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;
            try
            {
                var dumpFolder = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "downloadDump"));
                if (!dumpFolder.Exists) dumpFolder.Create(); foreach (var f in dumpFolder.GetFiles()) f.Delete();
                using (var ht = new HttpClient())
                {
                    ht.DefaultRequestHeaders.Clear();
                    ht.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                    //ht.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                    //ht.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Rain-World-Modding_BOI", BlepOut.VersionNumber));
                    var reqTask = ht.GetAsync(REPOADDRESS);
                    var responsejson = Newtonsoft.Json.Linq.JObject.Parse(await reqTask.Result.Content.ReadAsStringAsync());
                    //good enough diff detection
                    if (DateTime.Parse((string)responsejson["published_at"]) < File.GetLastWriteTimeUtc(System.Reflection.Assembly.GetExecutingAssembly().Location)) 
                    { Wood.WriteLine("Update not needed; youngest release is older than me."); return; }
                    ht.DefaultRequestHeaders.Clear();
                    ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                    ht.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                    foreach (var asset in responsejson["assets"] as Newtonsoft.Json.Linq.JArray)
                    {
                        //Backend.Wood.WriteLine(asset.ToString(Newtonsoft.Json.Formatting.Indented));
                        using (var wc = new WebClient()) 
                            wc.DownloadFile((string)asset["browser_download_url"], Path.Combine(dumpFolder.FullName, (string)asset["name"]));
                    }
                    File.WriteAllText("changelog.txt", (string)responsejson["body"]);
                }
                foreach (var f in dumpFolder.GetFiles())
                {
                    if (f.Extension == ".zip")
                    {
                        ZipFile.ExtractToDirectory(f.FullName, dumpFolder.FullName);
                        f.Delete();
                    }
                }
                var xcs = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                xcs.Arguments = $"/c xcopy /Y {dumpFolder.Name} \"{Directory.GetCurrentDirectory()}\"";
                System.Diagnostics.Process.Start(xcs);
                Wood.WriteLine($"Self-update completed. Time elapsed: {DateTime.Now - start}");
            }
            catch (Exception e)
            {
                Wood.WriteLine("Unhandled exception while attempting self-update:");
                Wood.WriteLine(e, 1);
            }
            
        }
    }
}
