using Mono.Cecil;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

using static Blep.Backend.Core;

namespace Blep.Backend
{
    //
    //  CODE MODS
    //

    /// <summary>
    /// Unified relay class for <see cref="ModData"/> and its children.
    /// </summary>
    public class ModRelay
    {
        public ModRelay (FileInfo file) : this (file.FullName)
        {

        }
        public ModRelay(string path)
        {
            ModPath = path;
            IsValid = !ModData.AbsolutelyIgnore(ModPath);
            if (IsValid)
            {
                if (Donkey.AintThisPS(path))
                {
                    AssociatedModData = new InvalidModData(path);
                    MyType = EUModType.Invalid;
                    return;
                }
                EUModType mt = GetModType(ModPath);
                MyType = mt;
                switch (mt)
                {
                    case EUModType.mmPatch:
                        AssociatedModData = new mmPatchData(path);
                        break;
                    case EUModType.Partmod:
                        AssociatedModData = new PartModData(path);
                        break;
                    case EUModType.BepPlugin:
                        AssociatedModData = new BepPluginData(path);
                        break;
                    case EUModType.BepPatcher:
                        AssociatedModData = new BepPatcherData(path);
                        break;
                    case EUModType.Invalid:
                        AssociatedModData = new InvalidModData(path);
                        break;
                    case EUModType.Unknown:
                    default:
                        AssociatedModData = new ModData(path);
                        MyType = EUModType.Unknown;
                        break;
                }
            }
        }
        public static EUModType GetModType(ModuleDefinition md)
        {
            var tstate = default(ModTypeFlags);
            foreach (TypeDefinition t in md.Types)
            {
                CheckThisType(t, ref tstate);
            }
            if (tstate.HasFlag(ModTypeFlags.MMpatch))
            {
                if (tstate != ModTypeFlags.MMpatch) return EUModType.Invalid;
                else return EUModType.mmPatch;
            }
            if (tstate.HasFlag(ModTypeFlags.BepPatcher))
            {
                if (tstate != ModTypeFlags.BepPatcher) return EUModType.Invalid;
                else return EUModType.BepPatcher;
            }
            if (tstate.HasFlag(ModTypeFlags.PartMod) ^ tstate.HasFlag(ModTypeFlags.BepPlugin))
            {
                return tstate.HasFlag(ModTypeFlags.PartMod) ? EUModType.Partmod : EUModType.BepPlugin; 
            }
            return EUModType.Unknown;
        }

        public static EUModType GetModType(string path)
        {

            try
            {
                using ModuleDefinition md = ModuleDefinition.ReadModule(path);
                return GetModType(md);
            }
            catch (Exception e)
            {
                Wood.WriteLine($"ERROR CHECKING MODTYPE FOR {path}");
                Wood.WriteLine(e);
                return EUModType.Unknown;
            }
        }

        public static void CheckThisType(TypeDefinition td, ref ModTypeFlags state)
        {
            if (td.BaseType != null && td.BaseType.Name == "PartialityMod") state |= ModTypeFlags.PartMod;
            var contract_M = false;
            var contract_P = false;
            foreach (var method in td.Methods)
            {
                if (method.IsStatic && method.Name == "Patch" && method.HasParameters && method.Parameters[0].Name == "assembly" && method.Parameters[0].ParameterType.Name == "AssemblyDefinition" ) { contract_M = true; break; }
            }
            foreach (var prop in td.Properties)
            {
                if (prop.Name == "TargetDLLs" && prop.GetMethod != null && prop.PropertyType.Name.Contains("IEnumerable")) { contract_P = true; break; }
            }
            if (contract_P & contract_M) state |= ModTypeFlags.BepPatcher;

            if (td.HasCustomAttributes)
            {
                foreach (CustomAttribute catr in td.CustomAttributes)
                {
                    if (catr.AttributeType.Name == "MonoModPatch") state |= ModTypeFlags.MMpatch;
                    if (catr.AttributeType.Namespace == "BepInEx") state |= ModTypeFlags.BepPlugin;
                }
            }
            if (td.HasNestedTypes)
            {
                foreach (TypeDefinition ntd in td.NestedTypes)
                {
                    ModTypeFlags nestate = 0;
                    CheckThisType(ntd, ref nestate);
                    state |= nestate;
                }
            }
        }

        [Flags]
        public enum ModTypeFlags
        {
            PartMod = 1,
            MMpatch = 2,
            BepPlugin = 4,
            BepPatcher = 8,
        }
        public enum EUModType
        {
            mmPatch,
            Partmod,
            Invalid,
            BepPlugin,
            BepPatcher,
            Unknown
        }
        public EUModType MyType;

