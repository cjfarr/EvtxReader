namespace EvtxReader.BinaryXml
{
	internal enum BinaryValueType
	{
		Null = 0x00,
		Utf16String = 0x01,
		AsciiString = 0x02,
		Byte = 0x03,
		UByte = 0x04,
		Int16 = 0x05,
		UInt16 = 0x06,
		Int32 = 0x07,
		UInt32 = 0x08,
		Int64 = 0x09,
		UInt64 = 0x0A,
		Real32 = 0x0B,
		Real64 = 0x0C,
		Bool = 0x0D,
		Binary = 0x0E,
		Guid = 0x0F,
		SizeT = 0x10,
		FileTime = 0x11,
		SysTime = 0x12,
		Sid = 0x13,
		HexInt32 = 0x14,
		HexInt64 = 0x15,
		BinXmlType = 0x21,

		Utf16StringArray = 0x81
	};
}
