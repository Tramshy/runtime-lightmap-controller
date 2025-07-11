using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeLightmapController
{
    public class LightSwitcher : MonoBehaviour
    {
        public static LightSwitcher Instance;

        [SerializeField] private LightState[] _data;
        public LightState[] States { get => _data; }

        private LightmapData[] _currentLightmaps;

        [SerializeField] private LightBoundDefiner[] _sceneBounds;

        public SphericalHarmonicsL2[] CurrentBakedProbes { get; private set; }

        public Vector2Int LightmapSize { get; private set; }

        public int LightStates { get => _data.Length - 1; }
        public int LightmapPerState { get => _data[0].LLightmaps.Length; }

#if ENABLE_LIGHTMAP_LERP
        private List<int> _temporaryLightmapToRemove = new List<int>();

#if ENABLE_SHADOW_MASK
        public Texture2D ShadowMaskReplacement { get; private set; }
#endif

        #region Shader properties

        // ID's for properties found in the compute shaders.
        public int LightmapLight1 { get; } = Shader.PropertyToID("_LLightmap1");
        public int LightmapLight2 { get; } = Shader.PropertyToID("_LLightmap2");
        public int LightmapDir1 { get; } = Shader.PropertyToID("_DLightmap1");
        public int LightmapDir2 { get; } = Shader.PropertyToID("_DLightmap2");
        public int ShadowMask1 { get; } = Shader.PropertyToID("_ShadowMask1");
        public int ShadowMask2 { get; } = Shader.PropertyToID("_ShadowMask2");
        public int LightmapFloat2Size { get; } = Shader.PropertyToID("_LightmapSize");
        public int MapLerpFactor { get; } = Shader.PropertyToID("_LerpFactor");
        public int LightResult { get; } = Shader.PropertyToID("_ResultLight");
        public int DirResult { get; } = Shader.PropertyToID("_ResultDir");
        public int ShadowMaskResult { get; } = Shader.PropertyToID("_ResultShadowMask");
        public int UseShadowMask { get; } = Shader.PropertyToID("_UseShadowMask");

        public int SH1 { get; } = Shader.PropertyToID("_SH1");
        public int SH2 { get; } = Shader.PropertyToID("_SH2");
        public int SHResult { get; } = Shader.PropertyToID("_Result");
        public int SHLerpFactor { get; } = Shader.PropertyToID("_LerpFactor");

        #endregion
#endif

        private void Awake()
        {
            // Singleton
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(this);
            }

            LightmapSize = new Vector2Int(_data[0].LLightmaps[0].width, _data[0].LLightmaps[0].height);

            List<int> indexes = new List<int>(), toRemove = new List<int>();
            int index = 0;

            // Make sure that light probes can only be owned by one light bound.
            for (int i = 0; i < _sceneBounds.Length; i++)
            {
                for (int j = 0; j < _sceneBounds[i].lightProbeIndexes.Length; j++)
                {
                    index = _sceneBounds[i].lightProbeIndexes[j];

                    if (indexes.Contains(index))
                        toRemove.Add(index);
                    else
                    {
                        indexes.Add(index);
                    }
                }

                var boundsIndexes = _sceneBounds[i].lightProbeIndexes.ToList();

                for (int j = 0; j < toRemove.Count; j++)
                {
                    boundsIndexes.Remove(toRemove[j]);
                }

                _sceneBounds[i].lightProbeIndexes = boundsIndexes.ToArray();
            }
        }

        private void Start()
        {
            SetupLightmaps();

            _currentLightmaps = LightmapSettings.lightmaps;

#if ENABLE_SHADOW_MASK && ENABLE_LIGHTMAP_LERP
            // Set up extra textures needed for shadow mask lerping.
            ShadowMaskReplacement = new Texture2D(LightmapSize.x, LightmapSize.y, TextureFormat.RGBAHalf, false);
            var pixels = new Color32[LightmapSize.x * LightmapSize.y];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }

            ShadowMaskReplacement.SetPixels32(pixels);
            ShadowMaskReplacement.Apply();
#endif
        }

        private void SetupLightmaps()
        {
            // Each scene should have a consistent amount of lightmaps,
            // this means that if the first element of the light data has 2 light lightmaps then all different light states should always have the same amount of light lightmaps.
            int indexLength = _data[0].LLightmaps.Length;
            List<LightmapData> lightmaps = new List<LightmapData>();

            for (int i = 0; i <= LightStates; i++)
            {
                for (int j = 0; j < indexLength; j++)
                {
                    var lightData = new LightmapData()
                    {
                        lightmapColor = _data[i].LLightmaps[j],
                        lightmapDir = _data[i].DLightmaps[j]
                    };

#if ENABLE_SHADOW_MASK
                    if (_data[i].ShadowMasks.Length > 0)
                        lightData.shadowMask = _data[i].ShadowMasks[j];
#endif

                    lightmaps.Add(lightData);
                }
            }

            CurrentBakedProbes = new SphericalHarmonicsL2[_data[0].StateProbeData.Length];

            for (int i = 0; i < CurrentBakedProbes.Length; i++)
            {
                CurrentBakedProbes[i] = _data[0].StateProbeData[i];
            }

            // It would be better to just set the array directly instead of calling this method, but that just doesn't work so this is the best solution.
            SwitchCurrentBakedProbeData(0, Enumerable.Range(0, LightmapSettings.lightProbes.bakedProbes.Length).ToArray());
            LightmapSettings.lightmaps = lightmaps.ToArray();
        }

        internal void SwitchCurrentBakedProbeData(int lightStateIndex, int[] indexes)
        {
            SphericalHarmonicsL2[] data = _data[lightStateIndex].StateProbeData;

            // For some reason using a for loop here causes light probes to sometimes get confused and not update for a while.
            foreach (int i in indexes)
            {
                if (data.Length <= i || CurrentBakedProbes.Length <= i)
                {
                    Debug.LogError("Indexes given for switching current baked probe data is out of bounds");

                    continue;
                }

                CurrentBakedProbes[i] = data[i];
            }

            LightmapSettings.lightProbes.bakedProbes = CurrentBakedProbes;
        }

        public void SwitchAllSceneBounds(int stateToSwitch)
        {
            for (int i = 0; i < _sceneBounds.Length; i++)
            {
                _sceneBounds[i].SwitchLightState(stateToSwitch);
            }
        }

        public void SwitchAllSceneBoundsToNextState()
        {
            for (int i = 0; i < _sceneBounds.Length; i++)
            {
                _sceneBounds[i].SwitchLightStateToNext();
            }
        }

