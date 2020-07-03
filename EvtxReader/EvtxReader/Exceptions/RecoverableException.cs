namespace EvtxReader.Exceptions
{
	using System;

	public abstract class RecoverableException : Exception
    {
		public RecoverableException(string message, long chunkOffset, int nextRecordOffset)
			:base(message)
		{
			this.RecoveryChunkOffset = chunkOffset;
			this.RecoveryRecordOffset = nextRecordOffset;
		}

		public long RecoveryChunkOffset
		{
			get;
			private set;
		}

		public int RecoveryRecordOffset
		{
			get;
			private set;
		}
    }
}
