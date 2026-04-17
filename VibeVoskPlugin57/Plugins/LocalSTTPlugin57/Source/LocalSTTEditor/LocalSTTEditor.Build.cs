using UnrealBuildTool;

public class LocalSTTEditor : ModuleRules
{
    public LocalSTTEditor(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine" });

        PrivateDependencyModuleNames.AddRange(new string[] {
            "Slate",
            "SlateCore",
            "UnrealEd",
            "Projects",
            "Settings",
            "DeveloperToolSettings",
            "EditorStyle",
            "LocalSTTCore"
        });

        if (Target.bBuildEditor)
        {
            PrivateDependencyModuleNames.Add("UnrealEd");
        }
    }
}
