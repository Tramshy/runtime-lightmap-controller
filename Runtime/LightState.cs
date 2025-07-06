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
        [Tooltip("This array can differ between states, but if the state has shadow masks the array has to be the same size as other lightmap arrays.\n
                  This means that if you get less shadow masks than other lightmaps after baking, you will have to fill in the remainder with any other textures (like one of the created shadow mask textures).\n
                  Just make sure the array index always match the index on the name of the shadow mask texture.")]
        [SerializeField] private Texture2D[] _shadowMask;
#endif

#if ENABLE_REFLECTION_PROBE
        [Tooltip("Make sure the index of this array matches the index in the name of the cubemap.\n" +
                 "If there is a gap in the indexes of the textures, simply recreate the gap in this array as well.")]
        [SerializeField] private Cubemap[] _reflectionProbeMaps;
#endif

        // Light probes for a state.
        [SerializeField] private SphericalHarmonicsL2[] _stateProbeData;

        public Texture2D[] LLightmaps { get => _lightmapLight; }
        public Texture2D[] DLightmaps { get => _lightmapDir; }

#if ENABLE_SHADOW_MASK
        public Texture2D[] ShadowMasks { get => _shadowMask; }
#endif

        public SphericalHarmonicsL2[] StateProbeData { get => _stateProbeData; }

#if ENABLE_REFLECTION_PROBE
        public Cubemap[] StateReflectionProbeData { get => _reflectionProbeMaps; }
#endif

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
