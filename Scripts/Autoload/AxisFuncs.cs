using System.Linq;
using SCol = System.Collections.Generic;
using Godot;
// ReSharper disable CommentTypo

namespace EFSMono.Scripts.Autoload
{
/// <summary>
/// Auto loaded script.
/// Contains easily accessible coordinate-system axis-related functions.
/// </summary>
public class AxisFuncs : Node
{
    /// <summary>
    /// Converts a Vector2 taken from the default canvas (which uses the cartesian axis) to the coordinates it would
    /// be if the canvas used the isometric axis instead.
    /// Note that this is NOT the same thing as TileMap.GetCellv which denotes tiles with coordinates such as (0, 0),
    /// (5, 5) etc.  This function would make an isometric tile (which looks like a diamond normally) look like a
    /// square.
    /// </summary>
    /// <param name="coordInCarte">Coordinate in canvas using default cartesian axis.  Note that I did not name this
    /// carteCoord or isoCoord to avoid confusion with TileMap.GetCellv.</param>
    /// <returns>Coordinate converted to isometric axis.</returns>
    public static Vector2 CoordToIsoAxis(Vector2 coordInCarte)
    {
        return new Vector2(coordInCarte.x + 2*coordInCarte.y, 2*coordInCarte.y - coordInCarte.x);
    }

    /// <summary>
    /// Converts a Vector2 which exists in a theoretical canvas using the isometric axis to the original cartesian axis
    /// used by default in the real canvas.
    /// </summary>
    /// <param name="coordInIso">Coordinate in canvas using the isometric axis.</param>
    /// <returns>Coordinate converted to cartesian axis.</returns>
    public static Vector2 CoordToCarteAxis(Vector2 coordInIso)
    {
        return new Vector2((coordInIso.x - coordInIso.y)/2, (coordInIso.x + coordInIso.y)/4);
    }

    /// <summary>
    /// Same as CoordToIsoAxis but for an IEnumberable of Vector2s, which is all converted to coordinates using the
    /// isometric axis.
    /// </summary>
    /// <param name="coordsInCarte">List of Vector2s in cartesian axis.</param>
    /// <returns>List of coordinates converted to isometric axis.</returns>
    public static SCol.List<Vector2> CoordArrayToIsoAxis(SCol.IEnumerable<Vector2> coordsInCarte)
    {
        return coordsInCarte.Select(CoordToIsoAxis).ToList();
    }

    /// <summary>
    /// Same as CoordToCarteAxis but for an IEnumberable of Vector2s, which is all converted to coordinates using the
    /// cartesian axis.
    /// </summary>
    /// <param name="coordsInCarte">List of Vector2s in isometric axis.</param>
    /// <returns>List of coordinates converted to cartesian axis.</returns>
    public static SCol.List<Vector2> CoordArrayToCarteAxis(SCol.IEnumerable<Vector2> coordsInCarte)
    {
        return coordsInCarte.Select(CoordToCarteAxis).ToList();       
    }
}
}