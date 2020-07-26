using Godot;

namespace EFSMono.Scripts.Autoload
{
/// <summary>
/// Auto loaded class.
/// Contains easily accessible sorting-related functions.
/// </summary>
public static class SortFuncs
{
    public static int SortByXThenYAscending(Vector2 a, Vector2 b)
    {
        if (a.x < b.x)
        {
            return -1;
        }
        else if (a.x > b.x)
        {
            return 1;
        }
        else
        { //a.x == b.x
            if (a.y < b.y)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
}
}