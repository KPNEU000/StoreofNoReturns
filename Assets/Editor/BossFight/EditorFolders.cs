using System.IO;
using UnityEditor;

namespace BossFight.Editor
{
    public static class EditorFolders
    {
        // Creates an Assets/... folder (and parents) if missing.
        public static void Ensure(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
