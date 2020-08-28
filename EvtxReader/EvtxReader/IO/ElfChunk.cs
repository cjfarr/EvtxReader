namespace EvtxReader.IO
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Exceptions;
	using Models;
	using BinaryXml;

	public class ElfChunk
    {
        public const long MaxLength = 65536;
        public const long HeaderSize = 512;
		public const long HeaderSignature = 30239216594480197;

		private const int recordSignature = 10794;

        private long fileOffset;
		private byte[] buffer;
		private byte[] guidBuffer;
		private Dictionary<int, BinaryXmlTemplate> templates;
		private UInt64 lastRecordId;

		public ElfChunk(byte[] buffer, long fileOffset)
        {
			this.buffer = buffer;
			this.lastRecordId = BitConverter.ToUInt64(this.buffer, 32);

			this.guidBuffer = new byte[16];
			this.fileOffset = fileOffset;
			this.templates = new Dictionary<int, BinaryXmlTemplate>();
        }

		public long FileOffset
		{
			get
			{
				return this.fileOffset;
			}
		}

        public IEnumerable<EventRecord> ReadEventRecords(int startingOffset)
        {
			int offset = startingOffset;

            while (offset < this.buffer.Length)
            {
                ////verify header
                int signature = BitConverter.ToInt32(this.buffer, offset);
                if (signature != recordSignature)
                {
                    break;
                }

                EventRecord record = new EventRecord();
				record.ChunkOffset = offset;
				offset += 4;
				record.BlockSize = BitConverter.ToInt32(this.buffer, offset);

				offset += 4;
                record.RecordId = BitConverter.ToUInt64(this.buffer, offset);

				offset += 8;
                record.RecordTime = DateTime.FromFileTime(BitConverter.ToInt64(this.buffer, offset));

				offset += 8;
				FragmentHeader parentFragmentHeader = this.ReadFragmentHeader(record, ref offset);

				BinaryXmlTemplate template = null;
				if (parentFragmentHeader.ContainsTemplateDefinition)
				{
					template = this.ReadBinaryXmlTemplateRecord(parentFragmentHeader, record, ref offset);					
					int recordOffset = offset;

					try
					{
						template.RootElement = this.ReadBinaryXmlElement(ref recordOffset);
						offset = recordOffset;
					}
					catch (Exception ex)
					{
						throw new BinaryXmlReadException(string.Format("Problem reading BinaryXml for record at offset {0} in ElfChunk offset {1}: {2}", record.ChunkOffset, this.fileOffset, ex.Message),
							this.fileOffset,
							record.GetNextRecordOffset());
					}
				}

				template = this.templates[parentFragmentHeader.TemplateDefinitionOffset];

				////whether we found a template or not, we should be in position to read value descriptors
				int valuesOffset = offset;
				ValueDescriptor[] descriptors = this.ReadValueDescriptors(ref valuesOffset);

				////before enumerating the template elements check if the template found the event data yet.
				if (template.RootElement.ValueType == BinaryValueType.BinXmlType)
				{
					if (!template.RootElement.Children.Any(e => e.Name == "EventData"))
					{
						valuesOffset = descriptors[template.RootElement.ValueIndex].Offset;

						FragmentHeader childFragmentHeader = this.ReadFragmentHeader(record, ref valuesOffset);

						////Revisit to see if we really need to store the eventDataTemplate in the Dictionary
						BinaryXmlTemplate eventDataTemplate = null;
						if (childFragmentHeader.ContainsTemplateDefinition)
						{
							eventDataTemplate = this.ReadBinaryXmlTemplateRecord(childFragmentHeader, record, ref valuesOffset);
							eventDataTemplate.RootElement = this.ReadBinaryXmlElement(ref valuesOffset);
						}

						eventDataTemplate = this.templates[childFragmentHeader.TemplateDefinitionOffset];

						template.RootElement.Children.Add(eventDataTemplate.RootElement);
					}
					else
					{
						////move the offset forward to the inner value descriptors for event data
						valuesOffset = descriptors[template.RootElement.ValueIndex].Offset;
						FragmentHeader instanceHeader = this.ReadFragmentHeader(record, ref valuesOffset);
					}
				}

				foreach (BinaryXmlElement element in template.RootElement.EnumerateAllChildren().Where(e => e.Name != "Data"))
				{
					switch (element.Name)
					{
						case "Level":
							byte eventLevel = this.buffer[descriptors[element.ValueIndex].Offset];
							record.SetEventLevel(eventLevel);
							break;

						case "Provider":
							if (element.Attributes.Count > 0)
							{
								if (!string.IsNullOrEmpty(element.Attributes[0].Value))
								{
									record.Provider = element.Attributes[0].Value;
								}
								else
								{
									////might be optional substitution
									record.Provider = Encoding.Unicode.GetString(
										this.buffer,
										descriptors[element.Attributes[0].ValueIndex].Offset,
										descriptors[element.Attributes[0].ValueIndex].Size);
								}
							}
							else
							{
								record.Provider = "Unknown";
							}

							break;
						case "TimeCreated":
							BinaryXmlAttribute systemTimeAttribute = element.Attributes.FirstOrDefault(e => e.Name == "SystemTime");
							if (systemTimeAttribute != null && systemTimeAttribute.ValueType == BinaryValueType.FileTime)
							{
								long fileTime = BitConverter.ToInt64(this.buffer, descriptors[systemTimeAttribute.ValueIndex].Offset);
								record.TimeCreated = DateTime.FromFileTime(fileTime);
							}

							break;
						case "EventData":
							ValueDescriptor[] eventDataDescriptors = this.ReadValueDescriptors(ref valuesOffset);
							StringBuilder message = new StringBuilder();
							foreach (BinaryXmlElement dataElement in element.EnumerateAllChildren().Where(e => 
								e.Name == "Data" && 
								e.ValueType == BinaryValueType.Utf16StringArray))
							{
								////There is a null terminator at the end of these
								int dataOffset = eventDataDescriptors[dataElement.ValueIndex].Offset;
								int dataSize = eventDataDescriptors[dataElement.ValueIndex].Size;
								if (dataOffset + dataSize > this.buffer.Length)
								{
									////I think the buffer is recycled by the writer, and not cleaned up afterwards.  I was seeing garbage data after the LastRecordId.
									////I believe it just so happened that the garbage started with 0x2A2A0000 which made the logic believe there was a next record in the chunk.
									////Even so, I will leave this here just in case this scenario really does occur.
									throw new EventDataOutsideOfBufferException(
										string.Format("Event data is refrenced outside of buffer. Record Offset: {0} BinaryXmlElement.Offset: {1}", this.FileOffset + record.ChunkOffset, dataElement.Offset), 
										this.fileOffset + 65536, 
										(int)ElfChunk.HeaderSize);
								}

								message.Append(Encoding.Unicode.GetString(this.buffer, dataOffset, dataSize).TrimEnd('\0'));
							}

							record.Message = message.ToString();

							break;
					}
				}

				offset = record.GetNextRecordOffset();
                yield return record;

				if (record.RecordId == this.lastRecordId)
				{
					yield break;
				}
			}
		}

		private string ReadName(ref int offset)
		{
			////Name offset first 4 bytes are unknown and usually null, next 2 bytes are a hash
			offset += 6;
			int nameCharCount = BitConverter.ToInt16(this.buffer, offset) * 2;

			offset += 2;
			string name = Encoding.Unicode.GetString(this.buffer, offset, nameCharCount);

			////there is a 16 bit null terminator at the end
			offset += nameCharCount + 2;
			return name;
		}

		private BinaryXmlElement ReadBinaryXmlElement(ref int offset)
		{
			bool hasAttributes = this.buffer[offset] == 0x41;
			BinaryXmlElement element = new BinaryXmlElement();
			element.Offset = offset;

			////I don't know what the next two bytes mean
			offset += 3;
			element.Length = BitConverter.ToInt32(this.buffer, offset) + 7;

			offset += 4;
			int nameOffset = BitConverter.ToInt32(this.buffer, offset);
			
			element.Name = this.ReadName(ref nameOffset);
			if (nameOffset < element.Offset)
			{
				////The name was stored with a previous element.  Restore the offset plus the length of pointer for the name
				offset += 4;
				nameOffset = offset; 
			}

			if (hasAttributes)
			{
				int attributesOffset = nameOffset;
				int attributesSize = BitConverter.ToInt32(this.buffer, attributesOffset);

				attributesOffset += 4;
				offset = attributesOffset;
				while (hasAttributes)
				{
					hasAttributes = this.buffer[attributesOffset] == 0x46;
					attributesOffset++;
					offset = attributesOffset;

					////seems silly because this is getting an offset immeadiately after the offset
					attributesOffset = BitConverter.ToInt32(this.buffer, attributesOffset);

					BinaryXmlAttribute attribute = new BinaryXmlAttribute();
					attribute.Name = this.ReadName(ref attributesOffset);
					if (attributesOffset < element.Offset)
					{
						////The name was stored with a previous element.  Restore the offset plus the length of pointer for the name
						attributesOffset = offset + 4;
					}

					this.ReadAttribute(attribute, ref attributesOffset);
					element.Attributes.Add(attribute);
				}

				offset = attributesOffset;
			}
			else
			{
				offset = nameOffset;
			}

			this.ProcessNextXmlToken(element, ref offset);

			while (offset < element.Offset + element.Length && 
				(this.buffer[offset] == 0x41 || this.buffer[offset] == 0x01))
			{
				element.Children.Add(this.ReadBinaryXmlElement(ref offset));
			}

			this.ProcessNextXmlToken(element, ref offset);
			if (this.buffer[offset] == 0x00)
			{
				////null terminator for the template
				offset++;
			}

			return element;
		}

		private void ProcessNextXmlToken(BinaryXmlElement element, ref int offset)
		{
			if (offset >= element.Offset + element.Length)
			{
				return;
			}

			byte nextToken = this.buffer[offset];

			switch (nextToken)
			{
				case 0x00:
					////EOF for the xml
					offset++;
					return;

				case 0x01:
				case 0x41:
					////starting a new element.
					return;

				case 0x02:
				case 0x03:
					////closing a start element or empty element
					offset++;
					this.ProcessNextXmlToken(element, ref offset);

					break;
				case 0x04:
					////End tag of an element
					offset++;
					this.ProcessNextXmlToken(element, ref offset);

					break;
				case 0x05:
					////value
					this.ReadAttribute(element, ref offset);
					this.ProcessNextXmlToken(element, ref offset);

					break;
				case 0x0D:
				case 0x0E:
					////I'm not 100% about the 0x0D
					offset++;
					element.ValueIndex = BitConverter.ToInt16(this.buffer, offset);

					offset += 2;
					element.ValueType = (BinaryValueType)this.buffer[offset];

					offset++;

					this.ProcessNextXmlToken(element, ref offset);

					break;
			}
		}

		private void ReadAttribute(IValueContainer element, ref int offset)
		{
			byte attributeType = this.buffer[offset];

			switch (attributeType)
			{
				case 0x05:
					offset += 2;

					int valueCharCount = BitConverter.ToInt16(this.buffer, offset) * 2;

					offset += 2;
					element.Value = Encoding.Unicode.GetString(this.buffer, offset, valueCharCount);
					offset += valueCharCount;

					break;
				case 0x0E:
					offset++;
					element.ValueIndex = BitConverter.ToInt16(this.buffer, offset);
					offset += 2;

					element.ValueType = (BinaryValueType)this.buffer[offset];
					offset++;

					////we will have to find this with value descriptors
					element.Value = null;

					return;
			}

			return;
		}

		private ValueDescriptor[] ReadValueDescriptors(ref int offset)
		{
			ValueDescriptor[] descriptors = new ValueDescriptor[BitConverter.ToInt32(this.buffer, offset)];
			offset += 4;

			int valueOffset = descriptors.Length * 4 + offset;

			for (int index = 0; index < descriptors.Length; index++)
			{
				ValueDescriptor descriptor = descriptors[index] = new ValueDescriptor();
				descriptor.Size = BitConverter.ToUInt16(this.buffer, offset);
				offset += 2;

				descriptor.Type = (BinaryValueType)this.buffer[offset];
				////fourth byte should be null
				offset += 2;

				////keep track of where the value should be
				descriptor.Offset = valueOffset;
				valueOffset += descriptor.Size;
			}

			return descriptors;
		}

		private FragmentHeader ReadFragmentHeader(EventRecord record, ref int offset)
		{
			int signature = BitConverter.ToInt32(this.buffer, offset);

			if (signature != FragmentHeader.Signature)
			{
				throw new FragmentHeaderNotFoundException(string.Format("Fragment Header not found at offset {0} in ElfChnk offset {1}", offset, this.fileOffset), this.fileOffset, record.GetNextRecordOffset());
			}

			FragmentHeader header = new FragmentHeader(offset);
			////After the signature there is an xml token and version of 0x0C01.
			////I'll trust that it's there if the signature passes
			offset += 6;

			header.TemplateId = BitConverter.ToInt32(this.buffer, offset);
			offset += 4;

			header.TemplateDefinitionOffset = BitConverter.ToInt32(this.buffer, offset);
			offset += 4;

			return header;
		}

		private BinaryXmlTemplate ReadBinaryXmlTemplateRecord(FragmentHeader fragmentHeader, EventRecord record, ref int offset)
		{
			BinaryXmlTemplate template = new BinaryXmlTemplate();

			////first 4 bytes appear to be null
			offset += 4;
			this.guidBuffer.Initialize();

			Buffer.BlockCopy(this.buffer, offset, this.guidBuffer, 0, this.guidBuffer.Length);
			offset += this.guidBuffer.Length;

			template = new BinaryXmlTemplate();
			template.TemplateGuid = new Guid(this.guidBuffer);
			template.ChunkOffset = fragmentHeader.TemplateDefinitionOffset;
			template.Size = BitConverter.ToInt32(this.buffer, offset);

			this.templates.Add(fragmentHeader.TemplateDefinitionOffset, template);
			////62-66 should be a new static fragment header
			offset += 4;

			int fragmentSignature = BitConverter.ToInt32(this.buffer, offset);
			if (fragmentSignature != FragmentHeader.Signature)
			{
				throw new FragmentHeaderNotFoundException(
					string.Format("Fragment Header for BinaryXmlTemplate not found at offset {0} in ElfChnk offset {1}", offset, this.fileOffset),
					this.fileOffset,
					record.GetNextRecordOffset());
			}

			offset += 4;

			return template;
		}
    }
}
