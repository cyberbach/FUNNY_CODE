// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

using UnrealBuildTool;

public class LocalSTTBlueprint : ModuleRules
{
	public LocalSTTBlueprint(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"LocalSTTCore"
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
			"Slate",
			"SlateCore"
		});
	}
}
