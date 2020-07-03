namespace EvtxReader.BinaryXml
{
	internal class BinaryXmlAttribute : IValueContainer
	{
		public string Name
		{
			get;
			set;
		}

		public string Value
		{
			get;
			set;
		}

		public short ValueIndex
		{
			get;
			set;
		}

		public BinaryValueType ValueType
		{
			get;
			set;
		}
	}
}
