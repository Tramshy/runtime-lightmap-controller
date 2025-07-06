using UnityEditor;
using UnityEngine;

namespace RuntimeLightmapController.LightmapEditor
{
    [CustomEditor(typeof(LightBoundDefiner))]
    public class EditorLightBoundsDefiner : Editor
    {
        public override void OnInspectorGUI()
        {
            LightBoundDefiner bounds = (LightBoundDefiner)target;

            DrawDefaultInspector();

            GUILayout.Label("More efficient, but only finds objects with colliders");
            if (GUILayout.Button("Get Static Renderers With Colliders"))
                bounds.GetStaticRenderers();

            GUILayout.Label("Less efficient, but finds all objects within bounds");
            if (GUILayout.Button("Get Static Renderers Without Colliders"))
                bounds.GetStaticRenderersWithoutCol();

            GUILayout.Space(20);

            if (GUILayout.Button("Get Probes Within Bounds"))
                bounds.GetProbesWithinBounds();

#if ENABLE_REFLECTION_PROBE
            GUILayout.Space(20);

            if (GUILayout.Button("Get Reflection Probes Within Bounds"))
                bounds.GetReflectionProbesWithinBounds();
#endif
        }
    }
}
