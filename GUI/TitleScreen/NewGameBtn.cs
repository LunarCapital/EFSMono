using System;
using EFSMono.SystemModules;
using Godot;

namespace EFSMono.GUI.TitleScreen
{
    public class NewGameBtn : Button
    {
        public static void OnPressed(MainController main, PackedScene startWorld)
        {
            if (main is null) throw new ArgumentNullException(nameof(main));
            main.SwitchWorld(startWorld);
        }
    }
}
