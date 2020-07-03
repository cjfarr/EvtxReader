namespace EvtxReader
{
	using System.Windows;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class EvtxReaderApplication : Application
	{
		public StartupEventArgs StartupArgs
		{
			get;
			private set;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			this.StartupArgs = e;
			base.OnStartup(e);
		}

		[System.STAThreadAttribute()]
		[System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
		public static void Main()
		{
			EvtxReaderApplication app = new EvtxReaderApplication();
			app.InitializeComponent();
			app.Run();
		}
	}
}
