namespace EvtxReader.Exceptions
{
	public class EventDataOutsideOfBufferException : RecoverableException
	{
		public EventDataOutsideOfBufferException(string message, long chunkOffset, int nextRecordOffset)
			: base(message, chunkOffset, nextRecordOffset)
		{
		}
	}
}
