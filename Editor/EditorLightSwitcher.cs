using UnityEditor;
using UnityEngine;

namespace RuntimeLightmapController.LightmapEditor
{
    [CustomEditor(typeof(LightSwitcher))]
    public class EditorLightSwitcher : Editor
    {
        private bool _shouldDisplayError;
        private string _errorMessage;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LightSwitcher switcher = (LightSwitcher)target;

            if (_shouldDisplayError)
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Warning);

            if (GUILayout.Button("Scan For Issues In Stored LightStates"))
            {
                if (!switcher.ScanSavedLightmapsForIssues(out LightSwitcher.IssueReason reason))
                {
                    Debug.Log("No issues found!");
                    _shouldDisplayError = false;

                    return;
                }

                switch (reason)
                {
                    case LightSwitcher.IssueReason.DifferingLLightmaps:
                        _shouldDisplayError = true;
                        _errorMessage = "There are an unequal amount of lightmaps per light state";

                        Debug.LogError("There are a differing amount of lightmaps per light state, this is not allowed and should not happen during regular circumstances.\n" +
                                       "Try baking lightmaps again.\n" +
                                       "It is possible that this can occur and that I just have never seen it, if so: woops.");

                        break;

                    case LightSwitcher.IssueReason.DifferingDLightmaps:
                        _shouldDisplayError = true;
                        _errorMessage = "There is an unequal amount of dir lightmaps to light lightmaps";

                        Debug.LogError("There were a differing amount of dir lightmaps compared to light lightmaps in some light states, there should always be an equal amount of dir and light lightmaps in each state.\n" +
                                       "Try baking lightmaps again.\n" +
                                       "It is possible that this can occur when baking scenes and that I just have never seen it, if so: woops.");

                        break;
                }
            }
        }
    }
}
