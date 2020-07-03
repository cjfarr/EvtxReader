namespace EvtxReader.BinaryXml
{
	using System.Collections.Generic;

	internal class BinaryXmlElement : IValueContainer
	{
		internal BinaryXmlElement()
		{
			this.Children = new List<BinaryXmlElement>();
			this.Attributes = new List<BinaryXmlAttribute>();
		}

		public int Offset
		{
			get;
			set;
		}

		public int Length
		{
			get;
			set;
		}

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

		public List<BinaryXmlAttribute> Attributes
		{
			get;
			set;
		}

		public List<BinaryXmlElement> Children
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

		public IEnumerable<BinaryXmlElement> EnumerateAllChildren()
		{
			foreach (BinaryXmlElement element in this.Children)
			{
				yield return element;
				foreach (BinaryXmlElement child in element.Children)
				{
					yield return child;
				}
			}
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(this.Name) ? base.ToString() : this.Name;
		}
	}
}
