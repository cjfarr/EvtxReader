namespace EvtxReader.BinaryXml
{
	using System;

	internal class BinaryXmlTemplate
	{
		public Guid TemplateGuid
		{
			get;
			set;
		}

		public int ChunkOffset
		{
			get;
			set;
		}

		public int Size
		{
			get;
			set;
		}

		public BinaryXmlElement RootElement
		{
			get;
			set;
		}
	}
}
