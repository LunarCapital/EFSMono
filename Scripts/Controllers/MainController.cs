using Godot;
using System;
using System.Linq;
using SCol = System.Collections.Generic;
using GCol = Godot.Collections;
using TileControllerNamespace;

public class MainController : Node
{
    
    public override void _Ready()
    {   
        TileController tileControl = (TileController)GetNode("Controllers/TileController");
        Node2D world = (Node2D)GetNode("World");
        var children = new SCol.List<Node>(world.GetChildren().OfType<Node>());
        GetChildren();
        tileControl.BuildTileNodes(children);
        //TODO: there are some methods reliant on tilemaps being a list of TileMaps that is:
            //1. ORDERED
            //2. Consisting of nodes with UNIQUE Z INDEXES
        //may need to create unique class TileMapCollection to ensure this.
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
