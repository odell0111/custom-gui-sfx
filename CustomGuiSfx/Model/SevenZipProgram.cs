using System;
using System.Diagnostics;
using System.Threading.Tasks;

using static CustomGuiSfx.ViewModel.CustomGuiSfxVM;

namespace CustomGuiSfx.Model;

public class SevenZipProgram : BinaryMergedFile
{
	public Process? process;

	private bool isProcessRunning = false;
	public void InitializeProcess()
	{
		process = new Process();
		process.StartInfo.FileName = FullName;
		process.EnableRaisingEvents = true;
		process.OutputDataReceived += WriteOutput;
		process.ErrorDataReceived += WriteOutput;
		process.Exited += (s, e) => isProcessRunning = false;
	}

	private void WriteOutput(object sender, DataReceivedEventArgs e) => CustomGuiSfxVMInstance!.Log(e.Data!);

	public async Task SendArguments(string arguments)
	{
		if(process is null)
			InitializeProcess();

		if (isProcessRunning)
		{
			process!.StandardInput.Write(arguments);
			process.StandardInput.WriteLine();
			process.StandardInput.Flush();
			return;
		}

		isProcessRunning = true;

		process!.StartInfo.Arguments = arguments;

		var isNotUsingWindowsConsole = !CustomGuiSfxVMInstance!.UseWindowsConsole;
		process.StartInfo.RedirectStandardInput = isNotUsingWindowsConsole;
		process.StartInfo.UseShellExecute = !isNotUsingWindowsConsole;
		process.StartInfo.RedirectStandardOutput = isNotUsingWindowsConsole;
		process.StartInfo.RedirectStandardError = isNotUsingWindowsConsole;
		process.StartInfo.CreateNoWindow = isNotUsingWindowsConsole;


		await Task.Run(() =>
		{
			_ = process.Start();
			if (isNotUsingWindowsConsole)
			{
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
			}

			process.WaitForExit();
		});

		if (isNotUsingWindowsConsole)
		{
			process.CancelErrorRead();
			process.CancelOutputRead();
		}
	}

	public async Task Unzip(string filePath, string outputPath) => await SendArguments($"x \"{filePath}\" -o\"{outputPath}\"");
}
