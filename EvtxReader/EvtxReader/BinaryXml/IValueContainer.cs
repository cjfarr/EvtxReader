namespace EvtxReader.BinaryXml
{
	internal interface IValueContainer
	{
		string Name
		{
			get;
			set;
		}

		string Value
		{
			get;
			set;
		}

		short ValueIndex
		{
			get;
			set;
		}

		BinaryValueType ValueType
		{
			get;
			set;
		}
	}
}
