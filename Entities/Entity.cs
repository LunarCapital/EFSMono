using EFSMono.Common.Autoload;
using EFSMono.Entities.Components.Controller;
using Godot;

namespace EFSMono.Entities
{
    public abstract class Entity : KinematicBody2D
    {
        //COMPONENTS
        public abstract IController controller { get; protected set; }

        private const int Gravity = 16; //TODO this will go in Globals eventually
        private const int BonusGrav = 320; //this might be entity-specific?
        private float _airTime;
        protected float fallVelocity { get; set; }


        public Sprite fullSprite { get; private set; }
        public bool falling { get; protected set; }
        public float spriteDrawPos { get; private set; }
        public bool raiseTileMapRequest { get; private set; } //al lthis shit is temporary btw before i eventually move to states

        public Entity()
        {
            foreach (Node child in this.GetChildren())
            {
                if (child is Sprite sprite)
                {
                    this.fullSprite = sprite;
                    break;
                }
            }
        }

        public override void _Ready()
        {
            this._airTime = 0;
            this.fallVelocity = 0;
            this.falling = false;
            this.spriteDrawPos = 0;
            this.raiseTileMapRequest = false;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _PhysicsProcess(float delta)
        {
            if (this.falling)
            {
                this._PhysicsProcessFall(delta);
            }
        }

        private void _PhysicsProcessFall(float delta)
        {
            this.fallVelocity += (Gravity + (this._airTime * BonusGrav)) * delta;
            this._airTime += delta;
            this.spriteDrawPos += this.fallVelocity;
            //GD.PrintS("spriteDrawPos: " + this.spriteDrawPos + ", fall velocity: " + this.fallVelocity);
            if (this.spriteDrawPos >= 0)
            {
                GD.PrintS("fin falling, velocity: " + this.fallVelocity + ", air time: " + this._airTime + ", spritedrawpos: " +this.spriteDrawPos);
                this.spriteDrawPos = 0;
                this.falling = false;
            }
            else
            {
                if (this.spriteDrawPos < - Globals.TileHeight)
                {
                    GD.PrintS("raise request, spritedrawpos: " + this.spriteDrawPos);
                    this.raiseTileMapRequest = true;
                }
            }
        }

        public void SetFall()
        {
            this.falling = true;
            this.Position += new Vector2(0, Globals.TileHeight);
            this.spriteDrawPos = -Globals.TileHeight;
        }

        public void SetGrounded()
        {
            this.fallVelocity = 0;
            this._airTime = 0;
        }

        public void RaiseZIndex()
        {
            this.spriteDrawPos = 0;
            this.raiseTileMapRequest = false;
            this.Position -= new Vector2(0, Globals.TileHeight);
        }
    }
}