using Blep;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;

using static Blep.Backend.Core;

namespace Blep.Backend
{

    internal class BoiConfigManager
    {
        public static JObject confjo;
        public static void ReadConfig()
        {
            try
            {
                confjo = null;
                string tfcont = File.ReadAllText(cfgpath);
                confjo = JObject.Parse(tfcont);
            }
            catch (JsonException joe)
            {
                Wood.WriteLine("ERROR PARSING BOI CONFIG FILE:");
                Wood.WriteLine(joe, 1);
            }
            catch (Exception e)
            {
                Wood.WriteLine("ERROR OPENING BOI CONFIG FILE:");
                Wood.WriteLine(e, 1);
            }
        }
        public static void WriteConfig()
        {
            try
            {
                Wood.WriteLine($"Saving config: {cfgpath}");
                File.WriteAllText(cfgpath, confjo.ToString());
            }
            catch (NullReferenceException)
            {
                Wood.WriteLine("Can not save config: nothing to write");
            }
            catch (Exception e)
            {
                Wood.WriteLine("ERROR SERIALIZING BOI CONFIG FILE:");
                Wood.WriteLine(e, 1);
            }
        }
        public static string TarPath
        {
            get
            {
                if (confjo == null || !confjo.ContainsKey("tarpath")) return string.Empty;
                return (string)confjo["tarpath"];
            }
            set
            {
                if (confjo == null)
                {
                    confjo = new JObject
                    {
                        { "tarpath", value }
                    };
                }
                if (confjo.ContainsKey("tarpath")) confjo["tarpath"] = value;
                else
                {
                    confjo.Add("tarpath", value);
                }
            }
        }
    }
}