#if ENABLE_LIGHTMAP_LERP
        public void SwitchAllSceneBoundsSmooth(int stateToSwitch, int framesBetweenChecks, float timeToBlend)
        {
            for (int i = 0; i < _sceneBounds.Length; i++)
            {
                _sceneBounds[i].SwitchLightStateSmooth(stateToSwitch, framesBetweenChecks, timeToBlend);
            }
        }

        public void SwitchAllSceneBoundsSmoothToNext(int framesBetweenChecks, float timeToBlend)
        {
            for (int i = 0; i < _sceneBounds.Length; i++)
            {
                _sceneBounds[i].SwitchLightStateSmoothToNext(framesBetweenChecks, timeToBlend);
            }
        }

        /// <summary>
        /// Lerps light probe data.
        /// </summary>
        internal void SwitchCurrentBakedProbeDataSmoothly(SphericalHarmonicsL2[] newData, int[] indexes)
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                if (CurrentBakedProbes.Length <= i)
                {
                    Debug.LogError("Indexes given for switching current baked probe data is out of bounds");

                    continue;
                }

                CurrentBakedProbes[indexes[i]] = newData[i];
            }

            LightmapSettings.lightProbes.bakedProbes = CurrentBakedProbes;
        }

        /// <summary>
        /// Adds new light maps slots that can be used to store the currently lerping light maps.
        /// </summary>
        /// <param name="stateForInitialCopy">The light state to copy when adding new light map slots</param>
        /// <returns></returns>
        internal int[] AddNewTemporaryLightmapSlots(int stateForInitialCopy)
        {
            if (stateForInitialCopy < 0 || stateForInitialCopy > LightStates)
                throw new System.Exception("State given for copy to temporary lightmap slot is not valid.");

            List<LightmapData> lightmaps = LightmapSettings.lightmaps.ToList();

            int from = LightmapSettings.lightmaps.Length;

            for (int i = 0; i < _data[stateForInitialCopy].LLightmaps.Length; i++)
            {
                lightmaps.Add(new LightmapData()
                {
                    lightmapColor = _data[stateForInitialCopy].LLightmaps[i],
                    lightmapDir = _data[stateForInitialCopy].DLightmaps[i]
                });
            }

            LightmapSettings.lightmaps = lightmaps.ToArray();
            int to = LightmapSettings.lightmaps.Length;

            return Enumerable.Range(from, to - from).ToArray();
        }

        internal void RemoveTemporaryLightmapSlots(int[] toRemove)
        {
            foreach (int i in toRemove)
            {
                _temporaryLightmapToRemove.Add(i);
            }

            if (toRemove[toRemove.Length - 1] != LightmapSettings.lightmaps.Length - 1)
                return;

            List<LightmapData> lightmaps = LightmapSettings.lightmaps.ToList(), removedData = new List<LightmapData>();

            foreach (int i in _temporaryLightmapToRemove)
            {
                removedData.Add(lightmaps[i]);
            }

            removedData.ForEach((data) => lightmaps.Remove(data));

            _temporaryLightmapToRemove.Clear();
            LightmapSettings.lightmaps = lightmaps.ToArray();
        }

        internal void SetLightmaps(int[] indexes, LightmapData[] data)
        {
            if (!LightmapSettings.lightmaps.SequenceEqual(data))
                _currentLightmaps = LightmapSettings.lightmaps;

            for (int i = 0; i < indexes.Length; i++)
            {
                _currentLightmaps[indexes[i]] = data[indexes[i]];
            }

            LightmapSettings.lightmaps = _currentLightmaps;
        }
#endif

#if UNITY_EDITOR
        public enum IssueReason { DifferingLLightmaps, DifferingDLightmaps, NoIssue }

        /// <param name="reason">Reason for issue, if any</param>
        /// <returns>Returns true if issue is found</returns>
        public bool ScanSavedLightmapsForIssues(out IssueReason reason)
        {
            reason = IssueReason.NoIssue;

            var expectedLLightmaps = _data[0].LLightmaps.Length;

            foreach (var state in _data)
            {
                if (state.LLightmaps.Length != expectedLLightmaps)
                {
                    reason = IssueReason.DifferingLLightmaps;

                    return true;
                }

                if (state.DLightmaps.Length != expectedLLightmaps)
                {
                    reason = IssueReason.DifferingDLightmaps;

                    return true;
                }
            }

            return false;
        }
#endif
    }
}
