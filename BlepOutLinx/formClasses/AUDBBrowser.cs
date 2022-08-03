using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Blep.Backend;

using static Blep.Backend.Core;

namespace Blep
{
    public partial class AUDBBrowser : Form
    {
        public AUDBBrowser()
        {
            InitializeComponent();
            FetchAndRefresh();
        }

        private void RefreshTriggered(object sender, EventArgs e)
        {
            FetchAndRefresh();
        }
        
        public void FetchAndRefresh()
        {
            bool fl = VoiceOfBees.FetchRelays();
            listAUDBEntries.Items.Clear();
            foreach (var rel in VoiceOfBees.ModEntryList)
            {
                listAUDBEntries.Items.Add(rel);
            }
            DrawBoxes();
            if (!fl) labelOperationStatus.Text = "Retrieving modlist failed, check BOILOG.txt for details";
        }

        public void DrawBoxes()
        {
            buttonDownload.Enabled = IsMyPathCorrect;
            btn_InstallBep.Enabled = currentStructureState.HasFlag(FolderStructureState.GameFound);
            var currEntry = listAUDBEntries.SelectedItem as VoiceOfBees.AUDBEntryRelay;

            labelEntryAuthors.Text = currEntry?.author ?? string.Empty;
            labelEntryDescription.Text = currEntry?.description ?? string.Empty;
            labelEntryName.Text = currEntry?.name ?? string.Empty;
            listDeps.Items.Clear();
            if (currEntry?.deps != null) foreach (var dep in currEntry.deps) listDeps.Items.Add(dep);
            labelOperationStatus.Text = "[Idle]";
        }

        private void listAUDBEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawBoxes();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            var currEntry = listAUDBEntries.SelectedItem as VoiceOfBees.AUDBEntryRelay;
            if (currEntry != null)
            {
                labelOperationStatus.Text = $"Downloading {currEntry.name} and dependencies...";
                labelOperationStatus.Text = (currEntry.TryDownload(ModFolder))? $"Downloaded {currEntry.name}." : $"Could not download {currEntry.name}! Check BOILOG.txt for details";
            }
        }

        private void btn_InstallBep_Click(object sender, EventArgs e)
        {
            int errc = VoiceOfBees.DownloadBepAsync(RootPath);
            labelOperationStatus.Text = (errc == 0) ? "BepInEx installed successfully" : $"{errc} error(s) encountered while installing!";
        }
    }
}
