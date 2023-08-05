using System.Runtime.InteropServices;

namespace CustomGuiSfx.ViewModel.Helpers;
internal static class PInvoke
{
	public const int ATTACH_PARENT_PROCESS = -1;

	/// <summary>
	/// Attaches the calling process to the console of the specified process as a client application
	/// </summary>
	/// <param name="processId"></param>
	/// <returns></returns>
	[DllImport("Kernel32.dll")]
	public static extern bool AttachConsole(int processId);

	/// <summary>
	/// Detaches the calling process from its console
	/// </summary>
	/// <returns></returns>
	[DllImport("kernel32.dll")]
	public static extern bool FreeConsole();
}
