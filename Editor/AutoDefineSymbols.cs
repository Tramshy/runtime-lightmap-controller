using UnityEditor;
using UnityEngine;

//[InitializeOnLoad]
//public static class AutoDefineSymbols
//{
//    static AutoDefineSymbols()
//    {
//        const string define = "ENABLE_LIGHTMAP_LERP";
//        BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;

//        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
//        if (!defines.Contains(define))
//        {
//            defines += ";" + define;
//            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
//            Debug.Log($"Added scripting define symbol: {define}");
//        }
//    }
//}

namespace RuntimeLightmapController.LightmapEditor
{
    public class DefineSymbolSetup
    {
        private const string LIGHTMAP_LERP_DEFINE_SYMBOL = "ENABLE_LIGHTMAP_LERP", SHADOW_MASK_SUPPORT_DEFINE_SYMBOL = "ENABLE_SHADOW_MASK";

#if !ENABLE_LIGHTMAP_LERP
    [MenuItem("Tools/Runtime Lightmap Controller/Enable Lightmap Lerp")]
    public static void EnableLightmapLerp()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        if (!defines.Contains(LIGHTMAP_LERP_DEFINE_SYMBOL))
        {
            defines += ";" + LIGHTMAP_LERP_DEFINE_SYMBOL;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
            Debug.Log($"{DEFINE_SYMBOL} enabled!");
        }
    }
#elif ENABLE_LIGHTMAP_LERP
        [MenuItem("Tools/Runtime Lightmap Controller/Disable Lightmap Lerp")]
        public static void DisableLightmapLerp()
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

            if (defines.Contains(LIGHTMAP_LERP_DEFINE_SYMBOL))
            {
                defines = defines.Replace(LIGHTMAP_LERP_DEFINE_SYMBOL, "").Replace(";;", ";").Trim(';');
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
                Debug.Log($"{LIGHTMAP_LERP_DEFINE_SYMBOL} disabled!");
            }
        }
#endif

#if !ENABLE_SHADOW_MASK
        [MenuItem("Tools/Runtime Lightmap Controller/Enable Shadow Mask Support")]
        public static void EnableShadowMaskSupport()
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

            if (!defines.Contains(SHADOW_MASK_SUPPORT_DEFINE_SYMBOL))
            {
                defines += ";" + SHADOW_MASK_SUPPORT_DEFINE_SYMBOL;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
                Debug.Log($"{SHADOW_MASK_SUPPORT_DEFINE_SYMBOL} enabled!");
            }
        }
#elif ENABLE_SHADOW_MASK
    [MenuItem("Tools/Runtime Lightmap Controller/Disable Shadow Mask Support")]
    public static void DisableShadowMaskSupport()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        if (defines.Contains(SHADOW_MASK_SUPPORT_DEFINE_SYMBOL))
        {
            defines = defines.Replace(SHADOW_MASK_SUPPORT_DEFINE_SYMBOL, "").Replace(";;", ";").Trim(';');
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
            Debug.Log($"{SHADOW_MASK_SUPPORT_DEFINE_SYMBOL} disabled!");
        }
    }
#endif
    }
}
