﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using Blep.Backend;

namespace Blep
{
    public partial class BlepOut : Form
    {
        public BlepOut()
        {
            
            InitializeComponent();
            firstshow = true;
            MaskModeSelect.Items.AddRange(new object[] {Maskmode.Names, Maskmode.Tags, Maskmode.NamesAndTags});
            MaskModeSelect.SelectedItem = Maskmode.NamesAndTags;
            Wood.SetNewPathAndErase(Path.Combine(Directory.GetCurrentDirectory(), "BOILOG.txt"));
            Wood.WriteLine("BOI starting " + DateTime.Now);
            targetFiles = new List<ModRelay>();
            pluginBlacklist = new List<string>();
            patchBlacklist = new List<string>();
            outrmixmods = new List<string>();
            TagManager.ReadTagsFromFile(tagfilePath);
            BoiConfigManager.ReadConfig();
            UpdateTargetPath(BoiConfigManager.TarPath);
            firstshow = false;
            if (File.Exists(Path.Combine(RootPath, "BepInEx", "LogOutput.log")))
            {
                string[] lans = File.ReadAllLines(Path.Combine(RootPath, "BepInEx", "LogOutput.log"));
                for (int cuwo = 0; cuwo < lans.Length; cuwo++)
                {
                    string scrpyr = lans[cuwo];
                    if (scrpyr.Contains("Here be dragons!"))
                    {

                        Wood.WriteLine("Dragon thoughts found. Saying hi.");
                        goto iolaa;
                    }
                }
            iolaa:
                {
                    Wood.WriteLine("...");
                    Wood.WriteLine("To you and your parent, greetings. May your work persist for as long as we do.");
                    Wood.WriteLine("Wish you all well. Bzz!");
                }
            }
            if (VoiceOfBees.ModEntryList.Count > 0) { VoiceOfBees.ModEntryList[0].TryDownload(ModFolder); }
        }
        

        public void UpdateTargetPath(string path)
        {
            btnLaunch.Enabled = false;
            Modlist.Enabled = false;
            RootPath = path;
            BoiConfigManager.TarPath = path;
            targetFiles.Clear();
            pluginBlacklist.Clear();
            patchBlacklist.Clear();
            if (IsMyPathCorrect) Setup();
            StatusUpdate();
        }
        private void Setup()
        {
            Wood.WriteLine("Path valid, starting setup " + DateTime.Now);
            Wood.Indent();
            PubstuntFound = false;
            MixmodsFound = false;
            metafiletracker = false;
            ReadyForRefresh = false;
            Modlist.Items.Clear();
            outrmixmods.Clear();
            targetFiles.Clear();

            Rootout();
            PrepareModsFolder();
            ResolveBlacklists();
            RetrieveAllDlls();
            CompileModList();
            BringUpToDate();
            Modlist.Enabled = true;
            btnLaunch.Enabled = true;
            TargetSelect.SelectedPath = RootPath;
            if (PubstuntFound && firstshow)
            {
                PubstuntInfoPopup popup;
                popup = new PubstuntInfoPopup();
                AddOwnedForm(popup);
                popup.Show();
            }
            if (MixmodsFound)
            {
                MixmodsPopup mixmodsPopup = new Blep.MixmodsPopup(outrmixmods);
                AddOwnedForm(mixmodsPopup);
                mixmodsPopup.Show();
            }
            ReadyForRefresh = true;
            buttonClearMeta.Visible = metafiletracker;
            Wood.Unindent();
        }
        private void ResolveBlacklists()
        {
            RetrieveHkBlacklist();
            RetrievePtBlacklist();
        }

