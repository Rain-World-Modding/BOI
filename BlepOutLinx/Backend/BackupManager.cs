using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Threading.Tasks;

using static Blep.Backend.Core;

namespace Blep.Backend
{
    public static class BackupManager
    {
        public static string BackupFolderPath => Path.Combine(BOIpath, "Backups");
        /// <summary>
        /// Loads the list of backups located in <see cref="BackupFolderPath"/>.
        /// </summary>
        public static void LoadBackupList()
        {
            bool flag = IsMyPathCorrect;
            if (!Directory.Exists(BackupFolderPath)) Directory.CreateDirectory(BackupFolderPath);
            if (flag)
            {
                ActiveSave = new UserDataStateRelay(UserDataFolder) { CreationTime = DateTime.UtcNow } ;
            }
            AllBackups.Clear();
            foreach (DirectoryInfo dir in new DirectoryInfo(BackupFolderPath).GetDirectories())
            {
                
                AllBackups.Add(new UserDataStateRelay(dir) { } );
            }
            
        }
        /// <summary>
        /// Clones the active save to a new subfolder in <see cref="BackupFolderPath"/>, adds it to backups list.
        /// </summary>
        public static void StashActiveSave()
        {
            if (!IsMyPathCorrect) return;
            UserDataStateRelay udsr = ActiveSave ?? new UserDataStateRelay(UserDataFolder);
            AllBackups.Add(udsr.CloneTo(PathForNewBackup));
        }
        /// <summary>
        /// Erases a given save, whether it's active or not.
        /// </summary>
        /// <param name="toDelete"><see cref="UserDataStateRelay"/> to be deleted.</param>
        /// <returns><c>True</c> if the operation was successful; <c>false</c> otherwise.</returns>
        public static bool TryDeleteSave(UserDataStateRelay toDelete)
        {
            try
            {
                Directory.Delete(toDelete.Location, true);
                if (toDelete.Location == ActiveSave?.Location) Directory.CreateDirectory(toDelete.Location);
                if (AllBackups.Contains(toDelete)) AllBackups.Remove(toDelete);
                else if (ActiveSave == toDelete) ActiveSave = null;
                return true;
            }
            catch (IOException ioe)
            {
                Wood.WriteLine($"ERROR DELETING SAVE {toDelete.MyName}:");
                Wood.Indent();
                Wood.WriteLine(ioe);
                Wood.Unindent();
                return false;
            }
            
        }
        /// <summary>
        /// Restores active savefile from a given backup; aborts if active savefile is not empty.
        /// </summary>
        /// <param name="backup"><see cref="UserDataStateRelay"/> to be cloned.</param>
        /// <returns><c>true</c> if the operation was successful; <c>false</c> otherwise.</returns>
        public static bool RestoreActiveSaveFromBackup(UserDataStateRelay backup)
        {
            if (backup.Location == ActiveSave?.Location) { Wood.WriteLine("Can not copy active save into itself!"); return false; }
            if (ActiveSave?.CurrState != UserDataStateRelay.UDSRState.Empty) { Wood.WriteLine("Active save not empty, will not overwrite!"); return false; };
            try
            {
                Wood.WriteLine("Restoring save from backup...");
                ActiveSave = backup.CloneTo(UserDataFolder);
                Wood.WriteLine("Backup restore successful.");
                return true;
            }
            catch (NullReferenceException ne)
            {
                Wood.WriteLine("ERROR RESTORING A SAVEFILE BACKUP:");
                Wood.WriteLine(ne, 1);
            }
            return true;
        }


