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
            var res = Blep.Backend.BoiCustom.AllocConsole();
            var err = Blep.Backend.BoiCustom.GetLastError();
            Application.Init();
            ListView modlist = new();
            TableView mainLayout = new TableView();

            Application.Top.AddKeyBinding(Key.Q, Command.QuitToplevel);
#warning impl
            Application.Run(excb);
        }
    }
}
