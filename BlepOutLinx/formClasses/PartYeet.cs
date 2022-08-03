using System;
using System.IO;
using System.Windows.Forms;

using static Blep.Backend.Core;

namespace Blep
{
    //todo: old garbage, redo
    public partial class PartYeet : Form
    {
        public PartYeet(BlepOut mainform)
        {
            mf = mainform;
            mf.Enabled = false;
            InitializeComponent();
        }

        private BlepOut mf;

        private void buttonUproot_Click(object sender, EventArgs e)
        {
            try
            {
                var manf = Path.Combine(RootPath, "RainWorld_Data", "Managed");
                var manbuf = Path.Combine(RootPath, "RainWorld_Data", "Managed_backup");
                Directory.Delete(manf);
                Directory.Move(manbuf, manf);
                label2.Text = "Partiality Launcher successfully uninstalled, you're free to go!";
            }
            catch
            {
                label2.Text = "Uhhhh, something went wrong. Check BOILOG.txt for details; you may also want to verify game integrity.";
            }
            finally
            {
                buttonUproot.Visible = false;
                buttonCancel.Text = "Back";
                mf.buttonUprootPart.Visible = false;
            }
            //System.IO.Directory.Delete(BlepOut.RootPath + @"\RainWorld_Data\Managed", true);
            //System.IO.Directory.Move(BlepOut.RootPath + @"\RainWorld_Data\Managed_backup", BlepOut.RootPath + @"\RainWorld_Data\Managed");
            
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void PartYeet_FormClosed(object sender, FormClosedEventArgs e)
        {
            mf.Enabled = true;
        }
    }
}