        /// <summary>
        /// Restores active save from a backup in <see cref="AllBackups"/> under a given index.
        /// <para>
        /// Aborts if index is invalid or active save is not empty.
        /// </para>
        /// </summary>
        /// <param name="index">Index of an item in <see cref="AllBackups"/>.</param>
        /// <returns><c>true</c> if the operation was successful; <c>false</c> otherwise.</returns>
        public static bool RestoreActiveSaveFromBackup(int index)
        {
            try
            {
                return RestoreActiveSaveFromBackup(AllBackups[index]);
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }
        /// <summary>
        /// Writes settings files for active save and backups.
        /// </summary>
        public static void SaveSettingsForAll()
        {
            ActiveSave?.RecordData();
            foreach (UserDataStateRelay udsr in AllBackups) udsr.RecordData();
        }

        /// <summary>
        /// Returns a path for a new backup to be created.
        /// </summary>
        public static string PathForNewBackup => Path.Combine(BackupFolderPath, $"{DateTime.UtcNow.Ticks}");
        /// <summary>
        /// Path to active save.
        /// </summary>
        public static string UserDataFolder => Path.Combine(RootPath, "UserData");

        public static UserDataStateRelay ActiveSave { get; set; }
        public static List<UserDataStateRelay> AllBackups { get { if (_abu == null) _abu = new List<UserDataStateRelay>(); return _abu; } set { _abu = value; } }
        private static List<UserDataStateRelay> _abu;

        /// <summary>
        /// Represents a UserData folder or a backup of such.
        /// </summary>
        public class UserDataStateRelay
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="path">Folder path.</param>
            public UserDataStateRelay(string path) : this(new DirectoryInfo(path))
            {

            }
            public UserDataStateRelay(DirectoryInfo dir)
            {
                Locdir = dir;
                ReadData();
                if (Data.Name == string.Empty && !IsActiveSave) Data.Name = Data.CreationTime.ToString();
            }

            public string DateTimeString => (!IsActiveSave) ? Data.CreationTime.ToString() : "N/A";
            public string UserDefinedName { get { return Data.Name; } set { Data.Name = value; } }
            public string MyName => (IsActiveSave) ? "Current save" : UserDefinedName;
            public string UserNotes { get => Data.Notes; set { Data.Notes = value; } }
            public DateTime CreationTime { get { return Data.CreationTime; } set { Data.CreationTime = value; } }

            /// <summary>
            /// Deserializes settings file.
            /// </summary>
            /// <returns></returns>
            internal bool ReadData()
            {
                try
                {
                    Data = JsonConvert.DeserializeObject<AttachedData>(File.ReadAllText(DataJsonPath));
                    return true;
                }
                catch (Exception e)
                {
                    Wood.WriteLine("Error reading config file for a UDSR:");
                    Wood.WriteLine(e, 1);
                    return false;
                }
            }
            /// <summary>
            /// Writes attached data json for the UDSR.
            /// </summary>
            /// <returns><c>true</c> if the operation was successful; <c>false</c> otherwise.</returns>
            internal bool RecordData()
            {
                if (CurrState == UDSRState.Invalid) return false;
                try
                {
                    string outjson = JsonConvert.SerializeObject(Data, Formatting.Indented);
                    File.WriteAllText(DataJsonPath, outjson);
                    return true;
                }
                catch (IOException ioe)
                {
                    Wood.WriteLine($"Error saving backup data for savefile backup {MyName}");
                    Wood.Indent();
                    Wood.Write(ioe);
                    Wood.Unindent();
                }
                return false;
            }


            public string Location { get { return Locdir?.FullName ?? string.Empty; } set { Locdir = new DirectoryInfo(value); } }
            private string DataJsonPath => Path.Combine(Location, "UDBACKUPDATA.json");
            public DirectoryInfo Locdir { get; set; }
            public bool IsActiveSave => Location == BackupManager.ActiveSave?.Location;

            public UDSRState CurrState
            {
                get
                {
                    if (!Locdir.Exists) return UDSRState.Invalid;
                    if (Locdir.GetFiles().Length > 1) return UDSRState.Normal;
                    return UDSRState.Empty;
                }
            }
            public enum UDSRState
            {
                Invalid,
                Empty,
                Normal
            }
            /// <summary>
            /// Clones itself to a new location; returns the newly created <see cref="UserDataStateRelay"/>.
            /// </summary>
            /// <param name="to">Target path.</param>
            /// <returns>A newly created <see cref="UserDataStateRelay"/>.</returns>
            public UserDataStateRelay CloneTo(string to)
            {
                var movequeue = new DirectoryInfo(Location).EnqueueRecursiveCopy(to);
                Task.WaitAll(movequeue.ToArray());
                int terrc = 0;// BoiCustom.BOIC_RecursiveDirectoryCopy(Location, to);
                foreach (var movt in movequeue)
                {
                    if (movt.Exception != null)
                    {
                        Wood.WriteLine("Error during EnqRecDir:");
                        Wood.WriteLine(movt.Exception, 1);
                        terrc++;
                    }
                }
                Wood.WriteLine((terrc == 0) ? $"Savefolder state successfully copied to {to}" : $"Attempt to copy a savefolder from {Location} to {to} complete; total of {terrc} errors encountered.");
                UserDataStateRelay Nudsr = new UserDataStateRelay(to)
                {
                    Data = this.Data
                };
                Nudsr.Data.CreationTime = DateTime.UtcNow;
                return Nudsr;
            }
            public override string ToString()
            {
                return $"{MyName} : {CurrState}";
            }

            private AttachedData Data;
            /// <summary>
            /// A set of attached fields for the savefile.
            /// </summary>
            public struct AttachedData
            {
                public string Notes { get { return _notes ?? string.Empty; } set { _notes = value; } }
                private string _notes;
                public string Name { get { return _name ?? string.Empty; } set { _name = value; } }
                private string _name;

                public DateTime CreationTime { get => _ct; set { _ct = value; } }
                private DateTime _ct;
            }

        }
    }
}
