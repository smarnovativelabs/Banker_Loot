#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// The Firebase .aar files in Assets/GeneratedLocalRepo are only a local maven repository for Gradle. When one of
/// them is imported as a normal Android plugin it is packed a second time next to the maven dependency and the
/// manifest merger fails with "Namespace ... is used in multiple modules". The dependency resolver sometimes
/// rewrites such a .meta without its import settings, so this switches them off again right before every build.
/// </summary>
public class FirebaseLocalRepoGuard : IPreprocessBuildWithReport
{
    const string RepoFolder = "Assets/GeneratedLocalRepo";
    const string ResolverLabel = "gpsr";

    // later than the resolver's own pre-build steps
    public int callbackOrder => 10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Apply();
    }

    [MenuItem("Banker Loot/Fix Firebase Local Repo Plugins")]
    public static void Apply()
    {
        if (!AssetDatabase.IsValidFolder(RepoFolder))
            return;

        foreach (var guid in AssetDatabase.FindAssets("", new[] { RepoFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".aar"))
                continue;
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null || (!importer.GetCompatibleWithAnyPlatform()
                && !importer.GetCompatibleWithPlatform(BuildTarget.Android)
                && !importer.GetCompatibleWithEditor()))
                continue;

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, false);
            importer.SetCompatibleWithEditor(false);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null)
            {
                AssetDatabase.SetLabels(asset, new[] { ResolverLabel });
            }
            importer.SaveAndReimport();
            Debug.Log("[BankerLoot] Switched off Android plugin import of " + path);
        }
    }
}
#endif
