using Godot;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// This is technically bad, but I don't really want to be writing edge simplification all over again and if I have to write
/// some shitty interface that nobody will ever see to get around this problem then hell i'm gonna do it
///
/// I might clean this up one day but not understanding why i can't override a base class's function and return a different
/// type wasted several hours of time and i really just want to be working on something else as soon as possible
/// </summary>
public interface IEdge
{
    Vector2 a { get; }
    Vector2 b { get; }
    bool isChecked { get; set; }

    int GetHashCode();
    IEdge GetReverseEdge();
    bool IsIdentical(IEdge other);
}
}