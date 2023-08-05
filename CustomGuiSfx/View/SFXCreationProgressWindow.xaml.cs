using System.Windows;

namespace CustomGuiSfx.View;

public partial class SFXCreationProgressWindow : Window
{
	public static SFXCreationProgressWindow? Instance { get; set; }
	public SFXCreationProgressWindow()
	{
		Instance = this;
		InitializeComponent();
	}
}

