﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using System.Security.Cryptography;
using System.IO;

namespace Blep.Backend
{
    /// <summary>
    /// Provides an interface for interacting with related AUDB endpoints.
    /// </summary>
    public static class VoiceOfBees
    {
        /// <summary>
        /// EUV endpoint
        /// </summary>
        public static string ModEntriesEP = "https://beestuff.pythonanywhere.com/audb/api/v2/enduservisible";

        /// <summary>
        /// Downloads 
        /// </summary>
        /// <returns></returns>
        public static bool FetchList()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    ModEntryList.Clear();
                    Wood.WriteLine($"Fetching mod entries from AUDB... {DateTime.Now}");
                    string euv_json = wc.DownloadString(ModEntriesEP);
                    var jo = JArray.Parse(euv_json);
                    foreach (JToken entry in jo)
                    {
                        try
                        {
                            AUDBEntryRelay rel = entry.ToObject<AUDBEntryRelay>();
                            ModEntryList.Add(rel);
                        }
                        catch (JsonReaderException je)
                        {
                            Wood.WriteLine($"Error deserializing AUDB entry :");
                            Wood.WriteLine(je, 1);
                            Wood.WriteLine("Json text:");
                            Wood.WriteLine(entry, 1);
                        }
                    }
                    Wood.WriteLine("Entrylist fetched and parsed:");
                    Wood.Indent();
                    foreach (var entry in ModEntryList) { Wood.WriteLine(entry.name); }
                    Wood.Unindent();
                    return true;
                }
                catch (WebException we) { Wood.WriteLine("Error fetching AUDB entries:");  Wood.WriteLine(we.Response, 1); }
                catch (JsonException jse) { Wood.WriteLine("Error deserializing AUDB entry list"); Wood.WriteLine(jse.Message, 1); }
                return false;
            }
        }
        public static List<AUDBEntryRelay> ModEntryList { get { _el = _el ?? new List<AUDBEntryRelay>(); return _el; } set { _el = value; } }
        private static List<AUDBEntryRelay> _el;

        /// <summary>
        /// Represents download data for a single AUDB file entry
        /// </summary>
        public class AUDBEntryRelay : IEquatable<AUDBEntryRelay>
        {
            public List<AUDBEntryRelay> deps { get { _deps = _deps ?? new List<AUDBEntryRelay>(); return _deps; } set { _deps = value; } }
            private List<AUDBEntryRelay> _deps;
            public KEY key;
            public string name;
            public string author;
            public string description;
            public string download;
            public string sig;
            public List<string> relativePath;
            public string fileExtension = "dll";

            public class KEY
            {
                public string e;
                public string n;
                /// <summary>
                /// Used for keys signed by other keys
                /// </summary>
                public KEYSRCDATA sig;
            }

            public class KEYSRCDATA
            {
                public KEY by;
                public string data;
            }

            public override string ToString()
            {
                return $"{name}";
            }
            /// <summary>
            /// Attempts downloading the entry into a selected directory.
            /// </summary>
            /// <param name="TargetDirectory">Target path.</param>
            /// <returns><c>true</c> if successful, <c>false</c> otherwise. </returns>
            public bool TryDownload(string TargetDirectory)
            {
                using (var dwc = new WebClient())
                {
                    try
                    {
                        var mcts = dwc.DownloadData(download);
                        var sha = new SHA512Managed();
                        var modhash = sha.ComputeHash(mcts);
                        var sigbytes = Convert.FromBase64String(sig);
                        var keyData = new RSAParameters();
                        keyData.Exponent = Convert.FromBase64String(key.e);
                        keyData.Modulus = Convert.FromBase64String(key.n);
                        var rsa = RSA.Create();
                        rsa.ImportParameters(keyData);
                        var def = new RSAPKCS1SignatureDeformatter(rsa);
                        def.SetHashAlgorithm("SHA512");
                        bool directSigCorrect = def.VerifySignature(modhash, sigbytes);
                        bool keySigCorrect = (key.e == PrimeKeyE && key.n == PrimeKeyN);
                        if (key.sig != null)
                        {
                            keyData.Exponent = Convert.FromBase64String(key.sig.by.e);
                            keyData.Modulus = Convert.FromBase64String(key.sig.by.n);
                            rsa.ImportParameters(keyData);
                            def = new RSAPKCS1SignatureDeformatter(rsa);
                            def.SetHashAlgorithm("SHA512");
                            var bee = Encoding.ASCII.GetBytes($"postkey:{key.e}-{key.n}");
                            modhash = sha.ComputeHash(bee);
                            keySigCorrect = def.VerifySignature(modhash, Convert.FromBase64String(key.sig.data));
                        }
                        if (directSigCorrect && keySigCorrect)
                        {
                            Wood.WriteLine($"Mod sig verified: {this.name}, saving");
                            try
                            {
                                var tfi = new DirectoryInfo(TargetDirectory);
                                if (!tfi.Exists) { tfi.Create(); tfi.Refresh(); }
                                File.WriteAllBytes(Path.Combine(TargetDirectory, $"{this.name}.{this.fileExtension}"), mcts);
                                if (deps.Count > 0)
                                {
                                    Wood.WriteLine("");
                                    foreach (var dep in deps)
                                    {
                                        if (dep.TryDownload(TargetDirectory)) { }
                                    }
                                }
                            }
                            catch (IOException ioe) 
                            { Wood.WriteLine($"Can not write the downloaded mod {this.name}:"); Wood.WriteLine(ioe, 1); return false; }
                            return true;
                        }
                        else
                        {
                            Wood.WriteLine($"Mod sig incorrect: {this.name}, download aborted");
                            return false;
                        }
                    }
                    catch (WebException we)
                    {
                        Wood.WriteLine($"Error downloading data from AUDB entry {name}:");
                        Wood.WriteLine(we, 1);
                        
                    }
                    finally
                    {
                        
                    }
                }
                return false;
            }

            public bool Equals(AUDBEntryRelay other)
            {
                return (this.download == other.download);
            }

            public static readonly string PrimeKeyE = "AQAB";
            public static readonly string PrimeKeyN = "yu7XMmICrzuavyZRGWoknFIbJX4N4zh3mFPOyfzmQkil2axVIyWx5ogCdQ3OTdSZ0xpQ3yiZ7zqbguLu+UWZMfLOBKQZOs52A9OyzeYm7iMALmcLWo6OdndcMc1Uc4ZdVtK1CRoPeUVUhdBfk2xwjx+CvZUlQZ26N1MZVV0nq54IOEJzC9qQnVNgeeHxO1lRUTdg5ZyYb7I2BhHfpDWyTvUp6d5m6+HPKoalC4OZSfmIjRAi5UVDXNRWn05zeT+3BJ2GbKttwvoEa6zrkVuFfOOe9eOAWO3thXmq9vJLeF36xCYbUJMkGR2M5kDySfvoC7pzbzyZ204rXYpxxXyWPP5CaaZFP93iprZXlSO3XfIWwws+R1QHB6bv5chKxTZmy/Imo4M3kNLo5B2NR/ZPWbJqjew3ytj0A+2j/RVwV9CIwPlN4P50uwFm+Mr0OF2GZ6vU0s/WM7rE78+8Wwbgcw6rTReKhVezkCCtOdPkBIOYv3qmLK2S71NPN2ulhMHD9oj4t0uidgz8pNGtmygHAm45m2zeJOhs5Q/YDsTv5P7xD19yfVcn5uHpSzRIJwH5/DU1+aiSAIRMpwhF4XTUw73+pBujdghZdbdqe2CL1juw7XCa+XfJNtsUYrg+jPaCEUsbMuNxdFbvS0Jleiu3C8KPNKDQaZ7QQMnEJXeusdU=";
        }
    }
}
