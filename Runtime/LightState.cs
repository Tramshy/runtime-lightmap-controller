using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeLightmapController
{
    [CreateAssetMenu(fileName = "New Light State", menuName = "Runtime Lightmap Switcher/Light State", order = 1)]
    public class LightState : ScriptableObject
    {
        [SerializeField] private Texture2D[] _lightmapLight, _lightmapDir;

        [SerializeField] private SphericalHarmonicsL2[] _stateProbeData;

        public Texture2D[] LLightmaps { get => _lightmapLight; }
        public Texture2D[] DLightmaps { get => _lightmapDir; }

        public SphericalHarmonicsL2[] StateProbeData { get => _stateProbeData; }

#if UNITY_EDITOR
        public void StoreCurrentProbeData()
        {
            _stateProbeData = LightmapSettings.lightProbes.bakedProbes;

            Undo.RecordObject(this, "Set Baked Light Probes to State");
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
