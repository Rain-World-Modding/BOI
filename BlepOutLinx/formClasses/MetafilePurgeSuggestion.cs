using System;
using System.IO;
using System.Windows.Forms;
using Blep.Backend;

using static Blep.Backend.Core;

namespace Blep
{
    public partial class MetafilePurgeSuggestion : Form
    {
        public MetafilePurgeSuggestion(Blep.BlepOut mainform)
        {
            mf = mainform;
            InitializeComponent();
            mf.Enabled = false;
        }

        private BlepOut mf;

        private void buttonUproot_Click(object sender, EventArgs e)
        {
            int errc = 0, succ = 0;
            string[] modfoldercontents = Directory.GetFiles(ModFolder);
            foreach (string path in modfoldercontents)
            {
                try
                {
                    var fi = new FileInfo(path);
                    if (fi.Extension == ".modHash" || fi.Extension == ".modMeta")
                    {
                        File.Delete(path);
                        succ++;
                    }

                }
                catch (Exception ex)
                {
                    Wood.WriteLine("Error cremating PL victims:");
                    Wood.WriteLine(ex, 1);
                    errc++;
                }
            }
            mf.buttonClearMeta.Visible = false;
            buttonUproot.Visible = false;
            buttonCancel.Text = "Back";
            label2.Text = $"Cleanup complete. Your karma just went up by {succ - errc}.";
        }

        private void MetafilePurgeSuggestion_FormClosed(object sender, FormClosedEventArgs e)
        {
            mf.Enabled = true;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
