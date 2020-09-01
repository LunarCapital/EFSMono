using Godot;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.DataKeys
{
    /// <summary>
    /// A class used as a key for the Data class dictionaries.
    /// This class specifically is used for mapping a TileMap to its number of tile groups.
    /// </summary>
    public class TileGroupKey
    {
        private readonly TileMap _tileMap;

        public TileGroupKey(TileMap tileMap)
        {
            this._tileMap = tileMap;
        }

        public override int GetHashCode()
        {
            return this._tileMap.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return this._Equals(obj as TileGroupKey);
        }

        private bool _Equals(TileGroupKey obj)
        {
            return this._tileMap == obj._tileMap;
        }
    }
}