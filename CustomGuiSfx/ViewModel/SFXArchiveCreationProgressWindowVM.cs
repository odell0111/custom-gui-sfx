using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

using CustomGuiSfx.View;
using CustomGuiSfx.Model;

using static CustomGuiSfx.App;
using static CustomGuiSfx.Model.Configuration;

namespace CustomGuiSfx.ViewModel;
public partial class SFXCreationProgressWindowVM : ObservableObject
{
	[ObservableProperty]
	private double progress;
	[ObservableProperty]
	private string sfxName;
	[ObservableProperty]
	private string archiveDirectoryPath;
	[ObservableProperty]
	private bool moveSourceFilesToRecycleBin;

	private event Action Closing;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public SFXCreationProgressWindowVM()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new()))
		{
			Progress = 0.68;
			SfxName = "MySFX.exe";
			ArchiveDirectoryPath = "C:\\ProgramFiles";
			MoveSourceFilesToRecycleBin = true;
			return;
		}

		LoadProgramSettings();
	}

	private async Task CreateSFXFromArguments()
	{
		// Getting Command Line arguments, Skipping first one (Program executable/Main dll path) since it's not necessary
		var args = Environment.GetCommandLineArgs().Skip(1);
		var programFullName = Environment.ProcessPath!;

		// Calculating final executable size since Windows doesn't run executables larger than 4,294,967,295 bytes (4GiB - 1B)
		{
			var size = new FileInfo(programFullName).Length;
			foreach (var arg in args)
			{
				if (!File.Exists(arg))
					continue;

				size += new FileInfo(arg).Length;
			}

			if (size > 4294967295) // 4GiB - 1B = 4,294,967,295 bytes
			{
				Helpers.HelperMethods.ShowErrorMessageBox(SFXCreationProgressWindow.Instance!, $"Final executable size can't be larger than 4,294,967,295 bytes (4GiB - 1B) because it won't run. Final executable size if proceed with this parameters: {size:N0} bytes");
				return;
			}
		}

		string? configurationFilePath = null,
				archivePath = null,
				firstDisplayImagePath = null,
				secondDisplayImagePath = null,
				thirdDisplayImagePath = null;

		foreach (var arg in args)
		{
			if (!File.Exists(arg))
				continue;

			Closing += () =>
			{
				if (File.Exists(arg))
					FileSystem.DeleteFile(arg, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
			};

			var ext = Path.GetExtension(arg).ToLower();
			switch (ext)
			{
				case ".jpg":
				case ".jpeg":
				case ".png":
				case ".gif":
					if (firstDisplayImagePath is null)
						firstDisplayImagePath = arg;
					else if (secondDisplayImagePath is null)
						secondDisplayImagePath = arg;
					else if (thirdDisplayImagePath is null)
						thirdDisplayImagePath = arg;
					break;

				case ".txt":
					configurationFilePath = arg; break;

				// 7-Zip Supported Formats (Unpacking)
				case ".7z":
				case ".apfs":
				case ".ar":
				case ".arj":
				case ".bz2":
				case ".cab":
				case ".chm":
				case ".cpio":
				case ".cramfs":
				case ".dmg":
				case ".ext":
				case ".fat":
				case ".gpt":
				case ".gz":
				case ".hfs":
				case ".hex":
				case ".iso":
				case ".lzh":
				case ".lzma":
				case ".mbr":
				case ".msi":
				case ".nsi":
				case ".ntfs":
				case ".qcow2":
				case ".rar":
				case ".rpm":
				case ".squashfs":
				case ".squ":
				case ".sfs":
				case ".sqf":
				case ".tar":
				case ".udf":
				case ".uefi":
				case ".vdi":
				case ".vhd":
				case ".vhdx":
				case ".vmdk":
				case ".wim":
				case ".xar":
				case ".xz":
				case ".z":
				case ".zip":
					archivePath = arg; break;
			}
		}

		if (archivePath is null)
			throw new Exception("Archive file format is not a 7-Zip supported format for unpacking, it was not passed or it doesn't exist on passed path");

		var archiveName = Path.GetFileNameWithoutExtension(archivePath);
		SFXCreationProgressWindow.Instance!.Title = $"{archiveName}.exe Creation Progress Window";

		var sfxPath = Path.Combine(Path.GetDirectoryName(archivePath)!, $"{archiveName}.exe");

		if (configurationFilePath is not null && new FileInfo(configurationFilePath).Length > CONFIGURATION_FILE_MAX_SIZE)
		{
			Helpers.HelperMethods.ShowErrorMessageBox(SFXCreationProgressWindow.Instance, $"Configuration file size must be less than or equal to {CONFIGURATION_FILE_MAX_SIZE / 1024}KB");
			return;
		}

		if (File.Exists(sfxPath))
		{
			var result = MessageBox.Show($"SFX \"{archiveName}.exe\" already exists, do you want to replace it?", PROJECT_SHORT_NAME, MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (result.Equals(MessageBoxResult.No))
			{
				SFXCreationProgressWindow.Instance.Close();
				return;
			}
		}

		SfxName = Path.GetFileName(sfxPath);
		ArchiveDirectoryPath = Path.GetDirectoryName(sfxPath)!;
		File.Copy(programFullName, sfxPath, true);



		// Merging Configuration file
		try
		{
			using var fs = new FileStream(sfxPath, FileMode.Append);
			var bw = new BinaryWriter(fs);

			var archiveSize = new FileInfo(archivePath).Length;
			var archiveExtension = Path.GetExtension(archivePath);
			var firstDisplayImageSize = firstDisplayImagePath is null ? 0 : new FileInfo(firstDisplayImagePath).Length;
			var secondDisplayImageSize = secondDisplayImagePath is null ? 0 : new FileInfo(secondDisplayImagePath).Length;
			var thirdDisplayImageSize = thirdDisplayImagePath is null ? 0 : new FileInfo(thirdDisplayImagePath).Length;

			// If configuration file was not passed create a new one
			if (configurationFilePath is null)
			{
				var configurationContent = new StringBuilder();
				configurationContent.AppendLine(CFS);
				configurationContent.AppendLine($"{nameof(DisplayName)}={archiveName}");
				configurationContent.AppendLine($"{nameof(ArchiveSize)}={archiveSize}");
				configurationContent.AppendLine($"{nameof(ArchiveExtension)}={archiveExtension}");

				if (firstDisplayImagePath is not null)
				{
					configurationContent.AppendLine($"{nameof(FirstDisplayImageSize)}={firstDisplayImageSize}");
				}
				if (secondDisplayImagePath is not null)
				{
					configurationContent.AppendLine($"{nameof(SecondDisplayImageSize)}={secondDisplayImageSize}");
				}
				if (thirdDisplayImagePath is not null)
				{
					configurationContent.AppendLine($"{nameof(ThirdDisplayImageSize)}={thirdDisplayImageSize}");
				}
				configurationContent.Append(CFE);

				bw.Write(Encoding.UTF8.GetBytes(configurationContent.ToString()));
			}
			else // fix current Configuration Entries
			{
				var configuration = File.ReadAllText(configurationFilePath);

				// Fix entry with name 'entryName' in case of having a value different to 'entryValue'
				void fixEntry(string entryName, object entryValue, bool addIfMissing = false)
				{
					var r = new Regex($"^{entryName}=(?<value>.*)", RegexOptions.Multiline);
					var m = r.Match(configuration!);
					var value = entryValue.ToString();

					if (!m.Success)
					{
						if (addIfMissing)
							configuration = configuration.Replace(CFS, $"{CFS}{Environment.NewLine}{entryName}={value}");
					}
					else if (!m.Groups["value"].Value.Equals(value))
						configuration = r.Replace(configuration!, $"{entryName}={value}");
				}

				fixEntry(nameof(ArchiveSize), archiveSize, true);
				fixEntry(nameof(ArchiveExtension), archiveExtension, true);
				fixEntry(nameof(FirstDisplayImageSize), firstDisplayImageSize);
				fixEntry(nameof(SecondDisplayImageSize), secondDisplayImageSize);
				fixEntry(nameof(ThirdDisplayImageSize), thirdDisplayImageSize);

				bw.Write(Encoding.UTF8.GetBytes(configuration));
			}

			//Merging display images, if any were passed
			if (firstDisplayImagePath is not null)
				bw.Write(File.ReadAllBytes(firstDisplayImagePath));
			if (secondDisplayImagePath is not null)
				bw.Write(File.ReadAllBytes(secondDisplayImagePath));
			if (thirdDisplayImagePath is not null)
				bw.Write(File.ReadAllBytes(thirdDisplayImagePath));

			// Merging archive
			await Task.Run(() =>
			{
				using var fileStreamReader = File.OpenRead(archivePath);

				var bufferSize = 50 * 1024 * 1024; //50MB
				var bufferedStream = new BufferedStream(fileStreamReader, bufferSize);
				var reader = new BinaryReader(bufferedStream);
				long bytesWrited = 0;
				var bytesToWrite = 0;
				while (bytesWrited < archiveSize && bufferedStream.CanRead)
				{
					bytesToWrite = (int)Math.Min(bufferSize, archiveSize - bytesWrited);
					bw.Write(reader.ReadBytes(bytesToWrite));
					bytesWrited += bytesToWrite;

					Progress = (float)bytesWrited / archiveSize;
				}
			});

		} catch (Exception)
		{
			if (File.Exists(sfxPath))
				File.Delete(sfxPath);
			throw;
		}

		await Task.Delay(2560);
		SFXCreationProgressWindow.Instance.Close();
	}

	private void LoadProgramSettings()
	{
		// settings.json file should exists at this point
		try
		{
			MoveSourceFilesToRecycleBin = JsonConvert.DeserializeObject<ProgramSettings>(File.ReadAllText(programSettingsFilePath))!.MoveSourceFilesToRecycleBinIsChecked;
		} catch (Exception ex)
		{
			Helpers.HelperMethods.ShowErrorLoadingSettingsJsonFileMessageBox(ex);
		}
	}

	// View Event Handlers bind using xaml Behaviours... !!! They will be referenced at runtime Don't let Visual Studio to delete them !!!
	#region Event Handlers
	public async void OnWindowLoaded()
	{
		try
		{
			await CreateSFXFromArguments();
		} catch (Exception ex)
		{
			Helpers.HelperMethods.ShowErrorMessageBox(SFXCreationProgressWindow.Instance!, ex.Message);
		}
	}

	public void OnClosing()
	{
		if (MoveSourceFilesToRecycleBin)
			Closing?.Invoke();
	}

	public void OnArchiveDirectoryPath_MouseLeftButtonDown()
	{
		Process.Start(new ProcessStartInfo()
		{
			UseShellExecute = true,
			FileName = ArchiveDirectoryPath
		});
	}
	#endregion
}