        public byte[] OrigChecksum
        {
            get
            {
                using FileStream fs = File.OpenRead(ModPath);
                SHA256 sha = new SHA256Managed();
                return sha.ComputeHash(fs);
            }
        }
        public byte[] TarCheckSum
        {
            get
            {
                if (!Enabled)
                {
                    return OrigChecksum;
                }
                using FileStream fs = File.OpenRead(TarPath);
                SHA256 sha = new SHA256Managed();
                return sha.ComputeHash(fs);
            }
        }
        public string TarPath
        {
            get
            {
                return (Path.Combine(AssociatedModData.TarFolder.FullName, AssociatedModData.TarName));
            }
        }

        public string ModPath { get; set; }
        public ModData AssociatedModData { get; set; }
        public bool IsValid { get; set; }

        public bool Enabled
        {
            get { return AssociatedModData.Enabled; }
        }
        public void Enable()
        {
            if (Enabled) return;
            AssociatedModData.OrigLocation.CopyTo(TarPath);
        }
        public void Disable()
        {
            if (!Enabled) return;
            File.Delete(TarPath);
        }
        public override string ToString()
        {
            return AssociatedModData.DisplayedName + " : " + MyType.ToString().ToUpper();
        }
    }

    /// <summary>
    /// Base representationn for RW code mods.
    /// </summary>
    public class ModData
    {
        public ModData(string path)
        {
            OrigLocation = new FileInfo(path);
            //DisplayedName = OrigPath.Name;
        }
        public virtual string TarName
        {
            get { return DisplayedName; }
        }

        public virtual DirectoryInfo TarFolder => Donkey.pluginsTargetPath;
        public FileInfo OrigLocation;

