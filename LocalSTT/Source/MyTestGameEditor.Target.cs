using UnrealBuildTool;
using System.Collections.Generic;

public class MyTestGameEditorTarget : TargetRules
{
	public MyTestGameEditorTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.V6;

		ExtraModuleNames.AddRange( new string[] { "MyTestGame" } );
	}
}
