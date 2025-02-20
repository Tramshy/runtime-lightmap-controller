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

public class DefineSymbolSetup
{
    private const string DEFINE_SYMBOL = "ENABLE_LIGHTMAP_LERP";

#if !ENABLE_LIGHTMAP_LERP
    [MenuItem("Tools/Enable Lightmap Lerp")]
    public static void EnableLightmapLerp()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        if (!defines.Contains("ENABLE_LIGHTMAP_LERP"))
        {
            defines += ";ENABLE_LIGHTMAP_LERP";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
            Debug.Log($"{DEFINE_SYMBOL} enabled!");
        }
    }
#elif ENABLE_LIGHTMAP_LERP
    [MenuItem("Tools/Disable Lightmap Lerp")]
    public static void DisableLightmapLerp()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        if (defines.Contains("ENABLE_LIGHTMAP_LERP"))
        {
            defines = defines.Replace(DEFINE_SYMBOL, "").Replace(";;", ";").Trim(';');
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defines);
            Debug.Log($"{DEFINE_SYMBOL} disabled!");
        }
    }
#endif
}
