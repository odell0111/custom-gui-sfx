using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using CustomGuiSfx.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileTypeChecker;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using XamlAnimatedGif;

using static CustomGuiSfx.App;
using static CustomGuiSfx.View.MainWindow;
using static CustomGuiSfx.ViewModel.Helpers.HelperMethods;

namespace CustomGuiSfx.ViewModel;

public partial class CustomGuiSfxVM : ObservableObject
{
	public static CustomGuiSfxVM? CustomGuiSfxVMInstance { get; private set; }
	private ProgramSettings programSettings;

	public SevenZipProgram sevenZipProgram = new();
	private readonly BinaryMergedFile archive = new();
	private readonly BinaryMergedFile configurationFile = new();
	private string programFullName;

	#region Binding Properties
	#region CheckBoxes
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ExtractButtonLabel))]
	[NotifyCanExecuteChangedFor(nameof(ExtractCommand))]
	private bool extractArchive = false;
	[ObservableProperty]
	private bool useWindowsConsole = true;
	[ObservableProperty]
	private bool closeAfterExtract = true;
	[ObservableProperty]
	private bool deleteAfterClose = false;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ExtractButtonLabel))]
	[NotifyCanExecuteChangedFor(nameof(ExtractCommand))]
	private bool setCustomArguments = false;
	#endregion

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ExtractCommand))]
	private string customArguments = "x %file_path% -o\"%output_path%\"";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ExtractionPathForeground))]
	[NotifyCanExecuteChangedFor(nameof(ExtractCommand))]
	public string extractionPath;

	public Brush ExtractionPathForeground => ExtractionPathExists ? Brushes.Black : Brushes.Red;

	public string ExtractButtonLabel => ExtractArchive ? "Extract Archive" : SetCustomArguments ? "Send Arguments" : "Extract";

	public bool ExtractCommandCanExecute
	{
		get {
			if (ExtractArchive && SetCustomArguments)
				return false;

			return SetCustomArguments ?
				!string.IsNullOrWhiteSpace(CustomArguments) :
				!IsExtracting && ExtractionPathExists;

			/* Original Code here in case of a nasty refactory by MVS
			
			if (ExtractArchive && SetCustomArguments)
				return false;

			return SetCustomArguments ?
				!string.IsNullOrWhiteSpace(CustomArguments) :
				!IsExtracting && ExtractionPathExists;

			}*/
		}
	}

	public bool IsMainWindowEnabled => !(UseWindowsConsole && IsExtracting);

	#region Configuration Entries
	[ObservableProperty]
	private string displayName = Configuration.DisplayName!;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsLinkAreaVisible))]
	private string link1Label, link2Label, link3Label;
	[ObservableProperty]
	private string comment;

	[ObservableProperty]
	private int displayImageHeight = Configuration.DisplayImageHeightToShowOnUI;
	#endregion

	[ObservableProperty]
	private bool showDisplayImageGrid;

	[ObservableProperty]
	private bool isExtractArchiveCheckboxEnabled;

	[ObservableProperty]
	private string output;
	[ObservableProperty]
	private int outputMaxHeight = 0;

	[ObservableProperty]
	public double progress;
	[ObservableProperty]
	public string version;

	public bool IsLinkAreaVisible => !string.IsNullOrWhiteSpace(Link1Label) || !string.IsNullOrWhiteSpace(Link2Label) || !string.IsNullOrWhiteSpace(Link3Label);

	public bool ExtractionPathExists => Directory.Exists(ExtractionPath);

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsMainWindowEnabled))]
	[NotifyCanExecuteChangedFor(nameof(ExtractCommand))]
	private bool isExtracting;
	#endregion

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public CustomGuiSfxVM()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		CustomGuiSfxVMInstance = this;
		version = programVersion.ToString();
		if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
		{
			/*AnimationBehavior.SetSourceUri(MainWindowInstance.firstDisplayImage, new Uri("ForTesting/cat.gif", UriKind.Relative));
			AnimationBehavior.SetSourceUri(MainWindowInstance.firstDisplayImage, new Uri("ForTesting/error.gif", UriKind.Relative));
			AnimationBehavior.SetSourceUri(MainWindowInstance.firstDisplayImage, new Uri("ForTesting/miEsposaMiSuegra.jpg", UriKind.Relative));
			ShowDisplayImageGrid = true;
			DisplayImageHeight = 200;*/

			DisplayName = PROJECT_NAME;
			Link1Label = "Link1";
			Link2Label = "Link2";
			Link3Label = "Link3";
			Comment = "The quick brown\n dog jumps over\n the lazy fox \n https://www.google.com";
			Configuration.Link1 = "https://link1.com";
			Configuration.Link2 = "https://link2.com";
			Configuration.Link3 = "https://link3.com";
			Progress = 0.5;
			Output = "Extracting example.7z file...\n";
			OutputMaxHeight = 300;
		}
	}

	private void Initialize()
	{
		// Creating 7-Zip program at programInstallationPath if it doesn't exist
		InitializeSevenZipProgram();

		using var fileStream = File.OpenRead(programFullName);
		var binaryReader = new BinaryReader(fileStream);

		// Getting configuration file which must be the first merged file after main program executable file
		{
			configurationFile.Index = PROGRAM_LENGTH;
			_ = fileStream.Seek(configurationFile.Index, SeekOrigin.Begin);
			var (index, content) = FindBytes(fileStream, Encoding.UTF8.GetBytes(CFE), CONFIGURATION_FILE_MAX_SIZE, false);

			if (index.Equals(-1))
				throw new Exception($"Configuration file end line \"{CFE}\" couldn't be found. Either configuration file was not merged, its content is not well formatted, its size is bigger than {CONFIGURATION_FILE_MAX_SIZE / 1024}MB, or an internal logic error occurred");

			configurationFile.Data = content;
			ParseConfiguration(configurationFile);
		}

		// If ArchiveSize entry is not specified, then disable Extract Archive Checkbox control
		IsExtractArchiveCheckboxEnabled = Configuration.ArchiveSize != 0;

		// Setting DisplayName and Window Title
		if (string.IsNullOrWhiteSpace(Configuration.DisplayName))
		{
			Configuration.DisplayName = PROJECT_NAME;
			Configuration.DisplayNameLink = GITHUB_REPOSITORY_LINK;
		}
		else
		{
			MainWindowInstance!.Title = Configuration.DisplayName + " ::: SFX";

			if (string.IsNullOrWhiteSpace(Configuration.DisplayNameLink))
			{
				Configuration.DisplayNameLink = "https://www.google.com/search?q=" + Configuration.DisplayName.Replace(' ', '+');
			}
		}
		DisplayName = Configuration.DisplayName;

		//Setting Comment
		if (!string.IsNullOrWhiteSpace(Configuration.Comment))
			Comment = Configuration.Comment;

		// Setting Links
		{
			static void tryToSetLinkLabel(string label, string link, Action<string> labelSetter)
			{
				if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(link))
					labelSetter(label);
			}

			tryToSetLinkLabel(Configuration.Link1Label!, Configuration.Link1!, l => Link1Label = l);
			tryToSetLinkLabel(Configuration.Link2Label!, Configuration.Link2!, l => Link2Label = l);
			tryToSetLinkLabel(Configuration.Link3Label!, Configuration.Link3!, l => Link3Label = l);
		}

		// Display images must be second merged files
		#region Display Images
		void setDisplayImage(int displayImageSize, Image displayImage)
		{
			if (displayImageSize.Equals(0))
			{
				displayImage.Visibility = Visibility.Hidden;
				return;
			}

			var imageBytes = binaryReader.ReadBytes(displayImageSize);

			AnimationBehavior.SetSourceStream(displayImage, new MemoryStream(imageBytes));

			if (programSettings.SaveDisplayImagesToDesktop)
			{
				var extension = FileTypeValidator.IsTypeRecognizable(imageBytes) ? FileTypeValidator.GetFileType(new MemoryStream(imageBytes)).Extension : "unknownImageFormat";

				File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"displayImage_{Guid.NewGuid()}.{extension}"), imageBytes);
			}
		}

		// First Display Image
		setDisplayImage(Configuration.FirstDisplayImageSize, MainWindowInstance!.firstDisplayImage);
		// Second Display Image
		setDisplayImage(Configuration.SecondDisplayImageSize, MainWindowInstance!.secondDisplayImage);
		// Third Display Image
		setDisplayImage(Configuration.ThirdDisplayImageSize, MainWindowInstance!.thirdDisplayImage);

		// If any display image is set then show grid and set DisplayImageHeight
		if (MainWindowInstance.firstDisplayImage.Visibility.Equals(Visibility.Visible) || 
			MainWindowInstance.secondDisplayImage.Visibility.Equals(Visibility.Visible) ||
			MainWindowInstance.thirdDisplayImage.Visibility.Equals(Visibility.Visible))
		{
			ShowDisplayImageGrid = true;
			DisplayImageHeight = Configuration.DisplayImageHeightToShowOnUI <= 0 ? 200 : Configuration.DisplayImageHeightToShowOnUI;
		}
		#endregion

		// archive must be the last merged file
		archive.Index = fileStream.Position;

		ExtractionPath = Directory.GetParent(programFullName)!.FullName;

		// Setting Output MaxHeight
		OutputMaxHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - (int)MainWindowInstance.ActualHeight - DisplayImageHeight - 100;

		if (programSettings.LogToOutput)
		{
			var sb = new StringBuilder();
			_ = sb.AppendLine(":::: Configuration File ::::");
			_ = sb.AppendLine("Index: " + configurationFile.Index);
			_ = sb.AppendLine("Size: " + configurationFile.Data.Length);
			_ = sb.AppendLine("Content:");
			_ = sb.AppendLine(Encoding.UTF8.GetString(configurationFile.Data));
			_ = sb.AppendLine(Environment.NewLine);

			_ = sb.AppendLine(":::: Parsed Configuration Content ::::");
			_ = sb.AppendLine($"{nameof(Configuration.DisplayName)}: {Configuration.DisplayName}");
			_ = sb.AppendLine($"{nameof(Configuration.DisplayNameLink)}: {Configuration.DisplayNameLink}");
			_ = sb.AppendLine($"{nameof(Configuration.Link1Label)}: {Configuration.Link1Label}");
			_ = sb.AppendLine($"{nameof(Configuration.Link1)}: {Configuration.Link1}");
			_ = sb.AppendLine($"{nameof(Configuration.Link2Label)}: {Configuration.Link2Label}");
			_ = sb.AppendLine($"{nameof(Configuration.Link2)}: {Configuration.Link2}");
			_ = sb.AppendLine($"{nameof(Configuration.Link3Label)}: {Configuration.Link3Label}");
			_ = sb.AppendLine($"{nameof(Configuration.Link3)}: {Configuration.Link3}");
			_ = sb.AppendLine($"{nameof(Configuration.ArchiveExtension)}: {Configuration.ArchiveExtension}");
			_ = sb.AppendLine($"{nameof(Configuration.FileToExecute)}: {Configuration.FileToExecute}");
			_ = sb.AppendLine($"{nameof(Configuration.FirstDisplayImageSize)}: {Configuration.FirstDisplayImageSize}");
			_ = sb.AppendLine($"{nameof(Configuration.SecondDisplayImageSize)}: {Configuration.SecondDisplayImageSize}");
			_ = sb.AppendLine($"{nameof(Configuration.ThirdDisplayImageSize)}: {Configuration.ThirdDisplayImageSize}");
			_ = sb.AppendLine($"{nameof(Configuration.DisplayImageHeightToShowOnUI)}: {Configuration.DisplayImageHeightToShowOnUI}");
			_ = sb.AppendLine($"{nameof(Configuration.ArchiveSize)}: {Configuration.ArchiveSize}");
			_ = sb.AppendLine($"{nameof(Configuration.Comment)}:\n{Configuration.Comment}\n");

			_ = sb.AppendLine(":::: 7z.exe ::::");
			_ = sb.AppendLine("FullName: " + sevenZipProgram.FullName);
			_ = sb.AppendLine("Version: " + sevenZipProgram.Version + "\n");

			Log(sb);
		}
	}

	/// <summary>
	/// Logic related to the manage of 7-Zip (7z.exe), command line version of the program
	/// </summary>
	private void InitializeSevenZipProgram()
	{
		// Creating executable from embedded resources
		sevenZipProgram.FullName = Path.Combine(programInstallationPath, "7z.exe");
		if (!sevenZipProgram.Exists())
		{
			var assembly = GetType().Assembly;
			// This stream should not be closed
			var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.7z.exe");
			sevenZipProgram.Data = new byte[stream!.Length];
			stream.Read(sevenZipProgram.Data, 0, sevenZipProgram.Data.Length);
			sevenZipProgram.CreateFile(true);
		}

		// Checking if there is an updated installed version
		FileInfo installedSevenZipProgramFI;
		if ((installedSevenZipProgramFI = new ("C:\\Program Files\\7-Zip\\7z.exe")).Exists ||
			(installedSevenZipProgramFI = new ("C:\\Program Files (x86)\\7-Zip\\7z.exe")).Exists)
		{
			var fvi = FileVersionInfo.GetVersionInfo(installedSevenZipProgramFI.FullName);
			var version = new Version(fvi.ProductMajorPart, fvi.ProductMinorPart);

			// If installed 7-Zip program is an updated version copy to programInstallationPath and replace existing 7z.exe
			if(version.CompareTo(sevenZipProgram.Version).Equals(1))
				installedSevenZipProgramFI.CopyTo(sevenZipProgram.FullName, true);
		}

		sevenZipProgram.InitializeProcess();
	}

	/// <summary>
	/// Load program settings stored in settings.json file located at installation path
	/// </summary>
	private void LoadProgramSettings()
	{
		// settings.json file should exists at this point
		try
		{
			programSettings = JsonConvert.DeserializeObject<ProgramSettings>(File.ReadAllText(programSettingsFilePath))!;
		} catch (Exception ex)
		{
			ShowErrorLoadingSettingsJsonFileMessageBox(ex);
		} finally
		{
			programSettings ??= new();
		}

		ExtractArchive = programSettings.ExtractArchiveIsChecked;
		UseWindowsConsole = programSettings.UseWindowsConsoleIsChecked;
		CloseAfterExtract = programSettings.CloseAfterExtractIsChecked;
		DeleteAfterClose = programSettings.DeleteAfterCloseIsChecked;
		SetCustomArguments = programSettings.SetCustomArgumentsCheckboxIsChecked;
	}

	#region Commands
	/// <summary>
	/// Extract Button Command
	/// </summary>
	/// <returns>A Task object representing the extraction process</returns>
	[RelayCommand(AllowConcurrentExecutions = true, CanExecute = nameof(ExtractCommandCanExecute))]
	public async Task ExtractAsync()
	{
		try
		{
			IsExtracting = true;

			if (SetCustomArguments)
			{
				var arguments = CustomArguments.Replace("%file_path%", programFullName).Replace("%output_path%", ExtractionPath);
				await sevenZipProgram.SendArguments(arguments);
			}
			else
			{
				if (ExtractArchive)
				{
					string archiveName;
					var extension = "";
					if(string.IsNullOrWhiteSpace(Configuration.ArchiveExtension))
					{
						MessageBox.Show("Archive Extension was not specified in configuration file so archive file will be created without an extension. You can upload the file to https://filext.com to find it out", PROJECT_SHORT_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
					}
					else
						extension = Configuration.ArchiveExtension;
					
					if (Configuration.DisplayName!.Equals(PROJECT_NAME))
					{
						var culture = new CultureInfo(CultureInfo.CurrentCulture.Name);
						culture.DateTimeFormat.DateSeparator = "_";
						archiveName = DateTime.Now.ToString("d", culture) + extension;
					}
					else
						archiveName = Configuration.DisplayName + extension;

					archive.FullName = Path.Combine(ExtractionPath, archiveName);
					if (archive.Exists() &&
						MessageBox.Show($"Archive \"{archiveName}\" already exists. Do you want to replace it?", PROJECT_SHORT_NAME, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
					{
						IsExtracting = false;
						return;
					}

					Log($"Extracting {archiveName}... ", false);

					await Task.Run(() =>
					{
						using var fileStreamReader = File.OpenRead(programFullName);
						using var fileStreamWriter = File.OpenWrite(archive.FullName);

						var bufferSize = 50 * 1024 * 1024; //50MB
						var bufferedStream = new BufferedStream(fileStreamReader, bufferSize);
						var reader = new BinaryReader(bufferedStream);
						_ = fileStreamReader.Seek(archive.Index, SeekOrigin.Begin);
						var writter = new BinaryWriter(fileStreamWriter);
						long bytesWrited = 0;
						var bytesToWrite = 0;
						while (bytesWrited < Configuration.ArchiveSize && bufferedStream.CanRead)
						{
							bytesToWrite = (int)Math.Min(bufferSize, Configuration.ArchiveSize - bytesWrited);
							writter.Write(reader.ReadBytes(bytesToWrite));
							bytesWrited += bytesToWrite;

							Progress = (float)bytesWrited / Configuration.ArchiveSize;
						}
					});

					Progress = 0;
					Log("Done");
				}
				else
				{
					// *** programPath is targetFilePath because 7-Zip (7z.exe) is able to locate the merged archive and unzip it 
					await sevenZipProgram.Unzip(programFullName, ExtractionPath);
					if (!string.IsNullOrWhiteSpace(Configuration.FileToExecute))
					{
						var fullPath = Path.Combine(ExtractionPath, Configuration.FileToExecute);
						
						if (File.Exists(fullPath))
						{
							Process.Start(new ProcessStartInfo()
							{
								UseShellExecute = true,
								FileName = fullPath
							});
						}
					}
				}

				if (CloseAfterExtract)
					MainWindowInstance!.Close();
			}

			IsExtracting = false;

		} catch (Exception ex)
		{
			ShowErrorMessageBox(MainWindowInstance!, programSettings.VerboseMessageBox ? ex.ToString() : ex.Message);
		}
	}

	/// <summary>
	/// Browse Button Command
	/// </summary>
	[RelayCommand]
	public void BrowsePath()
	{
		var dlg = new VistaFolderBrowserDialog
		{
			SelectedPath = ExtractionPath,
			ShowNewFolderButton = true
		};

		if (dlg.ShowDialog() is true)
			ExtractionPath = dlg.SelectedPath;
	}

	/// <summary>
	/// Clear Button Command
	/// </summary>
	[RelayCommand]
	public void ClearOutput() => Output = string.Empty;
	#endregion

	/// <summary>
	/// Helper Method to log to Output (Built-in Console)
	/// </summary>
	public void Log(string message, bool appendNewLine = true) => Output += appendNewLine ? message + Environment.NewLine : message;
	/// <summary>
	/// Helper Method to log to Output (Built-in Console)
	/// </summary>
	public void Log(object object_, bool appendNewLine = true) => Log(object_.ToString()!, appendNewLine);

	// View Event Handlers bind using xaml Behaviours... !!! They will be referenced at runtime Don't let Visual Studio to delete them !!!
	#region View Event Handlers
	public void OnWindowLoaded()
	{
		try
		{
			programFullName = Environment.ProcessPath!;

			LoadProgramSettings();

			if (new FileInfo(programFullName).Length.Equals(PROGRAM_LENGTH))
				throw new Exception($"Make sure to merge at least the configuration file content and the archive file content\n\nHomepage: {GITHUB_REPOSITORY_LINK}");

			Initialize();
		} catch (Exception ex)
		{
			ShowErrorMessageBox(MainWindowInstance!, (programSettings ??= new()).VerboseMessageBox ? ex.ToString() : ex.Message);
		}
	}

	public void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (DeleteAfterClose)
		{
			_ = Process.Start(new ProcessStartInfo()
			{
				FileName = "cmd.exe",
				Arguments = $@"/c timeout /t 2 /nobreak & del ""{programFullName}"" /f /q",
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
		}

		//FileSystem.DeleteFile(programPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
	}

	public void OnDisplayNameTextBlock_MouseLeftButtonDown() =>
		OpenLink(Configuration.DisplayNameLink!);

	public void OnLink1TextBlock_MouseLeftButtonDown() =>
		OpenLink(Configuration.Link1!);

	public void OnLink2TextBlock_MouseLeftButtonDown() =>
		OpenLink(Configuration.Link2!);

	public void OnLink3TextBlock_MouseLeftButtonDown() =>
		OpenLink(Configuration.Link3!);

	public void OnOdellTextBlock_MouseLeftButtonDown() =>
		OpenLink("https://instagram.com/odell0111");

	public void OnGitHubLogo_MouseLeftButtonDown() =>
		OpenLink(GITHUB_REPOSITORY_LINK);

	public void OnOutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		var tb = (TextBox)sender;
		tb.CaretIndex = tb.Text.Length;
		tb.Focus();
		tb.ScrollToEnd();
	}

	public void OnUseWindowConsoleUnchecked()
	{
		if (!programSettings.ShowBuiltInConsoleWarning)
			return;

		// Avoiding showing warning next time
		// ** At this point should not have problems loading settings.json file, but just in case
		try
		{
			if (File.Exists(programSettingsFilePath))
				programSettings = JsonConvert.DeserializeObject<ProgramSettings>(File.ReadAllText(programSettingsFilePath))!;

			programSettings!.ShowBuiltInConsoleWarning = false;
			File.WriteAllText(programSettingsFilePath, JsonConvert.SerializeObject(programSettings, Formatting.Indented));

		} catch (Exception ex)
		{
			ShowErrorLoadingSettingsJsonFileMessageBox(ex);
		}

		MessageBox.Show(
			"You may not be able to see the extraction progress if you use the built-in console\n\n" +
			"To send input check 'Set Custom Arguments' checkbox, type the input, and click Send Arguments Button\n\n" +
			"If program is waiting for input and available input options line is not displayed, it may be one of this two:\n" +
			"? (Y)es / (N)o / (A)lways / (S)kip all / A(u)to rename all / (Q)uit?\n" +
			"Enter password (will not be echoed):\n\n\n" +
			"Sorry for any inconvenience this may cause, I couldn't make a better implementation for this built-in console. If you're a developer and want to contribute, click the GitHub logo to open the project repository on GitHub", PROJECT_SHORT_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
	}
	#endregion
}