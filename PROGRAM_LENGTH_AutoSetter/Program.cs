using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PROGRAM_LENGTH_AutoSetter;

internal class Program
{
	// Path relatives to Solution File Directory
	private static readonly string appXamlCsFilePath = "CustomGuiSfx/App.xaml.cs";
	private static readonly string programSingleExecutablePath = "CustomGuiSfx/bin/Release/net6.0-windows7.0/publish/win-x64/CustomGuiSfx.exe";

	static Program()
	{
		try
		{
			var solutionDirectory = GetSolutionDirectory(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName);
			appXamlCsFilePath = Path.Combine(solutionDirectory, appXamlCsFilePath);
			programSingleExecutablePath = Path.Combine(solutionDirectory, programSingleExecutablePath);
		} catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			Environment.Exit(-1);
		}
	}

	static void Main()
	{
		if (!File.Exists(programSingleExecutablePath))
		{
			Console.WriteLine("Single File seems to be build yet");
			return;
		}

		try
		{
			CheckValue();
		} catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
			Environment.Exit(-1);
		}
	}

	private static void CheckValue()
	{
		//public const int PROGRAM_LENGTH = 2883591;
		var appXamlCsContent = File.ReadAllText(appXamlCsFilePath);
		var r = new Regex("public const int PROGRAM_LENGTH\\s?=\\s?(?<value>\\d+)");
		var m = r.Match(appXamlCsContent);
		if (!m.Success)
		{
			Console.WriteLine("App.PROGRAM_LENGTH couldn't be found");
			Thread.Sleep(1310);
			Environment.ExitCode = -1;
		}

		var PROGRAM_LENGTH_VariableValue = long.Parse(m.Groups["value"].Value);
		var singleExecutableSize = new FileInfo(programSingleExecutablePath).Length;
		if (!PROGRAM_LENGTH_VariableValue.Equals(singleExecutableSize))
		{
			Console.WriteLine($"Single Executable File Size has changed ({singleExecutableSize}), updating value...");
			appXamlCsContent = r.Replace(appXamlCsContent, "public const int PROGRAM_LENGTH = " + singleExecutableSize);
			File.WriteAllText(appXamlCsFilePath, appXamlCsContent);
			Console.WriteLine("Value Update!... Now trying to re-publish");
		}
		else
		{
			Console.WriteLine($"No need to update App.PROGRAM_LENGTH = {PROGRAM_LENGTH_VariableValue} :)");
		}
	}

	private static string GetSolutionDirectory(string directory)
	{
		var files = Directory.GetFiles(directory, "*.sln");
		if (files.Length.Equals(1))
			return directory;

		return GetSolutionDirectory(Directory.GetParent(directory)!.FullName);
	}
}
