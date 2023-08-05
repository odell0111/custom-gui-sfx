using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomGuiSfx.Model;
internal class ProgramSettings
{
	public bool VerboseMessageBox { get; set; } = false;
	public bool LogToOutput { get; set; } = false;
	public bool SaveDisplayImagesToDesktop { get; set; } = false;
	public bool ShowBuiltInConsoleWarning { get; set; } = true;

	// Checkboxes State
	// MainWindow.xaml
	public bool ExtractArchiveIsChecked { get; set; } = false;
	public bool UseWindowsConsoleIsChecked { get; set; } = true;
	public bool CloseAfterExtractIsChecked { get; set; } = true;
	public bool DeleteAfterCloseIsChecked { get; set; } = false;
	public bool SetCustomArgumentsCheckboxIsChecked { get; set; } = false;
	// SFXCreationProgressWindow.xaml
	public bool MoveSourceFilesToRecycleBinIsChecked { get; set; } = false;
}
