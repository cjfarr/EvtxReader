namespace EvtxReader.Exceptions
{
	using System;
	
    public class InvalidElfChnkSignature : Exception
    {
		public InvalidElfChnkSignature(string message)
			:base(message)
		{
		}
    }
}
