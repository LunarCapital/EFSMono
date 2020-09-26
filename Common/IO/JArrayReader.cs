using System.IO;
using Godot;
using Newtonsoft.Json.Linq;

namespace EFSMono.Common.IO
{
    /// <summary>
    /// A static class that obtains a JArray from some input json.
    /// </summary>
    public static class JArrayReader
    {
        public static JArray ReadJArray(string jsonPath)
        {
            var jsonStreamReader = new StreamReader(ProjectSettings.GlobalizePath(jsonPath));
            string fullJSON = jsonStreamReader.ReadToEnd();
            var jArray = JArray.Parse(fullJSON);
            jsonStreamReader.Close();

            return jArray;
        }

    }
}
