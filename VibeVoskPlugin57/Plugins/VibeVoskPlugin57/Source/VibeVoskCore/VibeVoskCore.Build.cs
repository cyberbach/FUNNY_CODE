// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

using UnrealBuildTool;
using System.IO;

public class VibeVoskCore : ModuleRules
{
	public VibeVoskCore(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"AudioCapture",
			"AudioCaptureCore",
			"AudioMixer",
			"SignalProcessing",
			"Json",
			"JsonUtilities",
			"DeveloperSettings",
			"Projects"
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
			"Slate",
			"SlateCore"
		});

		// Настройка VOSK SDK
		string VoskBaseDir = Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "vosk");
		string VoskIncludeDir = Path.Combine(VoskBaseDir, "include");
		string VoskBinDir = Path.Combine(VoskBaseDir, "bin");

		// Добавляем пути к заголовочным файлам
		PublicIncludePaths.Add(VoskIncludeDir);

		// Добавляем библиотеку для линковки
		string VoskLibDir = Path.Combine(VoskBaseDir, "lib");
		PublicAdditionalLibraries.Add(Path.Combine(VoskLibDir, "vosk.lib"));

		// Платформо-зависимая конфигурация
		if (Target.Platform == UnrealTargetPlatform.Win64)
		{
			// Копирование DLL в бинарную папку плагина при сборке
			string[] DllFiles = new string[]
			{
				"libvosk.dll",
				"libgcc_s_seh-1.dll",
				"libstdc++-6.dll",
				"libwinpthread-1.dll"
			};

			string PluginBinDir = Path.Combine(ModuleDirectory, "..", "..", "Binaries", "Win64");

			foreach (string DllName in DllFiles)
			{
				string SourcePath = Path.Combine(VoskBinDir, DllName);
				if (File.Exists(SourcePath))
				{
					RuntimeDependencies.Add(Path.Combine(PluginBinDir, DllName), SourcePath, StagedFileType.NonUFS);
				}
			}

			// Копирование папки Models (включая подпапки) в Binaries/Win64/Models
			string ModelsSourceDir = Path.Combine(ModuleDirectory, "..", "..", "Models");
			if (Directory.Exists(ModelsSourceDir))
			{
				string[] modelFiles = Directory.GetFiles(ModelsSourceDir, "*", SearchOption.AllDirectories);
				foreach (string SourcePath in modelFiles)
				{
					string RelativePath = SourcePath.Substring(ModelsSourceDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
					string DestPath = Path.Combine(PluginBinDir, "Models", RelativePath);
					RuntimeDependencies.Add(DestPath, SourcePath, StagedFileType.NonUFS);
				}
			}

			PublicDefinitions.Add("WITH_VOSK=1");
		}
		else
		{
			// TODO: Добавить поддержку Linux (libvosk.so) и macOS (libvosk.dylib)
			// Для Linux: VoskBinDir → .so, VoskLibDir → .a
			// Для macOS: VoskBinDir → .dylib
			PublicDefinitions.Add("WITH_VOSK=0");
		}
	}
}
