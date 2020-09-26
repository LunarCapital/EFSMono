using EFSMono.SystemModules;
using Godot;

namespace EFSMono.GUI.TitleScreen
{
    /// <summary>
    /// Main Menu class. Handles title screen GUI connection duties.
    /// </summary>
    public class MainMenu : Control
    {
        private const string NewGameBtnName = "NewGameBtn";

        private const string StartWorldPath = "res://Worlds/WorldTemplate.tscn";

        private MainController _main;
        private NewGameBtn _newGameBtn;

        public override void _Ready()
        {
            this._main = (MainController) this.GetParent(); //sucks that we need a reference to Main but what can you do
            this._newGameBtn = (NewGameBtn) this.FindNode(MainMenu.NewGameBtnName, true, true);

            this._ConnectGUIElements();
        }

        private void _ConnectGUIElements()
        {
            var startingWorld = (PackedScene)ResourceLoader.Load(StartWorldPath);
            var newGameParams = new Godot.Collections.Array
            {
                this._main,
                startingWorld
            };
            this._newGameBtn.Connect("pressed", this._newGameBtn, nameof(NewGameBtn.OnPressed), newGameParams);
        }
    }
}
