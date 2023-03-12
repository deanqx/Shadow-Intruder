using UnityEngine;
using UnityEditor;

namespace Terrain
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MapGenerator mapGen = (MapGenerator)target;

            if (DrawDefaultInspector())
            {
                if (mapGen.autoUpdate)
                {
                    mapGen.DrawMapInEditor();
                }
            }

            if (GUILayout.Button("Generate"))
            {
                mapGen.DrawMapInEditor();
            }
        }
    }

    [InitializeOnLoadAttribute]
    public static class PlayStateNotifier
    {
        static PlayStateNotifier()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                GameObject.Find("Map Generator").GetComponent<MapGenerator>().DrawMapInEditor();
            }
        }
    }
}
