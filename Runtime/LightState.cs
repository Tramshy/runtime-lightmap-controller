using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeLightmapController
{
    [CreateAssetMenu(fileName = "New Light State", menuName = "Runtime Lightmap Controller/Light State", order = 1)]
    public class LightState : ScriptableObject
    {
        [SerializeField] private Texture2D[] _lightmapLight, _lightmapDir;

#if ENABLE_SHADOW_MASK
        [Tooltip("This array can differ between states, but if the state has shadow masks the array has to be the same size as other lightmap arrays.")]
        [SerializeField] private Texture2D[] _shadowMask;
#endif

        [SerializeField] private SphericalHarmonicsL2[] _stateProbeData;

        public Texture2D[] LLightmaps { get => _lightmapLight; }
        public Texture2D[] DLightmaps { get => _lightmapDir; }

#if ENABLE_SHADOW_MASK
        public Texture2D[] ShadowMasks { get => _shadowMask; }
#endif

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
