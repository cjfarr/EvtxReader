namespace EvtxReader
{
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel viewModel;

		public MainWindow()
		{
			this.InitializeComponent();
			this.DataContext = this.viewModel = new MainWindowViewModel();
			this.eventRecordsGrid.Loaded += this.OnEventRecordsGridLoaded;
		}

		private void OnEventRecordsGridLoaded(object sender, RoutedEventArgs e)
		{
			this.eventRecordsGrid.Loaded -= this.OnEventRecordsGridLoaded;
			EvtxReaderApplication app = Application.Current as EvtxReaderApplication;
			if (app?.StartupArgs?.Args != null)
			{
				this.ValidateAndOpenFileFromArgs(app.StartupArgs.Args);
			}
		}

		private void OnDropExecuted(object sender, DragEventArgs e)
		{
			if (this.viewModel.IsBusy)
			{
				return;
			}

			string[] data = e.Data.GetData(DataFormats.FileDrop, false) as string[];
			this.ValidateAndOpenFileFromArgs(data);
		}

		private void ValidateAndOpenFileFromArgs(string[] args)
		{
			if (args?.Length > 0)
			{
				string file = args[0];
				if (this.viewModel.ValidateFile(file))
				{
					this.viewModel.OpenFile(file);
				}
			}
		}

		private void OnTryRecoveryClick(object sender, RoutedEventArgs e)
		{
			this.viewModel.TryRecovery();
		}
	}
}