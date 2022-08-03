using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

using static Blep.Backend.Core;

namespace Blep.TUI
{
    internal static class TCore
    {
        internal static void Init(Func<Exception, bool> excb = null)
        {
            //var res = Blep.Backend.BoiCustom.AllocConsole();
            //var err = Blep.Backend.BoiCustom.GetLastError();
            Application.Init();
            ListView modlist = new();

            var win = new Window()
            {
                X = 0,
                Y = 0,
                Height = Dim.Fill(),
                Width = Dim.Fill(),
            };
            win.Add(new Dialog("Thing", new Button("_Exit") { Id = "btn_exit" }) { Text = "You probably can't do anything here. Launch from windows explorer or something." });
            Application.Top.Add(win);

            //Application.Top.AddKeyBinding(Key.Q, Command.QuitToplevel);
#warning impl
            Application.Run(
                excb
                );
        }
    }
}
