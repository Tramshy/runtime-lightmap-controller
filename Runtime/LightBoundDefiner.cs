using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeLightmapController
{
    // Was gonna comment this, but I'm tired.
    // Good luck.
    public class LightBoundDefiner : MonoBehaviour
    {
        public int CurrentLightState { get; private set; } = 0;

        [SerializeField] private int[] _lightProbeIndexes;

        [SerializeField] private bool _shouldWarnAboutStaticNonuseOfLightmap = true, _shouldDisplayWireFrame = true;
#if ENABLE_LIGHTMAP_LERP
        [Tooltip("Turn this off to save a little memory and CPU usage that would otherwise be used on start to prepare for smooth lightmap changes.")]
        [SerializeField] private bool _willUseSmoothLightTransition = true;
#endif
        [SerializeField] private RendererData[] _staticRenderers;

#if ENABLE_LIGHTMAP_LERP
        private SphericalHarmonicsL2[] _probes;

        private ComputeShader _lightmapBlender, _shBlender;
        private ComputeBuffer _sh1, _sh2, _outSH;

        private RenderTexture _lRenderOut, _dRenderOut;
        private Texture2D[] _smoothChangeTextures, _midBlendedLightmaps;

        private RenderTexture _sRenderOut;
#endif

        private LightSwitcher _switcher;

#if ENABLE_LIGHTMAP_LERP
        private CancellationTokenSource _cancelSource = new CancellationTokenSource(), _stopSource = new CancellationTokenSource();

        private int[] _lightSmoothArrayIndexes;

        // Shader Properties
        private int _lLightmap1, _lLightmap2, _dLightmap1, _dLightmap2, _shadowMask1, _shadowMask2, _lightmapSize, _lerpFactor, _lResult, _dResult, _sResult, _shouldUseShadowMask;
        private int _firstSH, _secondSH, _shResult, _shLerpFactor;
#endif

        [Serializable]
        private struct RendererData
        {
            public Renderer ThisRenderer;

            public int StartIndex;

            public RendererData(Renderer renderer)
            {
                ThisRenderer = renderer;
                StartIndex = renderer.lightmapIndex;
            }
        }

#if ENABLE_LIGHTMAP_LERP
        private void Awake()
        {
            if (!_willUseSmoothLightTransition)
                return;

            _lightmapBlender = Resources.Load<ComputeShader>(@"LightmapSmoothing");

            if (_lightProbeIndexes.Length == 0)
                return;

            _shBlender = Resources.Load<ComputeShader>(@"SHSmoothing");

            int shCoeffCount = 9;
            int totalSize = _lightProbeIndexes.Length * shCoeffCount;
            int stride = sizeof(float) * 3;

            _sh1 = new ComputeBuffer(totalSize, stride);
            _sh2 = new ComputeBuffer(totalSize, stride);
            _outSH = new ComputeBuffer(totalSize, stride);
        }
#endif

        private void Start()
        {
            _switcher = LightSwitcher.Instance;

#if ENABLE_LIGHTMAP_LERP
            if (!_willUseSmoothLightTransition)
                return;

            _lLightmap1 = _switcher.LightmapLight1;
            _lLightmap2 = _switcher.LightmapLight2;
            _dLightmap1 = _switcher.LightmapDir1;
            _dLightmap2 = _switcher.LightmapDir2;
            _shadowMask1 = _switcher.ShadowMask1;
            _shadowMask2 = _switcher.ShadowMask2;
            _lightmapSize = _switcher.LightmapFloat2Size;
            _lerpFactor = _switcher.MapLerpFactor;
            _lResult = _switcher.LightResult;
            _dResult = _switcher.DirResult;
            _sResult = _switcher.ShadowMaskResult;
            _shouldUseShadowMask = _switcher.UseShadowMask;

#if ENABLE_SHADOW_MASK
            _smoothChangeTextures = new Texture2D[_switcher.LightmapPerState * 3];
            _midBlendedLightmaps = new Texture2D[_switcher.LightmapPerState * 3];
#elif !ENABLE_SHADOW_MASK
            _smoothChangeTextures = new Texture2D[_switcher.LightmapPerState * 2];
            _midBlendedLightmaps = new Texture2D[_switcher.LightmapPerState * 2];
#endif

            int width = _switcher.LightmapSize.x, height = _switcher.LightmapSize.y;

            _lRenderOut = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
            _dRenderOut = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { enableRandomWrite = true };
            _sRenderOut = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { enableRandomWrite = true };

            for (int i = 0; i < _switcher.LightmapPerState; i++)
            {
                _smoothChangeTextures[i] = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
                _smoothChangeTextures[i + _switcher.LightmapPerState] = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
#if ENABLE_SHADOW_MASK
                _smoothChangeTextures[i + _switcher.LightmapPerState * 2] = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
#endif

                _midBlendedLightmaps[i] = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
                _midBlendedLightmaps[i + _switcher.LightmapPerState] = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
#if ENABLE_SHADOW_MASK
                _midBlendedLightmaps[i + _switcher.LightmapPerState * 2] = new Texture2D(width, height, TextureFormat.RGBAHalf, false);
#endif
            }

            if (_lightProbeIndexes.Length == 0)
                return;

            _firstSH = _switcher.SH1;
            _secondSH = _switcher.SH2;
            _shResult = _switcher.SHResult;
            _shLerpFactor = _switcher.SHLerpFactor;

            _probes = new SphericalHarmonicsL2[_lightProbeIndexes.Length];

            SetCurrentProbes();
#endif
            }

#if ENABLE_LIGHTMAP_LERP
        private void SetCurrentProbes()
        {
            for (int i = 0; i < _probes.Length; i++)
            {
                _probes[i] = _switcher.CurrentBakedProbes[_lightProbeIndexes[i]];
            }
        }
#endif

        public void SwitchLightState(int lightStateToSwitch)
        {
            if (lightStateToSwitch > _switcher.LightStates)
                throw new Exception("Requested light state is out of bounds");

#if ENABLE_LIGHTMAP_LERP
            CancelSmoothLightTransition();
#endif

            CurrentLightState = lightStateToSwitch;

            for (int i = 0; i < _staticRenderers.Length; i++)
            {
                if (_staticRenderers[i].StartIndex == -1)
                {
                    if (_shouldWarnAboutStaticNonuseOfLightmap)
                        Debug.LogWarning(_staticRenderers[i].ThisRenderer.gameObject.name + " is static, but does not make use of baked light textures.\n" +
                                            "If this is intended, you can disable this warning by setting the Should Warn About Static Nonuse Of Lightmap bool to false");

                    continue;
                }

                _staticRenderers[i].ThisRenderer.lightmapIndex = (lightStateToSwitch * _switcher.LightmapPerState) + _staticRenderers[i].StartIndex;
            }

            _switcher.SwitchCurrentBakedProbeData(lightStateToSwitch, _lightProbeIndexes);
        }

#if ENABLE_LIGHTMAP_LERP
        public void SwitchLightStateSmooth(int lightStateToSwitch, int framesBetweenChecks, float timeToBlend)
        {
            if (!_willUseSmoothLightTransition)
                return;

            if (lightStateToSwitch > _switcher.LightStates)
                throw new Exception("Requested light state is out of bounds");

            if (_lightSmoothArrayIndexes == null)
            {
                _lightSmoothArrayIndexes = _switcher.AddNewTemporaryLightmapSlots(CurrentLightState);
#if ENABLE_SHADOW_MASK
                var masks = _switcher.States[CurrentLightState].ShadowMasks.Length > 0 ? _switcher.States[CurrentLightState].ShadowMasks : null;
#elif !ENABLE_SHADOW_MASK
                Texture2D[] masks = null;
#endif
                BlendSmooth(_switcher.States[CurrentLightState].LLightmaps, _switcher.States[CurrentLightState].DLightmaps, masks, lightStateToSwitch, framesBetweenChecks, timeToBlend, _lightSmoothArrayIndexes);
            }
            else
            {
                var t1 = new Texture2D[_switcher.LightmapPerState];
                var t2 = new Texture2D[_switcher.LightmapPerState];
#if ENABLE_SHADOW_MASK
                var t3 = _switcher.States[CurrentLightState].ShadowMasks.Length > 0 ? new Texture2D[_switcher.LightmapPerState] : null;
#elif !ENABLE_SHADOW_MASK
                Texture2D[] t3 = null;
#endif

                for (int i = 0; i < _switcher.LightmapPerState; i++)
                {
                    Graphics.CopyTexture(LightmapSettings.lightmaps[_lightSmoothArrayIndexes[i]].lightmapColor, _midBlendedLightmaps[i]);
                    Graphics.CopyTexture(LightmapSettings.lightmaps[_lightSmoothArrayIndexes[i]].lightmapDir, _midBlendedLightmaps[i + _switcher.LightmapPerState]);

                    if (t3 != null)
                    {
                        Graphics.CopyTexture(LightmapSettings.lightmaps[_lightSmoothArrayIndexes[i]].shadowMask, _midBlendedLightmaps[i + _switcher.LightmapPerState * 2]);
                        t3[i] = _midBlendedLightmaps[i + _switcher.LightmapPerState * 2];
                    }

                    t1[i] = _midBlendedLightmaps[i];
                    t2[i] = _midBlendedLightmaps[i + _switcher.LightmapPerState];
                }

                BlendSmooth(t1, t2, t3, lightStateToSwitch, framesBetweenChecks, timeToBlend, _lightSmoothArrayIndexes);
            }

            CurrentLightState = lightStateToSwitch;

            if (_lightProbeIndexes.Length != 0)
                BlendSH(_probes, lightStateToSwitch, timeToBlend, framesBetweenChecks);
        }

        public void SwitchLightStateSmoothToNext(int framesBetweenTextureCheck, float timeToBlend)
        {
            int nextLightState = _switcher.LightStates >= CurrentLightState + 1 ? CurrentLightState + 1 : 0;

            SwitchLightStateSmooth(nextLightState, framesBetweenTextureCheck, timeToBlend);
        }

        private async void BlendSmooth(Texture2D[] fromL, Texture2D[] fromD, Texture2D[] fromS, int to, int framesBetweenTextureCheck, float timeToBlend, int[] lightmapsLerpSlots)
        {
            _stopSource.Cancel();
            await Task.Yield();
            _stopSource = new CancellationTokenSource();

            framesBetweenTextureCheck = framesBetweenTextureCheck > 0 ? framesBetweenTextureCheck : 1;

            int mapsPerState = _switcher.LightmapPerState;
            int width = _switcher.LightmapSize.x, height = _switcher.LightmapSize.y;

            for (int i = 0; i < _staticRenderers.Length; i++)
            {
                if (_staticRenderers[i].StartIndex == -1)
                {
                    if (_shouldWarnAboutStaticNonuseOfLightmap)
                        Debug.LogWarning(_staticRenderers[i].ThisRenderer.gameObject.name + " is static, but does not make use of baked light textures.\n" +
                                            "If this is intended, you can disable this warning by setting the Should Warn About Static Nonuse Of Lightmap bool to false");

                    continue;
                }

                _staticRenderers[i].ThisRenderer.lightmapIndex = lightmapsLerpSlots[_staticRenderers[i].StartIndex];
            }

            var lightmaps = LightmapSettings.lightmaps;

#if ENABLE_SHADOW_MASK
            int amountOfTextureToProcess = 3;
#elif !ENABLE_SHADOW_MASK
            int amountOfTextureToProcess = 2;
#endif
            int kernelIndex = _lightmapBlender.FindKernel("CSMain");

            _lightmapBlender.SetFloats(_lightmapSize, width, height);

#if ENABLE_SHADOW_MASK
            if (fromS == null)
                _lightmapBlender.SetTexture(kernelIndex, _shadowMask1, _switcher.ShadowMaskReplacement);

            if (_switcher.States[to].ShadowMasks.Length == 0)
                _lightmapBlender.SetTexture(kernelIndex, _shadowMask2, _switcher.ShadowMaskReplacement);

            if (fromS == null && _switcher.States[to].ShadowMasks.Length == 0)
            {
                _lightmapBlender.SetInt(_shouldUseShadowMask, 0);
                _lightmapBlender.SetTexture(kernelIndex, _sResult, _sRenderOut);

                amountOfTextureToProcess = 2;
            }
            else
            {
                _lightmapBlender.SetInt(_shouldUseShadowMask, 1);
            }

#elif !ENABLE_SHADOW_MASK
            _lightmapBlender.SetTexture(kernelIndex, _shadowMask1, _sRenderOut);
            _lightmapBlender.SetTexture(kernelIndex, _shadowMask2, _sRenderOut);
            _lightmapBlender.SetInt(_shouldUseShadowMask, 0);
            _lightmapBlender.SetTexture(kernelIndex, _sResult, _sRenderOut);
#endif

            float t = 0;

            while (t < timeToBlend && !_cancelSource.IsCancellationRequested)
            {
                if (_stopSource.IsCancellationRequested)
                {
                    _lRenderOut.Release();
                    _dRenderOut.Release();
                    _sRenderOut?.Release();

                    return;
                }

                for (int i = 0; i < framesBetweenTextureCheck; i++)
                {
                    if (_stopSource.IsCancellationRequested)
                    {
                        _lRenderOut.Release();
                        _dRenderOut.Release();
                        _sRenderOut?.Release();

                        return;
                    }

                    if (_cancelSource.IsCancellationRequested)
                        break;

                    t += Time.deltaTime;

                    await Task.Yield();
                }

                // These checks are placed so that the awaits won't manage to avoid the cancelation request until it is reset.
                if (_cancelSource.IsCancellationRequested)
                    break;

                _lightmapBlender.SetFloat(_lerpFactor, t / timeToBlend);

                for (int i = 0; i < mapsPerState; i++)
                {
                    if (_stopSource.IsCancellationRequested)
                    {
                        _lRenderOut.Release();
                        _dRenderOut.Release();
                        _sRenderOut?.Release();

                        return;
                    }

                    if (_cancelSource.IsCancellationRequested)
                        break;

                    _lightmapBlender.SetTexture(kernelIndex, _lLightmap1, fromL[i]);
                    _lightmapBlender.SetTexture(kernelIndex, _lLightmap2, _switcher.States[to].LLightmaps[i]);
                    _lightmapBlender.SetTexture(kernelIndex, _dLightmap1, fromD[i]);
                    _lightmapBlender.SetTexture(kernelIndex, _dLightmap2, _switcher.States[to].DLightmaps[i]);

                    _lightmapBlender.SetTexture(kernelIndex, _lResult, _lRenderOut);
                    _lightmapBlender.SetTexture(kernelIndex, _dResult, _dRenderOut);

#if ENABLE_SHADOW_MASK
                    if (fromS != null || _switcher.States[to].ShadowMasks.Length > 0)
                    {
                        if (fromS != null)
                            _lightmapBlender.SetTexture(kernelIndex, _shadowMask1, fromS[i]);

                        if (_switcher.States[to].ShadowMasks.Length > 0)
                            _lightmapBlender.SetTexture(kernelIndex, _shadowMask2, _switcher.States[to].ShadowMasks[i]);

                        _lightmapBlender.SetTexture(kernelIndex, _sResult, _sRenderOut);
                    }
#endif

                    _lightmapBlender.Dispatch(kernelIndex, (width * amountOfTextureToProcess) / 8, (height * amountOfTextureToProcess) / 8, 1);

                    lightmaps[lightmapsLerpSlots[i]].lightmapColor = RenderTextureToTexture2D(_lRenderOut, _smoothChangeTextures[i]);
                    lightmaps[lightmapsLerpSlots[i]].lightmapDir = RenderTextureToTexture2D(_dRenderOut, _smoothChangeTextures[i + mapsPerState]);

#if ENABLE_SHADOW_MASK
                    if (fromS != null || _switcher.States[to].ShadowMasks.Length > 0)
                        lightmaps[lightmapsLerpSlots[i]].shadowMask = RenderTextureToTexture2D(_sRenderOut, _smoothChangeTextures[i + mapsPerState * 2]);
#endif
                }

                _switcher.SetLightmaps(lightmapsLerpSlots, lightmaps);

                await Task.Yield();
            }

            _lRenderOut.Release();
            _dRenderOut.Release();
            _sRenderOut?.Release();
            _switcher.RemoveTemporaryLightmapSlots(lightmapsLerpSlots);
            SwitchLightState(to);

            _lightSmoothArrayIndexes = null;
        }

        private async void BlendSH(SphericalHarmonicsL2[] from, int to, float timeToBlend, float framesBetweenSHCheck)
        {
            // Ensure CancellationTokenSource is reset.
            await Task.Yield();

            SphericalHarmonicsL2[] specificStateProbes = new SphericalHarmonicsL2[_lightProbeIndexes.Length];

            for (int i = 0; i < specificStateProbes.Length; i++)
            {
                specificStateProbes[i] = _switcher.States[to].StateProbeData[_lightProbeIndexes[i]];
            }

            _sh1.SetData(from);
            _sh2.SetData(specificStateProbes);

            float t = 0;
            SphericalHarmonicsL2[] outData = new SphericalHarmonicsL2[_lightProbeIndexes.Length];

            int threadGroups = (int)(_lightProbeIndexes.Length / 64);
            threadGroups = threadGroups < 1 ? 1 : threadGroups;

            while (t < timeToBlend && !_cancelSource.IsCancellationRequested)
            {
                for (int i = 0; i < framesBetweenSHCheck; i++)
                {
                    if (_stopSource.IsCancellationRequested || _cancelSource.IsCancellationRequested)
                    {
                        SetCurrentProbes();

                        if (_stopSource.IsCancellationRequested)
                            return;

                        if (_cancelSource.IsCancellationRequested)
                            break;
                    }

                    t += Time.deltaTime;

                    await Task.Yield();
                }

                if (_stopSource.IsCancellationRequested)
                    return;

                if (_cancelSource.IsCancellationRequested)
                    break;

                _shBlender.SetBuffer(0, _firstSH, _sh1);
                _shBlender.SetBuffer(0, _secondSH, _sh2);

                _shBlender.SetBuffer(0, _shResult, _outSH);
                _shBlender.SetFloat(_shLerpFactor, t / timeToBlend);
                _shBlender.Dispatch(0, threadGroups, 1, 1);

                _outSH.GetData(outData);
                _switcher.SwitchCurrentBakedProbeDataSmoothly(outData, _lightProbeIndexes);

                await Task.Yield();
            }

            _switcher.SwitchCurrentBakedProbeData(to, _lightProbeIndexes);
            SetCurrentProbes();
        }
#endif

        public void SwitchLightStateToNext()
        {
            int nextLightState = LightSwitcher.Instance.LightStates >= CurrentLightState + 1 ? CurrentLightState + 1 : 0;

            SwitchLightState(nextLightState);
        }

#if ENABLE_LIGHTMAP_LERP
        public async void CancelSmoothLightTransition()
        {
            if (!_willUseSmoothLightTransition)
                return;

            _cancelSource.Cancel();
            await Task.Yield();
            _cancelSource = new CancellationTokenSource();
        }

        private Texture2D RenderTextureToTexture2D(RenderTexture rt, Texture2D tex)
        {
            Graphics.CopyTexture(rt, tex);

            return tex;
        }
#endif

#if ENABLE_LIGHTMAP_LERP
        private void OnDisable()
        {
            _cancelSource?.Cancel();

            if (_lightProbeIndexes.Length == 0)
                return;

            _sh1.Release();
            _sh2.Release();
            _outSH.Release();

            _sh1 = null;
            _sh2 = null;
            _outSH = null;
        }
#endif

#if UNITY_EDITOR
        public void GetStaticRenderers()
        {
            List<RendererData> renderers = new List<RendererData>();

            foreach (var col in Physics.OverlapBox(transform.position, transform.lossyScale / 2))
            {
                if (!col.gameObject.isStatic)
                    continue;

                var temp = col.GetComponentInChildren<Renderer>();

                if (temp != null)
                    renderers.Add(new RendererData(temp));
            }

            _staticRenderers = renderers.ToArray();

            Undo.RecordObject(this, "Update Static Renderers");
            EditorUtility.SetDirty(this);
        }

        public void GetStaticRenderersWithoutCol()
        {
            var bounds = new Bounds(transform.position, transform.lossyScale);

            List<RendererData> renderers = new List<RendererData>();

            foreach (var g in GameObject.FindObjectsOfType<GameObject>())
            {
                if (!g.isStatic || !bounds.Contains(g.transform.position))
                    continue;

                var temp = g.GetComponentInChildren<Renderer>();

                if (temp != null)
                    renderers.Add(new RendererData(temp));
            }

            _staticRenderers = renderers.ToArray();

            Undo.RecordObject(this, "Update Static Renderers");
            EditorUtility.SetDirty(this);
        }

        public void GetProbesWithinBounds()
        {
            var bounds = new Bounds(transform.position, transform.lossyScale);

            List<int> indexes = new List<int>();

            foreach (var pos in LightmapSettings.lightProbes.positions)
            {
                if (bounds.Contains(pos))
                    indexes.Add(Array.IndexOf(LightmapSettings.lightProbes.positions, pos));
            }

            _lightProbeIndexes = indexes.ToArray();

            Undo.RecordObject(this, "Set Probe Indexes");
            EditorUtility.SetDirty(this);
        }

        private void OnDrawGizmos()
        {
            if (!_shouldDisplayWireFrame)
                return;

            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
#endif
    }
}
