namespace EvtxReader.BinaryXml
{
	internal class ValueDescriptor
	{
		public BinaryValueType Type
		{
			get;
			set;
		}

		public int Size
		{
			get;
			set;
		}

		public int Offset
		{
			get;
			set;
		}
	}
}
