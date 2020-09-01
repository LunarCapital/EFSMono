using System;
using System.Collections.Generic;
using EFSMono.Common.Autoload;
using EFSMono.Common.DataStructures.Geometry;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.DataKeys;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge
{
    /// <summary>
    /// <para>
    /// A class dedicated to building Ledges. A proper description of ledges are in the LedgeBuilderResult description
    /// but a brief explanation here:
    ///     * Every outer tile in a tile group may be either connected to an adjacent tile on a lower height, or nothing.
    ///     * If an outer tile has an adjacent lower tile, entities can pass through to fall down.
    ///     * If an outer tile has no adjacent lower tile, its edge should act as a wall to prevent anything falling into the void.
    ///     * Outer tiles may consist of any combination of walls/pass-through. These need to be grouped. Examples below.
    ///     * If a tile group consisting of one tile (which therefore has only one outer tile, itself) has two adjacent walls
    ///       and two adjacent pass-through, it has ONE LEDGE GROUP (passable ledges are not grouped).
    ///     * If a tile group consisting of one tile (which therefore has only one outer tile, itself) has one wall north, one
    ///       pass-through east, one wall south, and one pass-through west, it has TWO LEDGE GROUPS (important to understand this,
    ///       even though this example also has two walls and two pass-through, they are placed into two groups because the same
    ///       kinds of ledges are not adjacent).
    ///
    /// This class does not superimpose ledges. An explanation of that is in LedgeSuperimposer.
    /// </para>
    /// </summary>
    public static class LedgeBuilder
    {
        /// <summary>
        /// Builds the initial layer of ledges given all perimeter data as an input.
        /// More specifically, initial layer means that each TileMap has ledge data built for themselves,
        /// WITHOUT considering superimposition.
        /// </summary>
        /// <param name="perimData">Info on all perimeters.</param>
        /// <param name="tileMaps">List of all tilemaps.</param>
        /// <returns>Four dictionaries matching LedgeData's four properties which map EdgeCollections containing ledge data to multiple fields.</returns>
        public static (Dictionary<LedgeCollKey, EdgeCollection<TileEdge>>, Dictionary<LedgeGroupKey, int>,
                       Dictionary<HoleGroupKey, int>, Dictionary<TileGroupKey, int>) BuildLedges(this TileMapList tileMaps,
                                                                                                 PerimeterData perimData)
        {
            if (tileMaps is null) throw new ArgumentNullException(nameof(tileMaps));
            if (perimData is null) throw new ArgumentNullException(nameof(perimData));

            var ledgeCollMap = new Dictionary<LedgeCollKey, EdgeCollection<TileEdge>>();
            var ledgeGroupMap = new Dictionary<LedgeGroupKey, int>();
            var holeGroupMap = new Dictionary<HoleGroupKey, int>();
            var tileGroupMap = new Dictionary<TileGroupKey, int>();

            foreach (TileMap tileMap in tileMaps.Values)
            {
                int maxTileGroups = perimData.GetMaxTileGroup(tileMap);
                tileGroupMap.Add(new TileGroupKey(tileMap), maxTileGroups);
                for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
                {
                    int maxHoleGroups = perimData.GetMaxHoleGroup(tileMap, tileGroup);
                    holeGroupMap.Add(new HoleGroupKey(tileMap, tileGroup), maxHoleGroups);
                    for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                    {
                        EdgeCollection<TileEdge> thisPerim = perimData.GetEdgeCollection(tileMap, tileGroup, holeGroup);
                        (ledgeCollMap, ledgeGroupMap) = _FillLedges(ledgeCollMap, ledgeGroupMap, tileMaps,
                                                                         tileMap, thisPerim, tileGroup, holeGroup);
                    }
                }
            }

            return (ledgeCollMap, ledgeGroupMap, holeGroupMap, tileGroupMap);
        }

        /// <summary>
        /// Given a single perimeter, separate it into different ledge groups as appropriate, separate its Edges
        /// into different EdgeCollections, one for each ledge group, then return both as dictionaries.
        /// </summary>
        /// <param name="ledgeCollMap">Dict which maps ledge group to EdgeCollection.</param>
        /// <param name="ledgeGroupMap">Dict which maps superimposed TileMap to ledge group.</param>
        /// The above two are here for copy-and-return purposes (maintaining object immutability).
        /// <param name="tileMaps">List of all TileMaps.</param>
        /// <param name="tileMap">TileMap that ledges are being filled for.</param>
        ///<param name="perimeter">EdgeCollection holding perimeter which will be separated in this func.</param>
        /// <param name="tileGroup">TileGroup that ledges are being filled for.</param>
        /// <param name="holeGroup">HoleGroup that ledges are being filled for.</param>
        /// <returns>Two dictionaries matching the two input dicts but with the new ledge data added to each.</returns>
        private static (Dictionary<LedgeCollKey, EdgeCollection<TileEdge>>, Dictionary<LedgeGroupKey, int>) _FillLedges(
            IDictionary<LedgeCollKey, EdgeCollection<TileEdge>> ledgeCollMap,
            IDictionary<LedgeGroupKey, int> ledgeGroupMap,
            TileMapList tileMaps,
            TileMap tileMap,
            EdgeCollection<TileEdge> perimeter,
            int tileGroup,
            int holeGroup)
        {
            var ledgeCollMapClone = new Dictionary<LedgeCollKey, EdgeCollection<TileEdge>>(ledgeCollMap);
            var ledgeGroupMapClone = new Dictionary<LedgeGroupKey, int>(ledgeGroupMap);

            var ledges = new EdgeCollection<TileEdge>();
            int ledgeGroup = 0;

            foreach (TileEdge edge in perimeter)
            {
                Vector2 currentTile = edge.tileCoords;
                int currentLayer = tileMap.ZIndex;
                Vector2 adjTile = _GetAdjacentLowerTile(tileMaps, edge, currentLayer);

                if (adjTile == currentTile)
                { //no adjacent tile exists
                    ledges.Add(edge);
                }
                else if (ledges.Count > 0)
                { //gap in ledges, finish current ledge group and move to next one
                    ledgeCollMapClone.Add(new LedgeCollKey(tileMap, tileGroup, holeGroup, tileMap, ledgeGroup),
                                            new EdgeCollection<TileEdge>(ledges.GetOrderedCollection()));
                    ledgeGroup++;
                    ledges = new EdgeCollection<TileEdge>();
                }
            }

            if (ledges.Count > 0)
            { //store ledges if not done already
                ledgeCollMapClone.Add(new LedgeCollKey(tileMap, tileGroup, holeGroup, tileMap, ledgeGroup),
                    new EdgeCollection<TileEdge>(ledges.GetOrderedCollection()));
                ledgeGroup++;
            }

            ledgeGroupMapClone.Add(new LedgeGroupKey(tileMap, tileGroup, holeGroup, tileMap), ledgeGroup);
            return (ledgeCollMapClone, ledgeGroupMapClone);
        }

        /// <summary>
        /// From some input edge, grab the tile it originated from and check if there is an adjacent tile
        /// in the direction TO the edge on any lower TileMap.
        /// Basically we are checking if it is possible to move from the original tile in the direction
        /// of the edge and fall to a lower tile.
        /// Should no adjacent tile exist, returns the current tile.
        /// </summary>
        /// <param name="tileMaps">List of all tilemaps.</param>
        /// <param name="edge">Edge that is being examined.</param>
        /// <param name="currentLayer">Current layer that edge is on.</param>
        /// <returns>Coords of the highest adjacent tile, or current tile if no adjacent tile exists.</returns>
        private static Vector2 _GetAdjacentLowerTile(TileMapList tileMaps, TileEdge edge, int currentLayer)
        {
            Vector2 currentTile = edge.tileCoords;
            Vector2 adjTile = currentTile;

            for (int i = currentLayer; i >= 0; i--)
            {
                Vector2 adjCoords = _GetAdjLowerCoords(edge, currentLayer, i);
                TileMap lowerTileMap = tileMaps[i];

                if (adjCoords != currentTile && lowerTileMap.GetCellv(adjCoords) != TileMap.InvalidCell)
                {
                    adjTile = adjCoords;
                    break; //no need to continue searching we have the highest adj tile
                }
            }
            return adjTile;
        }

        /// <summary>
        /// Given some input edge, grabs the tile it originated from and attempts to calculate coordinates
        /// for some adjacent tile on the observed layer (which should be lower than the current layer).
        /// </summary>
        /// <param name="edge">Edge that is checked for what tile it originated from.</param>
        /// <param name="currentLayer">Layer of edge.</param>
        /// <param name="observedLayer">Layer of the tile that this func calcs coords for.</param>
        /// <returns>Coordinates of some adjacent tile on the observed layer, even if it does not exist.</returns>
        private static Vector2 _GetAdjLowerCoords(TileEdge edge, int currentLayer, int observedLayer)
        {
            Vector2 currentTile = edge.tileCoords;
            int side = edge.tileSide;

            Vector2 belowTile = TileFuncs.GetTileAboveOrBelow(currentTile, currentLayer, observedLayer);
            Vector2 adjBelowTile = TileFuncs.GetTileAdjacent(belowTile, side);

            return adjBelowTile;
        }
    }
}