// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

using UnrealBuildTool;

public class VibeVoskBlueprint : ModuleRules
{
	public VibeVoskBlueprint(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"VibeVoskCore"
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
			"Slate",
			"SlateCore"
		});
	}
}
