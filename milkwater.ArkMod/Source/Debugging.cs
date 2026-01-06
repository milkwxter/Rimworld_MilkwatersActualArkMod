using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class ArkShaderPathDebugger
{
    static ArkShaderPathDebugger()
    {
        const string packageId = "milkwater.ArkMod";
        const string shaderName = "ColorRegionTint";

        var mod = LoadedModManager.RunningModsListForReading
            .FirstOrDefault(m => m.PackageIdPlayerFacing == packageId);

        if (mod == null)
        {
            Log.Error("[ArkMod] Could not find mod with packageId " + packageId);
            return;
        }

        string pathRoot = Path.Combine("Assets", "Data");
        string byFolderName = Path.Combine(pathRoot, mod.FolderName);
        string byPackageId = Path.Combine(pathRoot, mod.PackageIdPlayerFacing);
        string shaderContent = GenFilePaths.ContentPath<Shader>(); // this is the missing piece

        string candidate1NoExt = Path.Combine(Path.Combine(byFolderName, shaderContent), shaderName);
        string candidate2NoExt = Path.Combine(Path.Combine(byPackageId, shaderContent), shaderName);

        Log.Message("[ArkMod] GenFilePaths.ContentPath<Shader>() = '" + shaderContent + "'");
        Log.Message("[ArkMod] Candidate shader path 1 (no ext): " + candidate1NoExt);
        Log.Message("[ArkMod] Candidate shader path 2 (no ext): " + candidate2NoExt);
    }
}
