namespace EvtxReader.Exceptions
{
	public class FragmentHeaderNotFoundException : RecoverableException
	{
		public FragmentHeaderNotFoundException(string message, long chunkOffset, int nextRecordOffset) 
			: base(message, chunkOffset, nextRecordOffset)
		{
		}
	}
}
