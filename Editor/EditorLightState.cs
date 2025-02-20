using UnityEditor;
using UnityEngine;

namespace RuntimeLightmapController.LightmapEditor
{
    [CustomEditor(typeof(LightState))]
    public class EditorLightState : Editor
    {
        public override void OnInspectorGUI()
        {
            LightState state = (LightState)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Store Current Baked Light Probes"))
                state.StoreCurrentProbeData();
        }
    }
}
