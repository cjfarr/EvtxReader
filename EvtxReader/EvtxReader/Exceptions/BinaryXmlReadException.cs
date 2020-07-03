namespace EvtxReader.Exceptions
{
	public class BinaryXmlReadException : RecoverableException
	{
		public BinaryXmlReadException(string message, long chunkOffset, int nextRecordOffset) 
			: base(message, chunkOffset, nextRecordOffset)
		{
		}
	}
}
