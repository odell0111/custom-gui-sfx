using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;

using CustomGuiSfx.Model;
using CustomGuiSfx.ViewModel.Helpers;

using Microsoft.Win32;

using Newtonsoft.Json;

namespace CustomGuiSfx;
public partial class App : Application
{
	// ****************************************************
	// PROGRAM_LENGTH must contain the final single executable file size
	// Its value is setted automatically by PROGRAM_LENGTH_AutoSetter after each build
	// ****************************************************
	public const int PROGRAM_LENGTH = 2891783;
	// ****************************************************
	// ****************************************************
	// ****************************************************

	public const string PROJECT_NAME = "Custom GUI Self-Extracting Archive";
	public const string PROJECT_SHORT_NAME = "Custom GUI SFX";
	public const string CFS = "###CFS###";
	public const string CFE = "###CFE###";
	public const int CONFIGURATION_FILE_MAX_SIZE = 1024 * 5; //5KB
	public const string GITHUB_REPOSITORY_LINK = "https://github.com/odell0111/custom-gui-sfx";
	private const string PROGRAM_REGISTRY_KEY_PATH = $@"Software\Classes\Directory\Background\shell\{PROJECT_SHORT_NAME}";


	public static readonly string programInstallationRootPath;
	public static readonly string programInstallationPath;
	public static readonly string programSettingsFilePath;
	public static readonly string programSendToShortcutPath;
	public static readonly Version programVersion;
	public static readonly string installedProgramPath;

	private enum ProgramInstallationState { Uninstalled, Installed, Broken }

	private Action? onExit;

