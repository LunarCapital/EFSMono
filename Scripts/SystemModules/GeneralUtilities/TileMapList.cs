using System;
using Godot;
using System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.GeneralUtilities
{
/// <summary>
/// A custom class designed to hold TileMaps in a world. Has three purposes:
///     1. Ensure self is sorted by Z Index in ASCENDING order at all times
///     2. Ensure self's LIST INDEX holds the TileMap with the matching Z INDEX
///     3. Ensure self's indexes are a sequence
/// If one of these conditions do not hold, an exception should be thrown as there
/// are several classes (such as LedgeBuilder and LedgeSuperimposer) rely on the
/// above conditions being true.
/// </summary>
public class TileMapList : SortedList<int, TileMap>
{

    public TileMapList(ICollection<TileMap> tileMaps) {
        foreach (TileMap tileMap in tileMaps) {
            this.Add(tileMap.ZIndex, tileMap);
        }

        try
        {
            this._CheckValidity();
        }
        catch (ZIndexMismatchException e)
        {
            GD.PrintS("Exception msg: " + e.Message);
        }
    }

    /// <summary>
    /// Return the last TileMap in this list.
    /// </summary>
    /// <returns>The last TileMap in this list.</returns>
    public TileMap Last()
    {
        int lastIndex = this.Count - 1;
        return this[lastIndex];
    }

    private void _CheckValidity()
    {
        int indexCheck = 0;
        foreach (int zKey in this.Keys)
        {
            if (indexCheck != zKey)
            {
                throw new ZIndexMismatchException("TileMapList indices are not a sequence. Missing index: " + indexCheck);
            }
            indexCheck++;
        }
    }

    [Serializable]
    private class ZIndexMismatchException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class's constructor attempts to initialise
        /// a sorted list of TileMaps but there is a missing Z index.
        /// </summary>
        public ZIndexMismatchException(string message) : base(message) {}

            public ZIndexMismatchException()
            {
            }

            public ZIndexMismatchException(string message, Exception innerException) : base(message, innerException)
            {
            }
    }
}
}