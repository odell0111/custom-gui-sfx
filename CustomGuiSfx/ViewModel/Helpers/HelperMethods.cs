using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

using CustomGuiSfx.Model;

using static CustomGuiSfx.App;
using static CustomGuiSfx.Model.Configuration;

namespace CustomGuiSfx.ViewModel.Helpers;
public static class HelperMethods
{
	public static bool isThisProcessAttachedToParentConsole = false;
	/// <summary>
	/// This method will attach this process to the console of the parent of this process and write passed message to stream output
	/// </summary>
	/// <param name="freeConsole">If true this process will be detached from its console</param>
	public static void WriteToConsole(string message, bool appendNewLine = true, bool freeConsole = true)
	{
		if(!isThisProcessAttachedToParentConsole)
		{
			PInvoke.AttachConsole(PInvoke.ATTACH_PARENT_PROCESS);
			isThisProcessAttachedToParentConsole = true;
		}

		if (appendNewLine)
			Console.WriteLine(message);
		else
			Console.Write(message);

		if (freeConsole && isThisProcessAttachedToParentConsole)
		{
			System.Windows.Forms.SendKeys.SendWait("{ENTER}");
			PInvoke.FreeConsole();
			isThisProcessAttachedToParentConsole = false;
		}
	}

	/// <summary>
	/// Open given link in default browser
	/// </summary>
	/// <param name="link"></param>
	public static void OpenLink(string link)
	{
		_ = Process.Start(new ProcessStartInfo()
		{
			UseShellExecute = true,
			FileName = link
		});
	}

	/// <summary>
	/// Helper method to show an error Message Box
	/// </summary>
	/// <param name="owner">
	/// MessageBox owner Window... If closeAppAfterError is true this method will call owner.Close() method after MessageBox closes
	/// </param>
	/// <param name="closeAppAfterError">
	/// If true this method will call owner.Close() method after MessageBox closes
	/// </param>
	public static void ShowErrorMessageBox(Window owner, string messageBoxText, bool closeAppAfterError = true)
	{
		_ = MessageBox.Show(owner, messageBoxText, PROJECT_SHORT_NAME + " - ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

		if (closeAppAfterError)
			owner.Close();
	}

	/// <summary>
	/// Helper method to show an error Message Box
	/// </summary>
	public static void ShowErrorMessageBox(string messageBoxText)
	{
		_ = MessageBox.Show(messageBoxText, PROJECT_SHORT_NAME + " - ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
	}

	/// <summary>
	/// Helper method to show an error Message Box when loading program settings from settings.json file located at installation path
	/// </summary>
	/// <param name="ex"></param>
	public static void ShowErrorLoadingSettingsJsonFileMessageBox(Exception ex)
	{
		MessageBox.Show($"settings.json file couldn't be deserialized. Cause: {ex.Message}", PROJECT_SHORT_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

		if (!File.Exists(programSettingsFilePath))
			return;

		var result = MessageBox.Show("Delete the file?", PROJECT_SHORT_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (result.Equals(MessageBoxResult.Yes))
			File.Delete(programSettingsFilePath);
	}

	/// <summary>
	/// Method to parse the content of the configuration file merged into the GUI Self-Extracting archive
	/// </summary>
	/// <exception cref="Exception"></exception>
	public static void ParseConfiguration(string content)
	{
		// Checking content format in a lazy way
		if (!content[..CFS.Length].Equals(CFS))
			throw new FormatException($"Configuration file start line \"{CFS}\" couldn't be found at the begining of the parsed content, either the content is not well formatted or an internal logic error parsing the data occurred");

		// Search for Comment
		var r = new Regex($"(?<=Comment=).*(?={CFE})", RegexOptions.Singleline | RegexOptions.Multiline);
		var m = r.Match(content);
		if (m.Success)
			Comment = m.Value.Trim();

		var lines = content.Replace("\r", "").Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			if (!line.Contains('='))
				continue;
			var lineContent = line.Split('=');
			if (lineContent.Length < 2)
				continue;

			var value = lineContent[1].Trim();

			// Maybe a recursive logic for this part will be great, do you want to contribute to this project?
			switch (lineContent[0].Trim())
			{
				case "DisplayName": DisplayName = value; break;
				case "DisplayNameLink": DisplayNameLink = value; break;
				case "Link1": Link1 = value; break;
				case "Link2": Link2 = value; break;
				case "Link3": Link3 = value; break;
				case "Link1Label": Link1Label = value; break;
				case "Link2Label": Link2Label = value; break;
				case "Link3Label": Link3Label = value; break;
				case "ArchiveExtension":
					if (!value[0].Equals('.'))
						ArchiveExtension = ".";
					ArchiveExtension += value; break;
				case "FileToExecute": FileToExecute = value; break;
				case "FirstDisplayImageSize": if (int.TryParse(value, out var v1)) FirstDisplayImageSize = v1; break;
				case "SecondDisplayImageSize": if (int.TryParse(value, out var v2)) SecondDisplayImageSize = v2; break;
				case "ThirdDisplayImageSize": if (int.TryParse(value, out var v3)) ThirdDisplayImageSize = v3; break;
				case "DisplayImageHeightToShowOnUI": if (int.TryParse(value, out var v7)) DisplayImageHeightToShowOnUI = v7; break;
				case "ArchiveSize": if (int.TryParse(value, out var v8)) ArchiveSize = v8; break;
				default: break;
			}
		}
	}

	/// <summary>
	/// Method to parse the content of the configuration file merged into the GUI Self-Extracting archive
	/// </summary>
	public static void ParseConfiguration(BinaryMergedFile bmf) => ParseConfiguration(bmf.Data!);

	/// <summary>
	/// Method to parse the content of the configuration file merged into the GUI Self-Extracting archive
	/// </summary>
	public static void ParseConfiguration(byte[] content) => ParseConfiguration(Encoding.UTF8.GetString(content));

	/// <summary>
	/// Method to find out the position of a given array of bytes into a FileStream object
	/// </summary>
	/// <returns>
	/// The position within the FileStream object where the bytesToFind array was found, and the bytes read as an array of bytes
	/// </returns>
	public static (long, byte[]) FindBytes(FileStream fs, byte[] bytesToFind, long maxCount, bool keepInitialStreamPosition = true)
	{
		var index = -1L;
		int currentByte;
		var matchedBytes = 0;
		var initialStreamPosition = fs.Position;
		var count = 0;
		var bytesReaded = new List<byte>();

		while ((currentByte = fs.ReadByte()) != -1 && count < maxCount)
		{
			bytesReaded.Add((byte)currentByte);
			if (currentByte.Equals(bytesToFind[matchedBytes]))
			{
				matchedBytes++;
				if (matchedBytes.Equals(bytesToFind.Length))
				{
					index = fs.Position - matchedBytes;
					break;
				}
			}
			else
				matchedBytes = 0;
		}

		if (keepInitialStreamPosition)
			fs.Position = initialStreamPosition;

		return (index, bytesReaded.ToArray());
	}
}
