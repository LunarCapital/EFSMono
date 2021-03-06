﻿using EFSMono.Common.Autoload;
using EFSMono.GameObjects;
using Godot;
using StatID = EFSMono.Preloading.Stats.StatsEnumerator.StatID;

namespace EFSMono.Entities.Common.Components.Gravity
{
    /// <summary>
    /// Base class for gravity components, which only contains some constants. Will be used for falling physics/gravity-related physics if some entity
    /// does not explicitly has its gravity parameters set.
    /// </summary>
    public abstract class GravityComponent
    {
        private readonly Entity _parent;

        public abstract bool inAir { get; protected internal set; }
        public abstract bool raiseHeight { get; protected set; }

        public abstract float airTime { get; protected set; }
        public abstract float fallVelocity { get; protected internal set; }
        public abstract float spriteDrawHeightPos { get; protected set; }

        private float _prevSpriteHeight;

        public GravityComponent(Entity parent)
        {
            this._parent = parent;

            this.inAir = false;
            this.airTime = 0;
            this.fallVelocity = 0;
            this.spriteDrawHeightPos = 0;
            this._prevSpriteHeight = 0;
        }

        /// <summary>
        /// Increases falling velocity and sprite height drawing position, while also incrementing air time.
        /// Stops falling if sprite hits the ground, ascends a TileMap if sprite hits TileHeight.
        /// </summary>
        /// <param name="delta"></param>
        public virtual void ProcessFall(float delta) {
            if (this.inAir)
            {
                this.fallVelocity += (this._parent.statsComponent.GetTotalStat(StatID.Gravity) + (this.airTime * this._parent.statsComponent.GetTotalStat(StatID.BonusGrav))) * delta;
                this.airTime += delta;
                this.spriteDrawHeightPos += this.fallVelocity;
                
                if (this.spriteDrawHeightPos >= 0)
                { //sprite height cannot go below 'floor'
                    this.spriteDrawHeightPos = 0;
                    if (this._prevSpriteHeight == this.spriteDrawHeightPos)
                    { //height pos unchanged, .'. ground
                        this.inAir = false;
                    }
                }
                else
                { //high enough to be raised by a tilemap
                    if (this.spriteDrawHeightPos < -Globals.TileHeight)
                    {
                        this.raiseHeight = true;
                    }
                }
                this._prevSpriteHeight = this.spriteDrawHeightPos;
            }
        }

        public void SetInAir()
        {
            this.inAir = true;
            this._parent.Position += new Vector2(0, Globals.TileHeight);
            this.spriteDrawHeightPos = -Globals.TileHeight;
        }

        public void SetGrounded()
        {
            this.fallVelocity = 0;
            this.airTime = 0;
        }

        /// <summary>
        /// Called by EntityTracker and changes this class's parameters appropriately to make it seem like its parent Entity was raised in height.
        /// I'm not too happy with using a boolean to notify a controller that checks all objects every frame, but it's probably faster than an event
        /// and doesn't require me to get a MessageHub from EntityTracker to every damn entity.
        /// </summary>
        public void SetAscendParameters()
        {
            this.spriteDrawHeightPos = 0;
            this.raiseHeight = false;
            this._parent.Position -= new Vector2(0, Globals.TileHeight);
        }
    }
}
