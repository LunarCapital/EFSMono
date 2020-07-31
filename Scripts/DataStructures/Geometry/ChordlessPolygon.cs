using System.Linq;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// A class that describes a rectangular irregular polygon that may or may not have holes, but
/// does not have any chords.
/// NO CHECKS are done to ensure that the polygon actually has no chords.
/// As always, closed loop conventions state that for some vector array[first] == array[last].
/// </summary>
public class RectangularPolygon
{
    public Vector2[] outerPerim { get; }
    public SCol.List<Vector2>[] holes { get; }

    public RectangularPolygon(Vector2[] outerPerim, SCol.List<Vector2>[] holes)
    {
        //TODO simplify both outerPerim and holes which i technically coded already in edgeCol and edges but AHHHHHHHHHHHH
        this.outerPerim = outerPerim;
        this.holes = holes;
    }
}
}