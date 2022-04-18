﻿using System;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Blep.Backend;

using static Blep.Backend.BoiCustom;

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
            Wood.WriteLine($"BOI {BlepOut.VersionNumber} starting {DateTime.UtcNow}");
            if (File.Exists("showConsole.txt") || argl.Contains("-nc") || argl.Contains("--new-console"))
            {
                //Wood.WriteLine("");
                //BoiCustom.AllocConsole();
                //Console.WriteLine("Launching BOI with output to a new console window.");
                //Console.WriteLine("Reminder: you can always select text in console and then copy it by pressing enter. It also pauses the app.\n");
            }
            else if (argl.Contains("-ac") || argl.Contains("--attach-console"))
            {
                //BoiCustom.AttachConsole(-1);
                //Console.WriteLine("\nLaunching BOI and attempting to attach parent process console.");
            }
            Wood.WriteLine("Console output is disabled for the time being, sorry.");
            //enter the form
            try
            {
                BlepOut Currblep = new BlepOut();
                Application.ThreadException += (sender, e) => {
                    var oind = Wood.IndentLevel;
                    Wood.IndentLevel = 0;
                    Wood.WriteLine("\n<--------------------->\nUNHANDLED EXCEPTION IN APPLICATION LOOP");
                    Wood.WriteLine(e.Exception);
                    Wood.WriteLine("<--------------------->\n");
                    Wood.IndentLevel = oind;
                    if (e.Exception is TypeLoadException) Currblep.Close();
                };
                Application.Run(Currblep);
            }
            catch (Exception e)
            {
                Wood.IndentLevel = 0;
                Wood.WriteLine("Unhandled exception in the application loop:");
                Wood.WriteLine(e);
            }
            //form is dead
            //if (File.Exists("changelog.txt")) File.Delete("changelog.txt");
            if (argl.Contains("-nu") || argl.Contains("--no-update") || File.Exists("neverUpdate.txt")) 
                Wood.WriteLine("Skipping self update.");
            else TrySelfUpdate();
            Wood.Lifetime = 5;
        }

        public const string REPOADDRESS = "https://api.github.com/repos/Rain-World-Modding/BOI/releases/latest";

        //self updates from GH stable releases.
        private static async void TrySelfUpdate()
        {
            var start = DateTime.UtcNow;
            Wood.WriteLine($"Starting self-update: {start}");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = true;
            try
            {
                var dumpFolder = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "downloadDump"));
                //cleaning out old stuff
                if (!dumpFolder.Exists) dumpFolder.Create(); foreach (var f in dumpFolder.GetFiles()) f.Delete();
                using (var ht = new HttpClient())
                {
                    ht.DefaultRequestHeaders.Clear();
                    ht.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                    ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                    var reqTask = ht.GetAsync(REPOADDRESS);
                    var responsejson = Newtonsoft.Json.Linq.JObject.Parse(await reqTask.Result.Content.ReadAsStringAsync());
                    //good enough (i hope????) diff detection
                    //i think this works?
                    if (Version.Parse(System.Text.RegularExpressions.Regex.Replace((string)responsejson["tag_name"], "[^0-9.]", string.Empty)) <= typeof(BlepApp).Assembly.GetName().Version) 
                    { Wood.WriteLine($"Update not needed; youngest release ({responsejson["tag_name"]}) is not younger than me ({typeof(BlepApp).Assembly.GetName().Version})."); return; }
                    Wood.WriteLine($"Youngest release is {responsejson["tag_name"]}, I am {typeof(BlepApp).Assembly.GetName().Version}.");
                    //downloading begins
                    ht.DefaultRequestHeaders.Clear();
                    ht.DefaultRequestHeaders.Add("User-Agent", "Rain-World-Modding/BOI");
                    ht.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
                    foreach (var asset in responsejson["assets"] as Newtonsoft.Json.Linq.JArray)
                    {
                        using var wc = new WebClient();
                        wc.DownloadFile(
                            //System.Text.RegularExpressions.Regex.Replace((string)asset["browser_download_url"], "[^0-9.]", string.Empty), 
                            //i am so confused why did i do the thing above
                            (string)asset["browser_download_url"],
                            Path.Combine(dumpFolder.FullName, (string)asset["name"]));
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
                var xcs = new System.Diagnostics.ProcessStartInfo("cmd.exe")
                {
                    Arguments = $"/c xcopy /Y {dumpFolder.Name} \"{Directory.GetCurrentDirectory()}\""
                };
                System.Diagnostics.Process.Start(xcs);
                Wood.WriteLine($"Self-update completed. Time elapsed: {DateTime.UtcNow - start}");
            }
            catch (Exception e)
            {
                Wood.WriteLine("Unhandled exception while attempting self-update:");
                Wood.WriteLine(e, 1);
            }
        }
    }
}
