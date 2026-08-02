using UnityEditor;
using UnityEngine;

namespace CardProject.Editor
{
    public static class MapStitcherMenu
    {
        [MenuItem("Tools/地图拼接工具 %&M")]
        public static void OpenMapStitcher()
        {
            Application.OpenURL("http://127.0.0.1:7860");
        }
    }
}
