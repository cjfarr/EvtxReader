namespace EvtxReader.BinaryXml
{
	internal class FragmentHeader
	{
		public const int Signature = 65807;

		private int offset;

		public FragmentHeader(int offset)
		{
			this.offset = offset;
		}

		public int TemplateId
		{
			get;
			set;
		}

		public int TemplateDefinitionOffset
		{
			get;
			set;
		}

		public bool ContainsTemplateDefinition
		{
			get
			{
				return this.TemplateDefinitionOffset > this.offset;
			}
		}
	}
}