        //deletes Pubstunt and mixmods from where they shouldn't be
        private void Rootout()
        {
            string[] patchfoldercontents = Directory.GetFiles(PatchesFolder);
            string[] pluginfoldercontents = Directory.GetFiles(PluginsFolder);
            foreach (string s in patchfoldercontents)
            {
                var fi = new FileInfo(s);
                if (fi.Extension == ".dll" && !fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    if (AintThisPS(s))
                    {
                        PubstuntFound = true;
                        Wood.WriteLine("Located PublicityStunt in active Plugins folder, removing.");
                        File.Delete(s);
                    }
                    else
                    {
                        ModRelay.ModType mt = ModRelay.GetModType(s);
                        if (!(mt == ModRelay.ModType.Patch || mt == ModRelay.ModType.Invalid) && !patchBlacklist.Contains(s) && !fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            Wood.WriteLine("Found a misplaced mod in Patches folder: " + fi.Name + "; Type: " + mt.ToString());
                            if (File.Exists(ModFolder + PtModData.GiveMeBackMyName(fi.Name)))
                            {
                                File.Delete(s);
                            }
                            else
                            {
                                File.Move(s, ModFolder + PtModData.GiveMeBackMyName(fi.Name));
                            }
                        }
                    }
                }
            }
            foreach (string s in pluginfoldercontents)
            {
                var fi = new FileInfo(s);
                if (fi.Extension == ".dll" && !pluginBlacklist.Contains(s) && !fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {

                    if (AintThisPS(s))
                    {
                        File.Delete(s);
                        Wood.WriteLine("Located PublicityStunt in active Plugins folder, removing.");
                        PubstuntFound = true;
                    }
                    else
                    {
                        ModRelay.ModType mt = ModRelay.GetModType(s);

                        if (mt == ModRelay.ModType.Patch || mt == ModRelay.ModType.Invalid)
                        {
                            Wood.WriteLine("Found a misplaced mod in Plugins folder: " + fi.Name + "; Type: " + mt.ToString());
                            if (File.Exists(ModFolder + PtModData.GiveMeBackMyName(fi.Name)))
                            {
                                File.Delete(s);
                                Wood.WriteLine("Duplicate exists in Mods folder, deleting.");
                            }
                            else
                            {
                                Wood.WriteLine("Moving to Mods folder.");
                                File.Move(s, ModFolder + PtModData.GiveMeBackMyName(fi.Name));
                            }
                        }
                    }
                }
            }
        }
        private void PrepareModsFolder()
        {
            metafiletracker = false;
            if (!Directory.Exists(ModFolder))
            {
                Wood.WriteLine("Mods folder not found, creating.");
                Directory.CreateDirectory(ModFolder);
            }
            string[] modfldcontents = Directory.GetFiles(ModFolder);
            foreach (string path in modfldcontents)
            {
                var fi = new FileInfo(path);
                if (fi.Extension == ".modHash" || fi.Extension == ".modMeta")
                {
                    metafiletracker = true;
                }
            }
            Wood.WriteLineIf(metafiletracker, "Found modhash/modmeta files in mods folder.");
        }
        //
        //blacklist stuff
        //
        private void RetrieveHkBlacklist()
        {
            if (File.Exists(hkblacklistpath))
            {
                string[] sl = File.ReadAllLines(hkblacklistpath);
                foreach (string s in sl)
                {
                    pluginBlacklist.Add(s);
                }
            }
            if (pluginBlacklist.Count == 0)
            {
                CreateHkBlacklist();
                pluginBlacklist.Add("LogFix.dll");
            }
            Wood.WriteLine("Plugin folder blacklist contents: ");
            Wood.Indent();
            foreach (string s in pluginBlacklist)
            {
                Wood.WriteLine(new FileInfo(s).Name);
            }
            Wood.Unindent();
        }
        private void CreateHkBlacklist()
        {
            Wood.WriteLine("Creating new plugin blacklist file.");
            StreamWriter sw = File.CreateText(hkblacklistpath);
            sw.WriteLine("LogFix.dll");
            sw.Close();
            sw.Dispose();

        }
        private void RetrievePtBlacklist()
        {
            if (File.Exists(ptblacklistpath))
            {
                string[] pblcts = File.ReadAllLines(ptblacklistpath);
                foreach (string s in pblcts)
                {
                    patchBlacklist.Add(s);
                }
            }
            if (patchBlacklist.Count == 0)
            {
                CreatePtBlacklist();
                patchBlacklist.Add("Assembly-CSharp.PatchNothing.mm.dll");
            }
            Wood.WriteLine("Patch folder blacklist contents: ");
            Wood.Indent();
            foreach (string s in patchBlacklist)
            {
                Wood.WriteLine(new FileInfo(s).Name);
            }
            Wood.Unindent();
        }
        private void CreatePtBlacklist()
        {
            StreamWriter sw = File.CreateText(ptblacklistpath);
            sw.WriteLine("Assembly-CSharp.PatchNothing.mm.dll");
            sw.Close();
        }

        //
        //blacklist stuff over
        //

        //brings mods up to date if necessary
        private void RetrieveAllDlls()
        {
            var plugins = new DirectoryInfo(PluginsFolder);
            var monomod = new DirectoryInfo(PatchesFolder);
            List<string> bl = new List<string>();
            bl.AddRange(pluginBlacklist);
            bl.AddRange(patchBlacklist);
            var fl = plugins.GetFiles().ToList();
            fl.AddRange(monomod.GetFiles());
            foreach (FileInfo inpl in fl)
            {
                try
                {
                    if (inpl.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
                    string pb = PathBack(inpl);
                    if (pb == null || bl.Contains(inpl.Name)) continue;
                    var wayBack = new FileInfo(pb);
                    if (!wayBack.Exists) inpl.CopyTo(wayBack.FullName);
                }
                catch (IOException ioe)
                {
                    Wood.WriteLine($"ERROR WHILE RETRIEVING {inpl}: ");
                    Wood.Indent();
                    Wood.WriteLine(ioe);
                    Wood.Unindent();
                }
                
            }
            
            
            

            string PathBack(FileInfo fi)
            {
                if (fi.Attributes.HasFlag(FileAttributes.ReparsePoint) || fi.Extension != ".dll") { return null; }
                return Path.Combine(ModFolder, PtModData.GiveMeBackMyName(fi.Name));
            }

        }
        private void CompileModList()
        {
            string[] ModsFolderContents = Directory.GetFiles(ModFolder);
            foreach (string s in ModsFolderContents)
            {
                var fi = new FileInfo(s);
                if (fi.Extension == ".dll" && !fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    targetFiles.Add(new ModRelay(s));
                }
                if (AintThisPS(s)) PubstuntFound = true;
            }
            foreach (ModRelay mr in targetFiles)
            {
                Modlist.Items.Add(mr);

            }
            for (int i = 0; i < Modlist.Items.Count; i++)
            {
                if (Modlist.Items[i] is ModRelay)
                {
                    ModRelay mr = Modlist.Items[i] as ModRelay;
                    Modlist.SetItemCheckState(i, (mr.enabled) ? CheckState.Checked : CheckState.Unchecked);
                }
            }
        }
        private void ApplyMaskToModlist(string mask)
        {
            bool oldrst = ReadyForRefresh;
            ReadyForRefresh = false;
            Modlist.Items.Clear();
            foreach (ModRelay mr in targetFiles)
            {
                if (ModSelectedByMask(mask, mr)) Modlist.Items.Add(mr);

            }
            for (int i = 0; i < Modlist.Items.Count; i++)
            {
                if (Modlist.Items[i] is ModRelay)
                {
                    ModRelay mr = Modlist.Items[i] as ModRelay;
                    Modlist.SetItemCheckState(i, (mr.enabled) ? CheckState.Checked : CheckState.Unchecked);
                }
            }
            ReadyForRefresh = oldrst;
        }
        private bool ModSelectedByMask(string mask, ModRelay mr)
        {
            if (mask == string.Empty) return true;
            string cmm = MaskModeSelect.Text;
            if (cmm == nameof(Maskmode.Names) || cmm == nameof(Maskmode.NamesAndTags)) if (mr.ToString().ToLower().Contains(mask.ToLower())) return true;
            if (cmm == nameof(Maskmode.NamesAndTags) || cmm == nameof(Maskmode.Tags))
            {
                string[] tags = TagManager.GetTagsArray(mr.AssociatedModData.DisplayedName);
                foreach (string tag in tags)
                {
                    if (tag.ToLower().Contains(mask.ToLower())) return true;
                }
            }
            
                
            return false;
        }

        [Flags]
        private enum Maskmode
        {
            Tags,
            Names,
            NamesAndTags
        }

        //apply/unapply all mods needed
        //overload for autorefreshes (unneeded)
        [Obsolete]
        private void ApplyModlist()
        {
            Wood.WriteLine("Applying modlist.");
            Wood.Indent();
            for (int i = 0; i < Modlist.Items.Count; i++)
            {
                if (Modlist.Items[i] is ModRelay)
                {
                    ModRelay mr = Modlist.Items[i] as ModRelay;
                    if (Modlist.GetItemChecked(i))
                    {
                        mr.Enable();
                    }
                    else mr.Disable();
                    Wood.WriteLine(mr.AssociatedModData.DisplayedName + " : " + ((mr.enabled) ? "ON" : "OFF"));
                }

            }
            Wood.Unindent();
        }
        //overload for manual checks/unchecks
        private void ApplyModlist(CheckState[] cst)
        {
            Wood.WriteLine("Applying modlist from manual check.");
            if (cst != null && cst.Length == Modlist.Items.Count)
            {
                for (int i = 0; i < cst.Length; i++)
                {
                    if (Modlist.Items[i] is ModRelay)
                    {
                        ModRelay mr = Modlist.Items[i] as ModRelay;
                        if (cst[i] == CheckState.Checked)
                        {
                            mr.Enable();
                        }
                        else mr.Disable();
                    }
                }
            }
        }

        private void BringUpToDate()
        {
            foreach (ModRelay mr in Modlist.Items)
            {
                if (mr.MyType == ModRelay.ModType.Invalid || !mr.enabled || new FileInfo(mr.TarPath).Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
                byte[] ModOrigSha = mr.origchecksum;
                byte[] ModTarSha = mr.TarCheckSum;

                if (!BoiCustom.BOIC_Bytearr_Compare(ModTarSha, ModOrigSha))
                {
                    FileInfo orfi = new FileInfo(mr.ModPath);
                    FileInfo tarfi = new FileInfo(mr.TarPath);
                    if (orfi.LastWriteTime > tarfi.LastWriteTime)
                    {
                        File.Delete(tarfi.FullName);
                        File.Copy(orfi.FullName, tarfi.FullName);
                        Wood.WriteLine($"Bringing {orfi.Name} up to date: copying from Mods to {((mr.MyType != ModRelay.ModType.Patch) ? "Plugins" : "Monomod")}.");
                    }
                    else
                    {
                        File.Delete(orfi.FullName);
                        File.Copy(tarfi.FullName, orfi.FullName);
                        Wood.WriteLine($"Bringing {orfi.Name} up to date: copying from {((mr.MyType != ModRelay.ModType.Patch) ? "Plugins" : "Monomod")} to Mods.");
                    }
                }
            }
        }
        public static bool IsMyPathCorrect
        {
            get { return (currentStructureState.HasFlag(FolderStructureState.BlepFound | FolderStructureState.GameFound)); }
        }

        private static FolderStructureState currentStructureState 
        {
            get
            {
                FolderStructureState res = 0;
                if (Directory.Exists(PluginsFolder) && Directory.Exists(PatchesFolder)) res = res | FolderStructureState.BlepFound;
                if (Directory.Exists(Path.Combine(RootPath, "RainWorld_Data"))) res = res | FolderStructureState.GameFound;
                return res;
            }
        }

        [Flags]
        private enum FolderStructureState
        {
            BlepFound = 1,
            GameFound = 2
        }

        public static string RootPath = string.Empty;
        private static bool metafiletracker;
        private static bool TSbtnMode = true;
        private Options opwin;
        private InvalidModPopup inp;
        private InfoWindow iw;
        public static string BOIpath => Directory.GetCurrentDirectory();
        public static string cfgpath => Path.Combine(BOIpath, "cfg.json");
        public static string tagfilePath => Path.Combine(BOIpath, "MODTAGS.txt");
        public static bool AintThisPS(string path)
        {
            var fi = new FileInfo(path);
            if (fi.Extension != ".dll" || fi.Attributes.HasFlag(FileAttributes.ReparsePoint)) return false;
            try
            {
                using (ModuleDefinition md = ModuleDefinition.ReadModule(path))
                {
                    return (md.Assembly.FullName.Contains("PublicityStunt"));
                }
            }
            catch (IOException ioe)
            {
                Wood.WriteLine("ATPS: ERROR CHECKING MOD ASSEMBLY :");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
                Wood.WriteLine("Well, it's probably not PS.");
                return false;
            }

        }

        private List<string> patchBlacklist;
        private List<string> pluginBlacklist;
        private List<string> outrmixmods;
        private List<ModRelay> targetFiles;
        private Process rw;
        private bool firstshow;
        private bool ReadyForRefresh;
        private bool PubstuntFound;
        private bool MixmodsFound;
        private string hkblacklistpath => Path.Combine(PluginsFolder, @"plugins_blacklist.txt");
        private string ptblacklistpath => Path.Combine(PatchesFolder, @"patches_blacklist.txt");
        public static string ModFolder => Path.Combine(RootPath, "Mods");
        public static string PluginsFolder => Path.Combine(RootPath, "BepInEx", "plugins");
        public static string PatchesFolder => Path.Combine(RootPath, "BepInEx", "monomod");

        private void buttonSelectPath_Click(object sender, EventArgs e)
        {

            if (TSbtnMode)
            {
                TargetSelect.ShowDialog();
                btnSelectPath.Text = "Press again to load modlist";
                TSbtnMode = false;
                btnLaunch.Enabled = false;

            }
            else
            {
                UpdateTargetPath(TargetSelect.SelectedPath);
                btnSelectPath.Text = "Select path";
                TSbtnMode = true;
            }
        }
        private void BlepOut_Activated(object sender, EventArgs e)
        {
            UpdateTargetPath(RootPath);
            StatusUpdate();
            ApplyMaskToModlist(textBox_MaskInput.Text);
            buttonUprootPart.Visible = Directory.Exists(Path.Combine(RootPath, "RainWorld_Data", "Managed_backup"));
        }
        private void BlepOut_Deactivate(object sender, EventArgs e)
        {
            if (IsMyPathCorrect && Directory.Exists(ModFolder))
            {

            }
            TagManager.SaveToFile(tagfilePath);
        }
        private void StatusUpdate()
        {
            var cf = currentStructureState;
            if (cf.HasFlag(FolderStructureState.BlepFound | FolderStructureState.GameFound))
            {
                lblPathStatus.Text = "Path valid";
                lblPathStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightGreen);
            }
            else if (cf.HasFlag(FolderStructureState.GameFound))
            {
                lblPathStatus.Text = "No bepinex";
                lblPathStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.LightYellow);
            }
            else
            {
                lblPathStatus.Text = "Path invalid";
                lblPathStatus.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Salmon);
            }
            
            lblProcessStatus.Visible = IsMyPathCorrect;
            lblProcessStatus.Text = (rw != null && !rw.HasExited) ? "Running" : "Not running";
            lblProcessStatus.BackColor = (rw != null && !rw.HasExited) ? System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Orange) : System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Gray);
            if (rw != null && !rw.HasExited)
            {
                Modlist.Enabled = false;
            }
            else Modlist.Enabled = true;
            btnLaunch.Enabled = Modlist.Enabled;
            btnSelectPath.Enabled = Modlist.Enabled;

        }
        private void btnLaunch_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                rw = new System.Diagnostics.Process();
                rw.StartInfo.FileName = Path.Combine(RootPath, "RainWorld.exe");
                rw.Start();
                Wood.WriteLine("Game launched.");
            }

            catch (Exception ce)
            {
                Wood.WriteLine("Launch failed");
                Wood.WriteLine(ce);
            }
            btnLaunch.Enabled = false;
            StatusUpdate();

        }
        private void btn_Help_Click(object sender, EventArgs e)
        {
            if (iw == null || iw.IsDisposed) iw = new Blep.InfoWindow(this);
            iw.Show();
        }
        private void buttonUprootPart_Click(object sender, EventArgs e)
        {
            Blep.PartYeet py = new Blep.PartYeet(this);
            AddOwnedForm(py);
            py.ShowDialog();
        }
        private void buttonClearMeta_Click(object sender, EventArgs e)
        {
            Blep.MetafilePurgeSuggestion psg = new Blep.MetafilePurgeSuggestion(this);
            AddOwnedForm(psg);
            psg.ShowDialog();
        }
        private void Modlist_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!ReadyForRefresh) return;
            if (Modlist.Items[e.Index] is ModRelay && (Modlist.Items[e.Index] as ModRelay).MyType == ModRelay.ModType.Invalid && e.NewValue == CheckState.Checked)
            {
                //e.NewValue = CheckState.Unchecked;
                if (inp == null || inp.IsDisposed) inp = new InvalidModPopup(this, (Modlist.Items[e.Index] as ModRelay).AssociatedModData.DisplayedName);
                inp.ShowDialog();
            }
            CheckState[] ich = new CheckState[Modlist.Items.Count];
            for (int i = 0; i < ich.Length; i++)
            {
                ich[i] = Modlist.GetItemCheckState(i);
            }
            ich[e.Index] = e.NewValue;
            Wood.WriteLine("Mod state about to change: " + (Modlist.Items[e.Index] as ModRelay).AssociatedModData.DisplayedName + "; Type: " + (Modlist.Items[e.Index] as ModRelay).MyType.ToString());
            ApplyModlist(ich);
            Wood.WriteLine("Resulting state: " + ((Modlist.Items[e.Index] as ModRelay).enabled ? "ON" : "OFF"));
        }
        private void BlepOut_FormClosing(object sender, FormClosingEventArgs e)
        {
            Wood.WriteLine("BOI shutting down. " + DateTime.Now);
            BoiConfigManager.WriteConfig();
            List<string> mns = new List<string>();
            foreach (ModRelay mr in targetFiles) mns.Add(mr.AssociatedModData.DisplayedName);
            TagManager.TagCleanup(mns.ToArray());
            TagManager.SaveToFile(tagfilePath);
        }
        private void buttonOption_Click(object sender, EventArgs e)
        {
            if (opwin == null || opwin.IsDisposed) opwin = new Options(this);
            opwin.Show();
        }

        private void Modlist_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Wood.WriteLine($"Drag&Drop: {files.Length} files were dropped in the mod list.");
            Wood.Indent();
            foreach (string file in files)
            {
                // get the file info for easier operations
                FileInfo ModFileInfo = new FileInfo(file);
                // check if we are dealing with a dll file
                if (!String.Equals(ModFileInfo.Extension, ".dll", StringComparison.CurrentCultureIgnoreCase))
                {
                    Wood.WriteLine($"Error: {ModFileInfo.Name} was ignored, as it is not a dll file.");
                    continue;
                }
                // move the dll file to the Mods folder
                string ModFilePath = Path.Combine(RootPath, "Mods", ModFileInfo.Name);
                if(File.Exists(ModFilePath))
                {
                    Wood.WriteLine($"Error: {ModFileInfo.Name} was ignored, as it already exists.");
                    continue;
                }
                // move the dll file to the Mods folder
                File.Copy(ModFileInfo.FullName, ModFilePath);
                // get mod data
                var mr = new ModRelay(ModFilePath);
                // add the mod to the mod list
                targetFiles.Add(mr);
                Wood.WriteLine($"{ModFileInfo.Name} successfully added.");
                // since it's a new mod just added to the folder, it shouldn't be checked as active, nothing else to do here
            }
            Wood.Unindent();
            Wood.WriteLine("Drag&Drop operation ended.");
        }
        private void Modlist_DragEnter(object sender, DragEventArgs e)
        {
            // if we're about to drop a file, indicate a copy to allow the drop
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void textBoxMaskInput_TextChanged(object sender, EventArgs e)
        {
            ApplyMaskToModlist(textBox_MaskInput.Text);
        }

        private void Modlist_SelectionChanged(object sender, EventArgs e)
        {
            if (Modlist.SelectedItem == null) return;
            TagInputBox.Text = TagManager.GetTagString(((ModRelay)Modlist.SelectedItem)?.AssociatedModData.DisplayedName);
        }
        private void TagTextChanged(object sender, EventArgs e)
        {
            if (Modlist.SelectedItem == null) return;
            TagManager.SetTagData(((ModRelay)Modlist.SelectedItem).AssociatedModData.DisplayedName, TagInputBox.Text);
        }
        private void MaskModeSelect_TextChanged(object sender, EventArgs e)
        {
            if (!ReadyForRefresh) return;
            ApplyMaskToModlist(textBox_MaskInput.Text);
        }

        private void OpenAudbBrowser(object sender, EventArgs e)
        {
            if (browser == null || browser.IsDisposed) { browser = new AUDBBrowser(); browser.Show(); }
        }
        private AUDBBrowser browser;

    }
}
