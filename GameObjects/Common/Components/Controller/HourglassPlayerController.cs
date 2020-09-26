using System;
using System.Collections.Generic;
using EFSMono.Common.Enums;
using EFSMono.Entities.Common.Components.Controller;
using EFSMono.Entities.Players.States;
using EFSMono.GameObjects.Common.Components.Action;
using Godot;
using ActionID = EFSMono.Common.Enums.Actions.ActionID;

namespace EFSMono.GameObjects.Common.Components.Controller
{
    public class HourglassPlayerController : IController
    {
        public Vector2 motion { get; private set; }
        public Vector2 target { get; private set; }
        private readonly Dictionary<ActionID, ActionComponent> _actions;

        private readonly Entity _parent;
        private IPlayerState _state;

        public HourglassPlayerController()
        {
        }

        public void ControlEntity(float delta)
        {
            throw new NotImplementedException();
        }

        public ActionComponent GetAction(Actions.ActionID actionID)
        {
            throw new NotImplementedException();
        }
    }
}