        public virtual bool Enabled => TarFolder.GetFiles(TarName, SearchOption.TopDirectoryOnly).Length > 0;
        public virtual string DisplayedName => OrigLocation.Name;
        public static bool AbsolutelyIgnore(string tpath)
        {
            return (new FileInfo(tpath).Extension != @".dll" || new FileInfo(tpath).Attributes.HasFlag(FileAttributes.ReparsePoint));
        }
        public override string ToString()
        {
            return DisplayedName + " : UNKNOWN";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for Partiality mods.
    /// </summary>
    public class PartModData : ModData
    {
        public PartModData(string path) : base(path)
        {

        }

        public override string ToString()
        {
            return DisplayedName + " : HOOK";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for Monomod patches.
    /// </summary>
    public class mmPatchData : ModData
    {
        public mmPatchData(string path) : base(path)
        {

        }

        public override string TarName => "Assembly-CSharp." + DisplayedName.Replace(".dll", string.Empty) + ".mm.dll";
        public override DirectoryInfo TarFolder => Donkey.mmpTargetPath;
        public static string GiveMeBackMyName(string partname)
        {
            //TODO: make more proof?
            string sl = partname;
            if (sl.StartsWith("Assembly-CSharp.") && sl.EndsWith(".mm.dll"))
            {
                sl = sl.Replace("Assembly-CSharp.", string.Empty);
                sl = sl.Replace(".mm.dll", ".dll");
            }
            return sl;
        }
        public override string ToString()
        {
            return DisplayedName + " : PATCH";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for Bepinex plugins.
    /// </summary>
    public class BepPluginData : ModData
    {
        public BepPluginData(string path) : base(path)
        {

        }

        public override string ToString()
        {
            return DisplayedName + " : PLUGIN";
        }
    }

    /// <summary>
    /// Implementation of <see cref="ModData"/> for invalid mods (mixing mm patches with elsewhat)
    /// </summary>
    public class InvalidModData : mmPatchData
    {
        public InvalidModData(string path) : base(path)
        {

        }

        public override string ToString()
        {
            return DisplayedName + " : INVALID";
        }
    }

    public class BepPatcherData : ModData
    {
        public BepPatcherData(string path) : base(path) { }
        public override DirectoryInfo TarFolder => Donkey.bepPatcherTargetPath;
        public override string ToString() { return DisplayedName + " : BEPPATCHER"; }
    }

    //
    //  REGMODS
    //

    /// <summary>
    /// Represents a CRS regpack state.
    /// </summary>
    public class RegModData
    {
        public RegModData(string pth)
        {
            path = pth;
            hasBeenChanged = false;
            ReadRegInfo();
        }

        
        private JObject jo;
        private string path;
        public bool hasBeenChanged;
        public enum CfgState
        {
            RegInfo,
            PackInfo,
            None
        }
        //Config file type
        public CfgState CurrCfgState
        {
            get
            {
                if (File.Exists(Path.Combine(path, @"packInfo.json")))
                {
                    return CfgState.PackInfo;
                }
                else if (File.Exists(Path.Combine(path, @"regionInfo.json") ))
                {
                    return CfgState.RegInfo;
                }
                else return CfgState.None;
            }
        }
        public string pathToCfg
        {
            get
            {
                return CurrCfgState switch
                {
                    CfgState.PackInfo => Path.Combine(path, "packInfo.json"),
                    CfgState.RegInfo => Path.Combine(path + "regionInfo.json"),
                    CfgState.None => null,
                    _ => null,
                };
            }
        }
        public string regionName
        {
            get
            {
                if (jo == null || !jo.ContainsKey("regionName")) return new DirectoryInfo(path).Name;
                return (string)jo["regionName"];
            }
        }
        public string description
        {
            get
            {
                if (jo == null || !jo.ContainsKey("description")) return "Settings file could not have been loaded; description inaccessible.";
                return (string)jo["description"];
            }
        }
        public bool activated
        {
            get
            {
                if (jo == null || !jo.ContainsKey("activated")) return false;
                return (bool)jo["activated"];
            }

            set 
            {
                if (jo == null || !jo.ContainsKey("activated")) return;
                hasBeenChanged = true;
                jo["activated"] = value;
            }
        }
        public bool structureValid
        {
            get
            {
                return (Directory.Exists(Path.Combine(path, "World") ) || Directory.Exists(Path.Combine(path, "Levels")));
            }
        }
        public int? loadOrder
        {
            get
            {
                if (jo == null || !jo.ContainsKey("loadOrder")) return null;
                return (int)jo["loadOrder"];
            }
            set
            {
                if (jo == null || !jo.ContainsKey("loadOrder")) return;
                hasBeenChanged = true;
                jo["loadOrder"] = value;
            }
        }
        /// <summary>
        /// Reads regionInfo.json / packInfo.json
        /// </summary>
        public void ReadRegInfo()
        {
            if (pathToCfg != null)
            {

                try
                {
                    string jscts = File.ReadAllText(pathToCfg);
                    jo = JObject.Parse(jscts);
                }
                catch (JsonException ioe)
                {
                    Wood.WriteLine($"ERROR READING REGPACK CONFIG JSON FOR: {regionName}");
                    Wood.Indent();
                    Wood.WriteLine(ioe);
                    Wood.Unindent();
                }
                
            }
        }
        /// <summary>
        /// Writes regpack settings json
        /// </summary>
        public void WriteRegInfo()
        {
            if (jo == null)
            {
                Wood.WriteLine($"Region mod {regionName} does not have a config file; cannot apply any changes.");
                return;
            }
            Wood.WriteLine($"Writing changes to regpack config for: {regionName}, contents:");
            Wood.Indent();
            Wood.WriteLine(jo);
            Wood.Unindent();
            
            hasBeenChanged = false;
            File.WriteAllText(pathToCfg, jo.ToString());
        }
        public override string ToString()
        {
            return regionName;
        }
    }

    //
    // EDT CONFIG
    //

    /// <summary>
    /// it's bad
    /// </summary>
    public static class EDTCFGDATA
    {
        public static edtSetup cfg;
        public static string edtConfigPath => Path.Combine(RootPath, "edtSetup.json");
        public static bool edtConfigExists => File.Exists(edtConfigPath);
        public static void loadJo()
        {
            if (!edtConfigExists) return;
            cfg = null;
            try
            {
                string jsf = File.ReadAllText(edtConfigPath);
                cfg = JsonConvert.DeserializeObject<edtSetup>(jsf);
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("Error reading EDT config file:");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
            }
            catch (Exception e)
            {
                Wood.WriteLine("Error parsing EDT config:");
                Wood.Indent();
                Wood.WriteLine(e);
                Wood.Unindent();
            }
            
            
        }
        public static void SaveJo()
        {
            try
            {
                File.WriteAllText(edtConfigPath, JsonConvert.SerializeObject(cfg));
                Wood.WriteLine("Saving EDT config. Contents:");
                Wood.Indent();
                Wood.WriteLine(JsonConvert.SerializeObject(cfg));
                Wood.Unindent();
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("Error writing EDT config file:");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
            }
            catch (ArgumentNullException)
            {
                Wood.WriteLine("JO is null; nothing to write.");
            }
        }
    }
    public class edtSetup
    {
        public string start_map;
        public bool skip_title;
        public int force_selected_character;
        public bool no_rain;
        public bool devtools;
        public int cheat_karma;
        public bool reveal_map;
        public bool force_light;
        public bool bake;
        public bool encrypt;
    }
}
