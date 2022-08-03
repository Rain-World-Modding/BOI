using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace Blep.Backend
{
    internal static class Core
    {

        #region statfields
        /// <summary>
        /// Mod selection mask
        /// </summary>
        internal static string MASK;
        /// <summary>
        /// Selection mask mode
        /// </summary>
        internal static Maskmode CMM;

        /// <summary>
        /// Path to the game's root folder.
        /// </summary>
        public static string RootPath = string.Empty;
        /// <summary>
        /// Indicates whether modhash/modmeta files were found during setup.
        /// </summary>
        internal static bool metafiletracker;
        /// <summary>
        /// State tracker for path select dialog and button; janky, definitely subject to change.
        /// </summary>
        internal static bool TSbtnMode = true;

        /// <summary>
        /// List of mods that have been erased during rootout.
        /// </summary>
        internal static readonly List<string> outrmixmods = new();
        /// <summary>
        /// Indicates whether the form is being viewed for the first time in the session.
        /// </summary>
        internal static bool firstshow;
        /// <summary>
        /// Setup tracker for pubstunt.
        /// </summary>
        internal static bool PubstuntFound;
        /// <summary>
        /// Setup tracker for invalid mods.
        /// </summary>
        internal static bool MixmodsFound;

        #endregion

        #region statprops


        /// <summary>
        /// Checks if enabled mods are identical to their counterparts in active folders; if not, brings the needed side up to date.
        /// </summary>
        public static bool IsMyPathCorrect
            => currentStructureState.HasFlag(FolderStructureState.BlepFound | FolderStructureState.GameFound)
            && !currentStructureState.HasFlag(FolderStructureState.RealmFound);


        /// <summary>
        /// Gets <see cref="FolderStructureState"/> for currently selected path.
        /// </summary>
        public static FolderStructureState currentStructureState
        {
            get
            {
                FolderStructureState res = 0;
                if (Directory.Exists(PluginsFolder) && Directory.Exists(PatchersFolder))
                {
                    res |= FolderStructureState.BlepFound;
                    if (File.Exists(Path.Combine(PatchersFolder, "Realm.dll"))) res |= FolderStructureState.RealmFound;
                }
                if (Directory.Exists(Path.Combine(RootPath, "RainWorld_Data"))) res |= FolderStructureState.GameFound;

                return res;
            }
        }

        /// <summary>
        /// Returns BOI folder path.
        /// </summary>
        public static string BOIpath => Directory.GetCurrentDirectory();
        /// <summary>
        /// Returns path to BOI config file.
        /// </summary>
        public static string cfgpath => Path.Combine(BOIpath, "cfg.json");
        /// <summary>
        /// Returns path to tag data file.
        /// </summary>
        public static string tagfilePath => Path.Combine(BOIpath, "MODTAGS.txt");

        public static string ModFolder => Path.Combine(RootPath, "Mods");
        public static string PluginsFolder => Path.Combine(RootPath, "BepInEx", "plugins");
        public static string mmFolder => Path.Combine(RootPath, "BepInEx", "monomod");
        public static string PatchersFolder => Path.Combine(RootPath, "BepInEx", "patchers");

        #endregion

        #region carriedMethods
        /// <summary>
        /// Returns if a <see cref="ModRelay"/> is selected by a given mask.
        /// </summary>
        /// <param name="mask">Mask text.</param>
        /// <param name="mr"><see cref="ModRelay"/> to be checked.</param>
        /// <returns></returns>
        internal static bool SelectedByMask(this ModRelay mr)
        {
            if (string.IsNullOrEmpty(MASK)) return true;
            //string cmm = MaskModeSelect.Text;
            if (CMM == Maskmode.Names || CMM == Maskmode.NamesAndTags) if (mr.ToString().ToLower().Contains(MASK.ToLower())) return true;
            if (CMM == Maskmode.NamesAndTags || CMM == Maskmode.Tags)
            {
                string[] tags = TagManager.GetTagsArray(mr.AssociatedModData.DisplayedName);
                foreach (string tag in tags)
                {
                    if (tag.ToLower().Contains(MASK.ToLower())) return true;
                }
            }
            return false;
        }

        internal static IEnumerable<ModRelay> AllSelectedMods()
        {
            foreach (ModRelay mr in Donkey.cargo) if (mr.SelectedByMask()) yield return mr;
        }
        internal static IEnumerable<int> AllSelectedModIndices()
        {
            return AllSelectedMods().Select(mr => Donkey.cargo.IndexOf(mr));
        }
        #endregion

        #region enums

        [Flags]
        internal enum Maskmode
        {
            Tags,
            Names,
            NamesAndTags
        }

        [Flags]
        public enum FolderStructureState
        {
            BlepFound = 0x00000001,
            GameFound = 0x00000010,
            RealmFound = 0x00000100,
        }
        #endregion


        #region misc

        public static string VersionNumber// => "0.1.7a";
        {
            get
            {
                foreach (var aat in Assembly.GetExecutingAssembly().GetCustomAttributes(false))
                    if (aat is AssemblyInformationalVersionAttribute verA) return verA.InformationalVersion;
                return "vUnk";
            }
        }
        #endregion
    }
}
