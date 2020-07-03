namespace EvtxReader.Exceptions
{
	using System;

	public class TruncatedFileException : Exception
    {
		public TruncatedFileException(string message)
			: base(message)
		{
		}
    }
}
