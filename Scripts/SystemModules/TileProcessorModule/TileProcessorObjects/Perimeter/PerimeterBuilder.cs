using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.SystemModules.GeneralUtilities;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter
{
/// <summary>
/// A class that is dedicated to building perimeters based on tile placements.  The detailed version is in
/// PerimeterBuilderResult but a brief explanation here:
///     * Each TileMap represents a different height.
///     * Each TileMap can have tiles that don't touch each other.
///     * Tiles that do touch each other are placed in the same group which we dub a 'tile group'.
///     * Tile groups can have any number of holes which we define by 'hole group(s)'.
///     * Hole group == 0 is just the outside perimeter for the group of tiles.
///     * Hole group 1 and above are outside perimeters for each hole.
/// </summary>
public static class PerimeterBuilder
{

    /// <summary>
    /// Builds perimeters, packs into a dictionary of tilemaps -> List of EdgeCollections, and returns.
    /// </summary>
    public static SCol.Dictionary<TileMap, SCol.List<EdgeCollection>> BuildPerimeter(this TileMapList tileMaps)
    {
        var tileMapToAllEdgeCollections = new SCol.Dictionary<TileMap, SCol.List<EdgeCollection>>();

        foreach (TileMap tileMap in tileMaps.Values)
        {
            SCol.List<TilePerim> tilePerims = _FillTiles(tileMap);
            SCol.Dictionary<int, SCol.HashSet<int>> adjMatrix = _InitAdjMatrix(tilePerims);

            //recursive flood fill to find tile groups
            int maxColor = 0;
            foreach (TilePerim tilePerim in tilePerims.Where(tilePerim => tilePerim.color == TilePerim.UNCOLORED))
            {
                tilePerims = _TileFloodFill(adjMatrix, tilePerims, tilePerim, maxColor);
                maxColor++; //i could put this on the prev line 
            }
            SCol.List<EdgeCollection> allEdgeCollections = _FillEdgeCollections(tilePerims, maxColor);
            tileMapToAllEdgeCollections.Add(tileMap, allEdgeCollections);
        }
        return tileMapToAllEdgeCollections;
    }

    /// <summary>
    /// Fills list of TilePerims of tiles in a TileMap.
    /// </summary>
    /// <param name="tileMap">The TileMap which we are grabbing tiles from</param>
    /// <returns>A list containing the TilePerims in the input TileMap</returns>
    private static SCol.List<TilePerim> _FillTiles(TileMap tileMap)
    {
        var tilePerims = new SCol.List<TilePerim>();
        var usedCells = new SCol.List<Vector2>(tileMap.GetUsedCells().OfType<Vector2>()); //thanks to godot's C# api array being non-typed i have to commit this atrocity

        for (int i = 0; i < usedCells.Count; i++)
        {
            Vector2 cell = usedCells[i];
            Vector2 botCoord = tileMap.MapToWorld(cell); //Thanks to graphics in general using reverse Y axis, the bottom coordinate actually has the HIGHEST y-value
            int cellID = tileMap.GetCellv(cell);
            Vector2 originCoord = botCoord - (new Vector2((float)Globals.TILE_WIDTH/2, 0)) + tileMap.TileSet.TileGetTextureOffset(cellID);
            NavigationPolygon navPoly = tileMap.TileSet.TileGetNavigationPolygon(cellID);

            Vector2[] vertices = _ShiftVertices(navPoly.Vertices, originCoord);
            var tilePerim = new TilePerim(vertices, cell, i);
            tilePerims.Add(tilePerim);
        }
        return tilePerims;
    }

    /// <summary>
    /// Shifts vertices within some array of vertices by the originCoord vector.
    /// .-----
    /// | /\ |
    /// |/  \|
    /// |\  /|
    /// | \/ |
    /// ------
    /// Above img is a tile's NavPoly in isolation. The '.' is the origin and its vertices are relative to that point. This function adds the NavPoly's
    /// vertices to the 'real' coordinate of where the tile's origin should be to obtain its 'real' vertex coordinates.
    /// </summary>
    /// <param name="origVertices">NavPoly vertex coordinates.</param>
    /// <param name="originCoord">'Real' coordinate of tile's origin</param>
    /// <returns>Array of NavPoly (or original) vertex coords which have been shifted by the originCoord.</returns>
    private static Vector2[] _ShiftVertices(SCol.IReadOnlyList<Vector2> origVertices, Vector2 originCoord)
    {
        var shiftedVertices = new Vector2[origVertices.Count];
        for (int i = 0; i < origVertices.Count; i++)
        {
            Vector2 origVertex = origVertices[i];
            shiftedVertices[i] = originCoord + origVertex;
        }
        return shiftedVertices;
    }

    /// <summary>
    /// Initialises an adjacency matrix of tiles to each other.
    /// </summary>
    /// <param name="tilePerims">List of TilePerims used to form the matrix.</param>
    /// <returns>A dictionary mapping a TilePerim's ID to a hashset of TilePerim's ID that represents an adj matrix.</returns>
    private static SCol.Dictionary<int, SCol.HashSet<int>> _InitAdjMatrix(SCol.IReadOnlyCollection<TilePerim> tilePerims)
    {
        var adjMatrix = new SCol.Dictionary<int, SCol.HashSet<int>>();
        foreach (TilePerim tilePerimA in tilePerims)
        {
            adjMatrix.Add(tilePerimA.id, new SCol.HashSet<int>());
            foreach (TilePerim tilePerimB in tilePerims)
            {
                if (tilePerimA.IsTileAdjacent(tilePerimB) && tilePerimA != tilePerimB)
                {
                    adjMatrix[tilePerimA.id].Add(tilePerimB.id);
                }
            }
        }

        return adjMatrix;
    }

    /// <summary>
    /// Flood files TilePerims via DFS, coloring them, in order to find how they are grouped up.
    /// </summary>
    /// <param name="adjMatrix">Adjacency matrix of TilePerims.</param>
    /// <param name="tilePerims"></param>
    /// <param name="thisTile">The tile this function is currently 'looking' at (AKA coloring and checking neighbours)</param>
    /// <param name="color">Color that we paint tiles. Actually an integer.</param>
    /// <returns>The same TilePerim but with changed color. I chose not to modify the TilePerim within the func due to the immutability principle.</returns>
    private static SCol.List<TilePerim> _TileFloodFill(SCol.IReadOnlyDictionary<int, SCol.HashSet<int>> adjMatrix,
                                                       SCol.IEnumerable<TilePerim> tilePerims, TilePerim thisTile, int color)
    {
        var tilePerimsClone = new SCol.List<TilePerim>(tilePerims);
        var visited = new SCol.HashSet<int>();
        var stack = new SCol.Stack<TilePerim>();
        stack.Push(thisTile);
        visited.Add(thisTile.id);

        while (stack.Any())
        {
            TilePerim examinee = stack.Pop();
            foreach (int neighbourID in adjMatrix[examinee.id].Where(x => !visited.Contains(x)))
            {
                TilePerim neighbour = tilePerimsClone.Find(x => x.id == neighbourID);
                stack.Push(neighbour);
                visited.Add(neighbourID);
            }
            examinee.color = color;
            tilePerimsClone[tilePerimsClone.FindIndex(0, x => x.id == examinee.id)] = examinee;
        }
        return tilePerimsClone;
    }

    /// <summary>
    /// Takes the tile perims inside a list and groups them according to their flood fill color.
    /// From these groupings, edges are extracted and placed inside an EdgeCollection IFF they are NOT
    /// intersections. Edges that are intersections appear more than once.
    /// </summary>
    /// <param name="tilePerims">List of tile perims.</param>
    /// <param name="maxColor">Max color that a tile can be.</param>
    /// <returns>A list of EdgeCollections, each containing EDGES from tiles of the same color (group).</returns>
    private static SCol.List<EdgeCollection> _FillEdgeCollections(SCol.IReadOnlyCollection<TilePerim> tilePerims, int maxColor)
    {
        var allEdgeCollections = new SCol.List<EdgeCollection>();
        for (int color = 0; color < maxColor; color++)
        {
            var perimeter = new SCol.List<Edge>(); //contains edges on the OUTSIDE of tile group
            var intersections = new SCol.List<Edge>(); //contains inner edges of tile group

            foreach (TilePerim tilePerim in tilePerims.Where(x => x.color == color))
            { //only check tilePerims of matching color
                foreach (Edge edge in tilePerim.GetEdgesArray().Where(x => _ContainsEdge(intersections, x) == -1))
                { //ignore intersections
                    int identicalID = _ContainsEdge(perimeter, edge);
                    if (identicalID != -1)
                    { //perimeter already contains edge, .'. it is actually an intersection
                        perimeter.RemoveAt(identicalID);
                        intersections.Add(edge);
                    }
                    else
                    {
                        perimeter.Add(edge);
                    }
                }
            }
            allEdgeCollections.Add(new EdgeCollection(perimeter));
        }
        return allEdgeCollections;
    }

    /// <summary>
    /// Checks if the input collection contains the edge. Required because the Edge class is more than just Points A and B (it also
    /// includes the tile it originated from and tile side) but we only care about those two properties.
    /// </summary>
    /// <param name="collection">Collection being checked for whether it contains Edge (but just the points).</param>
    /// <param name="edge">Edge being checked.</param>
    /// <returns>Index of identical edge in collection with matching points to the input edge, or -1 if no match found.</returns>
    private static int _ContainsEdge(SCol.IReadOnlyList<Edge> collection, Edge edge)
    {
        for (int i = 0; i < collection.Count; i++)
        {
            Edge examinee = collection[i];
            if (examinee.IsIdentical(edge))
            {
                return i;
            }
        }
        return -1;
    }

}
}