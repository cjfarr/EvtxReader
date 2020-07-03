namespace EvtxReader
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using Exceptions;
	using IO;
	using Models;

	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private string windowTitle;
		private bool isBusy;
		private ObservableCollection<EventRecord> eventRecords;
		private BackgroundWorker fileWorker;
		private EventRecord selectedEventRecord;
		private bool showDragDropInstruction;
		private string errorMessage;
		private long recoveryChunkOffset;
		private int recoveryRecordOffset;
		private string currentFile;
		private bool canAttemptRecovery;

		public event PropertyChangedEventHandler PropertyChanged;

		public MainWindowViewModel()
		{
			this.WindowTitle = "Evtx Reader";
			this.errorMessage = string.Empty;
			this.ShowDragDropInstruction = true;
			this.CanAttemptRecovery = false;

			this.recoveryChunkOffset = -1;
			this.recoveryRecordOffset = -1;
		}

		public string WindowTitle
		{
			get
			{
				return this.windowTitle;
			}

			set
			{
				this.windowTitle = value;
				this.RaisePropertyChanged(nameof(this.WindowTitle));
			}
		}

		public bool IsBusy
		{
			get
			{
				return this.isBusy || this.fileWorker?.IsBusy == true;
			}

			set
			{
				this.isBusy = value;
				this.RaisePropertyChanged(nameof(this.IsBusy));
			}
		}

		public ObservableCollection<EventRecord> EventRecords
		{
			get
			{
				return this.eventRecords;
			}

			set
			{
				this.eventRecords = value;
				this.RaisePropertyChanged(nameof(this.EventRecords));
			}
		}

		public EventRecord SelectedEventRecord
		{
			get
			{
				return this.selectedEventRecord;
			}

			set
			{
				this.selectedEventRecord = value;
				this.RaisePropertyChanged(nameof(this.SelectedEventRecord));
				this.RaisePropertyChanged(nameof(this.CurrentRecordMessage));
			}
		}

		public string CurrentRecordMessage
		{
			get
			{
				if (this.SelectedEventRecord != null)
				{
					return this.SelectedEventRecord.Message;
				}

				return string.Empty;
			}
		}

		public bool ShowDragDropInstruction
		{
			get
			{
				return this.showDragDropInstruction;
			}

			set
			{
				this.showDragDropInstruction = value;
				this.RaisePropertyChanged(nameof(this.ShowDragDropInstruction));
			}
		}

		public string ErrorMessage
		{
			get
			{
				return this.errorMessage;
			}
		}

		public bool HasError
		{
			get
			{
				return !string.IsNullOrEmpty(this.ErrorMessage);
			}
		}

		public bool CanAttemptRecovery
		{
			get
			{
				return this.canAttemptRecovery;
			}

			set
			{
				this.canAttemptRecovery = value;
				this.RaisePropertyChanged(nameof(this.CanAttemptRecovery));
			}
		}

		public bool ValidateFile(string file)
		{
			string ext = Path.GetExtension(file).ToLower();
			if (ext != ".evtx")
			{
				return false;
			}

			if (!File.Exists(file))
			{
				return false;
			}

			return true;
		}

		public void OpenFile(string file)
		{
			this.recoveryChunkOffset = -1;
			this.recoveryRecordOffset = -1;

			this.ResetErrorMessage();

			this.currentFile = file;
			this.ShowDragDropInstruction = false;
			if (this.EventRecords == null)
			{
				this.EventRecords = new ObservableCollection<EventRecord>();
			}

			this.EventRecords.Clear();

			if (this.fileWorker == null)
			{
				this.fileWorker = new BackgroundWorker();
				this.fileWorker.WorkerReportsProgress = true;
				this.fileWorker.DoWork += this.OnFileOpenedDoWork;
				this.fileWorker.ProgressChanged += this.OnFileWorkerProgressChanged;
				this.fileWorker.RunWorkerCompleted += this.OnFileWorkerCompleted;
			}

			this.IsBusy = true;
			this.fileWorker.RunWorkerAsync(this.currentFile);
		}

		public void TryRecovery()
		{
			this.ResetErrorMessage();
			this.IsBusy = true;
			this.fileWorker.RunWorkerAsync(this.currentFile);
		}

		private void OnFileOpenedDoWork(object sender, DoWorkEventArgs e)
		{
			string file = e.Argument.ToString();

			bool anyExceptionOccur = false;
			using (EvtxStreamReader reader = new EvtxStreamReader(file, this.recoveryChunkOffset, this.recoveryRecordOffset))
			{
				try
				{
					foreach (EventRecord record in reader.ReadRecords())
					{
						this.fileWorker.ReportProgress(0, record);
					}
				}
				catch (BinaryXmlReadException ex)
				{
					anyExceptionOccur = true;
					this.ProcessRecoverableException(ex);
				}
				catch (FragmentHeaderNotFoundException ex)
				{
					anyExceptionOccur = true;
					this.ProcessRecoverableException(ex);
				}
				catch (InvalidDataException ex)
				{
					anyExceptionOccur = true;
					this.ProcessUnRecoverableException(ex);
				}
				catch (Exception ex)
				{
					anyExceptionOccur = true;
					this.ProcessUnRecoverableException(ex);
				}
			}

			if (!anyExceptionOccur)
			{
				this.recoveryChunkOffset = -1;
				this.recoveryRecordOffset = -1;
			}
		}

		private void ProcessRecoverableException(RecoverableException exception)
		{
			this.errorMessage = exception.Message;
			this.recoveryChunkOffset = exception.RecoveryChunkOffset;
			this.recoveryRecordOffset = exception.RecoveryRecordOffset;
		}

		private void ProcessUnRecoverableException(Exception exception)
		{
			this.errorMessage = exception.Message;
			this.recoveryChunkOffset = -1;
			this.recoveryRecordOffset = -1;
		}

		private void OnFileWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			////This is on the UI thread, so we don't need to worry about Dispatcher.Invoke here
			EventRecord record = e.UserState as EventRecord;
			this.EventRecords.Add(record);
			this.WindowTitle = "Evtx Reader " + this.EventRecords.Count + " Records Found";
		}

		private void OnFileWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.IsBusy = false;
			this.RaisePropertyChanged(nameof(this.ErrorMessage));
			this.RaisePropertyChanged(nameof(this.HasError));

			this.CanAttemptRecovery = this.recoveryRecordOffset > -1;
			this.ShowDragDropInstruction = this.EventRecords.Count <= 0;
		}

		private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (!string.IsNullOrEmpty(propertyName))
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private void ResetErrorMessage()
		{
			this.errorMessage = string.Empty;
			this.RaisePropertyChanged(nameof(this.ErrorMessage));
			this.RaisePropertyChanged(nameof(this.HasError));
		}
	}
}