	static App()
	{
		programInstallationRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OGM");

		programInstallationPath = Path.Combine(programInstallationRootPath, PROJECT_SHORT_NAME);

		programSettingsFilePath = Path.Combine(programInstallationPath, "settings.json");

		programSendToShortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SendTo), $"{PROJECT_SHORT_NAME}.lnk");

		var programVersionFVI = FileVersionInfo.GetVersionInfo(Environment.ProcessPath!);
		programVersion = new Version(programVersionFVI.ProductMajorPart, programVersionFVI.ProductMinorPart, programVersionFVI.ProductBuildPart);

		installedProgramPath = Path.Combine(programInstallationPath, $"{typeof(ViewModel.CustomGuiSfxVM).Assembly.GetName().Name}.exe");
	}

	#region Overrides
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Getting Command Line arguments, Skipping first one (Program executable/Main dll path) since it's not necessary
		var args = Environment.GetCommandLineArgs().Skip(1);

		try
		{
			// Checking if any switch was passed
			foreach (var arg in args)
			{
				switch (arg)
				{
					// Install switch [-i, --install]
					case "-i":
					case "--install":
						string? iMess = null;
						if (GetProgramInstallationState().Equals(ProgramInstallationState.Installed))
						{
							var fvi = FileVersionInfo.GetVersionInfo(installedProgramPath);
							var installedProgramVersion = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart);

							// If there is a later version installed
							if (installedProgramVersion.CompareTo(programVersion).Equals(1))
								iMess = $"A later version of {PROJECT_SHORT_NAME} is installed on your system. Do you want to install this version anyway?\n\n" +
								$"Installed Version: {installedProgramVersion}\n" +
								$"{"Current Version: ",-19}{programVersion}";
							else if (installedProgramVersion.CompareTo(programVersion).Equals(0))
							{
								iMess = $"{PROJECT_SHORT_NAME} v{programVersion} is already installed on your system. Do you want to reinstall it?";
							}
						}
						iMess ??= $"{PROJECT_SHORT_NAME} v{programVersion} will be installed on your system, continue?";
						var iRes = MessageBox.Show(iMess, PROJECT_SHORT_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);
						if (iRes.Equals(MessageBoxResult.Yes))
							Install(false);
						goto Shutdown;

					// Uninstall switch [-u, --uninstall]
					case "-u":
					case "--uninstall":
						string uMess;
						if (GetProgramInstallationState().Equals(ProgramInstallationState.Installed))
						{
							var fvi = FileVersionInfo.GetVersionInfo(installedProgramPath);
							var installedProgramVersion = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart);

							uMess = $"{PROJECT_SHORT_NAME} v{installedProgramVersion} will be uninstalled from your system, continue?";
						}
						else
							uMess = $"{PROJECT_SHORT_NAME} seems not be installed or fully installed on the system, try to perform an uninstallation anyway?";

						var uRes = MessageBox.Show(uMess, PROJECT_SHORT_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);
						if (uRes.Equals(MessageBoxResult.Yes))
							Uninstall();
						goto Shutdown;

					// Help switch [-h, --help]
					case "-h":
					case "--help":
						var sb = new StringBuilder();
						sb.AppendLine($"{PROJECT_SHORT_NAME} v{programVersion} : Copyright (c) 2023 Odell Garcia : August, 2023 : {GITHUB_REPOSITORY_LINK}\n");
						sb.AppendLine("Switches:");
						sb.AppendLine($"{"-i, --install",-20} Install, update, downgrade, fix existing installation");
						sb.AppendLine($"{"-u, --uninstall",-20} Uninstall");
						sb.AppendLine($"{"-h, --help",-20} Show this help");
						HelperMethods.WriteToConsole(sb.ToString());

						goto Shutdown;
				}
			}

			Install(true);
		} catch (Exception ex)
		{
			HelperMethods.ShowErrorMessageBox($"Error during program installation/uninstallation. Cause: " + ex.Message);

			Shutdown(3);
			return;
		}

		// Setting Startup Window
		StartupUri = new(args.Count() > 0 ?
			"View/SFXCreationProgressWindow.xaml" :
			"View/MainWindow.xaml", UriKind.Relative);

		return;

	Shutdown:
		Shutdown(0);
	}

	protected override void OnExit(ExitEventArgs e)
	{
		base.OnExit(e);
		onExit?.Invoke();
	}
	#endregion

	/// <summary>
	/// Install, Fix, or Update program... If checkExistingInstallation is false this method will go directly to install the program
	/// </summary>
	private void Install(bool checkExistingInstallation)
	{
		if (!Directory.Exists(programInstallationPath))
			Directory.CreateDirectory(programInstallationPath);

		var installedProgramFI = new FileInfo(installedProgramPath);
		var sendToProgramShortcutFI = new FileInfo(programSendToShortcutPath);

		var wasThisProcessStartedByInstalledProgram = string.Equals(installedProgramPath, Environment.ProcessPath!);

		if (checkExistingInstallation)
		{
			var message = string.Empty;
			if (!GetProgramInstallationState().Equals(ProgramInstallationState.Installed))
			{
				message = $"{PROJECT_SHORT_NAME} seems not to be installed or fully installed, do you wish to install it now?";
			}
			//else if at least installed program executable exists and it's not attached to this process
			else if (installedProgramFI.Exists && !wasThisProcessStartedByInstalledProgram)
			{
				var installedProgramVersionFVI = FileVersionInfo.GetVersionInfo(installedProgramPath);
				var installedProgramVersion = new Version(installedProgramVersionFVI.ProductMajorPart, installedProgramVersionFVI.ProductMinorPart, installedProgramVersionFVI.ProductBuildPart);

				// If installed program is an earlier version
				if (programVersion.CompareTo(installedProgramVersion).Equals(1))
				{
					message = $"An earlier version of {PROJECT_SHORT_NAME} is installed on your system, do you wish to updated it?\n\n" +
					$"Installed Version: {installedProgramVersion}\n" +
					$"{"Current Version: ",-19}{programVersion}";
				}
			}

			if (string.IsNullOrWhiteSpace(message))
				return;

			var result = MessageBox.Show(message, PROJECT_SHORT_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result.Equals(MessageBoxResult.No))
				return;
		}

		// Creating Program Executable if... :|
		if (!wasThisProcessStartedByInstalledProgram)
		{
			using (var bw = new BinaryWriter(File.Create(installedProgramPath)))
			{
				using var br = new BinaryReader(File.OpenRead(Environment.ProcessPath!));
				bw.Write(br.ReadBytes(PROGRAM_LENGTH));
			}
		}

		// Creating Program SendTo Shortcut
		var shell = new IWshRuntimeLibrary.WshShell();
		var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(sendToProgramShortcutFI.FullName);
		shortcut.Description = "SendTo shortcut to create a Custom GUI Self-Extracting Archive by sending source files through Send To menu";
		shortcut.WorkingDirectory = programInstallationPath;
		shortcut.TargetPath = installedProgramPath;
		shortcut.Save();

		// Creating config.txt template
		var configTemplateDirectory = Path.Combine(programInstallationPath, "Configuration Template");
		if (!Directory.Exists(configTemplateDirectory))
			Directory.CreateDirectory(configTemplateDirectory);
		var configTemplateFilePath = Path.Combine(configTemplateDirectory, "config.txt");
		File.WriteAllText(configTemplateFilePath, Configuration.GetTemplate());
		File.Copy(Path.Combine(configTemplateDirectory, "config.txt"), Path.Combine(configTemplateDirectory, "config.txt.bak"), true);

		// Creating settings.json file
		File.WriteAllText(programSettingsFilePath, JsonConvert.SerializeObject(new ProgramSettings(), Formatting.Indented));

		// If 7-Zip (7z.exe) exists in intallation directory then delete it
		var sevenZipProgramAtInstallationPath = Path.Combine(programInstallationPath, "7z.exe");
		if (File.Exists(sevenZipProgramAtInstallationPath))
			File.Delete(sevenZipProgramAtInstallationPath);

		MessageBox.Show($"{PROJECT_SHORT_NAME} v{programVersion} succesfully installed! :)", PROJECT_SHORT_NAME, MessageBoxButton.OK, MessageBoxImage.Information);

		// Creating registry entry for creating a File Explorer Context Menu entry to create a new Configuration Template
		var rk = Registry.CurrentUser.CreateSubKey(PROGRAM_REGISTRY_KEY_PATH);
		rk.SetValue(null, "New Configuration Template");
		rk.SetValue("Icon", installedProgramPath);

		rk.CreateSubKey("command").SetValue(null, $@"cmd /c copy ""{configTemplateFilePath}"" ""%V"" && start config.txt");

		rk.Close();
	}

	/// <summary>
	/// Uninstall program
	/// </summary>
	private void Uninstall()
	{
		StringBuilder performedActions = new();
		Version? version = null;

		// Deleting Program Installation Root Path
		if (Directory.Exists(programInstallationRootPath))
		{
			var installedProgramPath = Path.Combine(programInstallationPath, $"{GetType().Assembly.GetName().Name}.exe");
			if (File.Exists(installedProgramPath))
			{
				var fvi = FileVersionInfo.GetVersionInfo(installedProgramPath);
				version = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart);
			}

			var msg = $"> Directory: \"{programInstallationRootPath}\"";
			// If this process was started by the executable at program installation path delete it 2 seconds after closing this process, else just delete it inmmediately
			if (installedProgramPath.Equals(Environment.ProcessPath!))
			{
				onExit += () =>
				{
					_ = Process.Start(new ProcessStartInfo()
					{
						FileName = "cmd.exe",
						Arguments = $@"/c timeout /t 2 /nobreak & rmdir ""{programInstallationRootPath}"" /s /q & pause",
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
						WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					});
				};

				msg += " will be deleted as soon as this this MessageBox closes";
			}
			else
			{
				Directory.Delete(programInstallationRootPath, true);
				msg += " deleted";
			}

			performedActions.AppendLine(msg);
		}

		// Deleting SendTo program shortcut
		if (File.Exists(programSendToShortcutPath))
		{
			File.Delete(programSendToShortcutPath);
			performedActions.AppendLine($"> SendTo Shortcut: \"{programSendToShortcutPath}\" deleted");
		}

		// Deleting registry key
		var programRegistryKeyParentKey = PROGRAM_REGISTRY_KEY_PATH.Remove(PROGRAM_REGISTRY_KEY_PATH.IndexOf($@"\{PROJECT_SHORT_NAME}"));
		var rk = Registry.CurrentUser.OpenSubKey(programRegistryKeyParentKey,
			true);
		if (rk is not null)
		{
			try
			{
				rk.DeleteSubKeyTree(PROJECT_SHORT_NAME, true);
				performedActions.AppendLine($"> Registry entry: \"{PROGRAM_REGISTRY_KEY_PATH}\" deleted");

			} catch (Exception) { }
		}

		// Showing message
		string message, performedActns;
		if (string.IsNullOrWhiteSpace(performedActns = performedActions.ToString()))
			message = $"There were nothing to delete, no operation was performed";
		else
		{
			message = PROJECT_SHORT_NAME;

			if (version is not null)
				message += $" v{version}";

			message += " succesfully uninstalled! :(\n\n" + performedActns;
		}

		MessageBox.Show($"{message}", PROJECT_SHORT_NAME, MessageBoxButton.OK, MessageBoxImage.Information);
	}

	/// <summary>
	/// This method checks if the program is (at least how it looks) installed, uninstalled or broken (meaning that there are missing installation components)   
	/// </summary>
	/// <returns>A ProgramInstallationState enum object</returns>
	private static ProgramInstallationState GetProgramInstallationState()
	{
		var keyExists = Registry.CurrentUser.OpenSubKey(PROGRAM_REGISTRY_KEY_PATH) is not null;

		var flags = new System.Collections.Generic.List<bool>()
		{
			File.Exists(installedProgramPath),
			File.Exists(programSendToShortcutPath),
			keyExists
		};

		if (flags.All(x => x is true))
			return ProgramInstallationState.Installed;
		else if (flags.All(x => x is false))
			return ProgramInstallationState.Uninstalled;
		else
			return ProgramInstallationState.Broken;
	}
}