using System.Windows.Forms;
using System;

using static Blep.Backend.Core;

namespace Blep
{
    [Obsolete]
    public partial class InfoWindow : Form
    {
        public InfoWindow()
        {
            InitializeComponent();
            TabControl.TabPages[0].VerticalScroll.Visible = true;
            TabControl.TabPages[0].VerticalScroll.Enabled = true;
            label2.Text = label2.Text.Replace("<VersionNumber>", VersionNumber);
        }
        private int hmm = 0;

        private void linkLabelRDB_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.raindb.net/");
        }

        private void linkLabelDiscord_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/rainworld");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://cdn.discordapp.com/emojis/776904253741858847.gif?v=1");
        }

        private void linkLabelWiki_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://rain-world-modding.fandom.com/wiki/Rain_World_Modding_Wiki");
        }

        private void label11_Click(object sender, System.EventArgs e)
        {
            if (hmm > 3)
            {
                linkLabel1.Visible = true;
                label15.Visible = true;
            }
            else hmm++;
        }

        private void linkLabelBep_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://drive.google.com/file/d/1WcCCsS3ABBdO1aX-iJGeqeE07YE4Qv88/view");
        }

        private void InfoWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }
    }
}
