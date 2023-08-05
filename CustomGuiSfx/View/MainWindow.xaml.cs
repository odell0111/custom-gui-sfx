using System.Windows;

namespace CustomGuiSfx.View;

public partial class MainWindow : Window
{
	public static MainWindow? MainWindowInstance { get; set; }
	public MainWindow()
	{
		MainWindowInstance = this;
		InitializeComponent();
	}
}