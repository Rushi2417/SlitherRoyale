using UnityEngine;
using UnityEditor;

public static class MapAssetGenerator
{
    [MenuItem("Tools/Slither Royale/Generate Map Assets")]
    public static void Generate()
    {
        GenerateMap("Neon Grid", MapConfig.NeonGrid);
        GenerateMap("Coral Reef", MapConfig.CoralReef);
        GenerateMap("Magma Core", MapConfig.MagmaCore);
        GenerateMap("Candy Kingdom", MapConfig.CandyKingdom);
        GenerateMap("Space Station", MapConfig.SpaceStation);
        GenerateMap("Haunted Forest", MapConfig.HauntedForest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MapAssetGenerator] All 6 map assets created");
    }

    private static void GenerateMap(string name, System.Func<MapConfig> factory)
    {
        string path = $"Assets/Maps/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<MapConfig>(path);
        if (existing != null) return;
        var config = factory();
        config.name = name;
        AssetDatabase.CreateAsset(config, path);
    }
}
