using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors.PolygonPartitioning;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors
{
/// <summary>
/// A class that constructs floor shapes using ConvexPolygonShape2Ds for Area2Ds by decomposing the original irregular
/// orthogonal polygon (with holes) that describes some tileGroup into the minimal set of rectangles.
/// This is actually a fairly long process so here's the details:
/// 
///     1. Take the original irregular, orthogonal polygon with holes (and possibly chords).
/// 
///     2. Decompose it into irregular, orthogonal polygons WITHOUT chords (although it may still have holes) by:
/// 
///         2.1 Separating vertical and horizontal chords into a bipartite graph which connect based off intersections.
///         2.2 Deriving the Maximum Matching of intersections, denoted as MM, via an algorithm such as Ford-Fulkerson.
///         2.3 Using MM to derive the Maximum Vertex Cover, denoted as MVC, by:
/// 
///             2.3.1 Taking the left-side nodes EXCLUDED by the MM.
///             2.3.2 Running DFS on these nodes to find all the nodes they 'cover'.
///             2.3.3 Taking the right-side visited and the left-side UNVISITED of the DFS 'cover', which is the MVC.
/// 
///         2.4 Using the MVC to derive the Maximum Independent Set, denoted as MIS.
///         2.5 'Drawing' the chords in the MIS, which splits the original polygon into multiple chord-less polygons.
///         2.6 Extracting these chord-less polygons by:
/// 
///             2.6.1 Constructing a graph out of the 'drawing', with vertices as nodes and lines/chords as edges.
///             2.6.2 Finding 'Node Covers' by running DFS on the graph until all nodes are visited.
///             2.6.3 Constructing a 'tree' of Node Covers where a parent cover contains child covers, and covers that
///                   do not contain each other are siblings.
///             2.6.4 Finding the 'Outer Perimeter' of each Node Cover by:
/// 
///                 2.6.4.1 Starting from the node with the smallest X and Y position.
///                 2.6.4.2 Defining a bearing as being in the negative Y direction (as there are no nodes in that direction).
///                 2.6.4.3 Picking the next node out of available neighbours by choosing the one with the last positive.
///                         CCW angle change from the bearing.
///                 2.6.4.4 Making the new bearing = direction from new node TO old node.
///                 2.6.4.5 Repeating until the start node is reached.
/// 
///             2.6.5 Finding the 'Minimum Cycles' of each Node Cover using BFS.
///             2.6.6 Checking which of a Node Cover's Minimum Cycle contains the Outer Perimeter of its children in the
///                   Tree of Node Covers.
///             2.6.7 Denoting each Minimum Cycle as being a chord-less polygon IF it is not a hole, and denoting the
///                   Outer Perimeter of any Node Covers contained within a Minimum Cycle as its hole(s) (it's still
///                   a chord-less polygon, just with a hole).
/// 
///     3. Iterate over all of the new chord-less polygons and stash the ones that are already rectangles.
///     4. For the rest, which are irregular, decompose them into rectangles by:
///
///         4.1 Extending all of their concave vertices in ANY direction until said extension reaches a line.
///         4.2 Extracting these new rectangles by:
///
///             4.2.1 Constructing a graph with vertices as nodes, lines and extensions as edges, and the intersection
///                   of extensions and lines as new vertex nodes if they do not already exist as one.
///             4.2.2 Finding the Minimum Cycles of the graph, with each Minimum Cycle being a rectangle.
///
///    5. Done.
///
/// Credit to: https://nanoexplanations.wordpress.com/2011/12/02/polygon-rectangulation-part-1-minimum-number-of-rectangles/
/// for part of the explanation, mainly about conversion of polygons-with-chords to chord-less-polygons via the MIS.
/// Extraction of both rectangles and chord-less-polygons via Minimum Cycles and Outer Perimeters and Node Covers and
/// whatever else is my own solution and thus may be sub-optimal and/or unnecessarily long/over-engineered.  
/// </summary>
public static class FloorConstructor
{

    /// <summary>
    /// Constructs a dictionary that maps an Area2D RID to all of the ConvexPolygonShape2Ds that it will be made up of.
    /// Shapes are obtained by decomposing the original, irregular, orthogonal polygon (with holes) into the minimal set
    /// of rectangles (which are ConvexPolygonShape2Ds and not Rectangle2Ds because they are on the isometric axis).
    /// </summary>
    /// <param name="tileMaps">List of all TileMaps.</param>
    /// <param name="tileMapToFloorArea2Ds">Map of TileMaps to their respective Area2D RID.</param>
    /// <param name="perimData">Data of all perimeters of all TileMaps.</param>
    /// <returns></returns>
    public static SCol.Dictionary<RID, SCol.List<ConvexPolygonShape2D>> 
        ConstructFloorPartitions(this TileMapList tileMaps, SCol.Dictionary<TileMap, RID> tileMapToFloorArea2Ds,
                                 PerimeterData perimData)
    {
        var floorArea2DToPolygons = new SCol.Dictionary<RID, SCol.List<ConvexPolygonShape2D>>();

        foreach (TileMap tileMap in tileMaps.Values)
        {
            RID area2dRID = tileMapToFloorArea2Ds[tileMap];
            int maxTileGroups = perimData.GetMaxTileGroup(tileMap);
            for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
            {
                int maxHoleGroups = perimData.GetMaxHoleGroup(tileMap, tileGroup);
                var allPerims = new SCol.List<Vector2>[maxHoleGroups];
                for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                { //put all perims (outer and hole) from one tile group into a single array of lists (gods help me) for partitioning
                    EdgeCollection<TileEdge> edgeColl = perimData.GetEdgeCollection(tileMap, tileGroup, holeGroup);
                    allPerims[holeGroup] = new SCol.List<Vector2>(edgeColl.GetSimplifiedPerim());
                }
                SCol.List<ConvexPolygonShape2D> partitionedRectangles = _PartitionPolygonToRectangles(allPerims);
                foreach (ConvexPolygonShape2D shape in partitionedRectangles)
                {
                    Physics2DServer.AreaAddShape(area2dRID, shape.GetRid());
                }
                floorArea2DToPolygons[area2dRID] = partitionedRectangles;
            }
        }
        return floorArea2DToPolygons;
    }

    /// <summary>
    /// Given an input of perimeters that describe a single tile group, returns a list of ConvexPolygonShape2Ds that
    /// represent the tile group but decomposed into rectangles.
    /// </summary>
    /// <param name="allPerims">Array of lists describing single tile group. Every list in the array represents a
    /// different perimeter.</param>
    /// <returns>A list of ConvexPolygonShape2Ds, with each one being a rectangle that the original irregular polygon
    /// was decomposed into.</returns>
    private static SCol.List<ConvexPolygonShape2D> _PartitionPolygonToRectangles(
        SCol.IReadOnlyList<SCol.List<Vector2>> allPerims)
    {
        var partitionedRectangles = new SCol.List<ConvexPolygonShape2D>();

        var allIsoPerims = new SCol.List<Vector2>[allPerims.Count];
        for (int i = 0; i < allPerims.Count; i++)
        {
            //convert to iso axis so all the shapes 'look' rectangular
            allIsoPerims[i] = AxisFuncs.CoordArrayToIsoAxis(allPerims[i]);
        }

        SCol.List<SCol.List<Vector2>> allRectangles = allIsoPerims.DecomposeComplexPolygonToRectangles();

        foreach (SCol.List<Vector2> rectangle in allRectangles)
        {
            SCol.List<Vector2> carteRectangle = AxisFuncs.CoordArrayToCarteAxis(rectangle);
            var cps2d = new ConvexPolygonShape2D()
            {
                Points = carteRectangle.ToArray()
            };
            partitionedRectangles.Add(cps2d);
        }

        return partitionedRectangles;
    }
}
}