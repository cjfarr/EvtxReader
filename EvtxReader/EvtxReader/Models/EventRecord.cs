namespace EvtxReader.Models
{
    using System;
	using Constants;

	public class EventRecord
    {
        /// <summary>
        /// Gets or sets the record identifier.
        /// Relative offset 8
        /// </summary>
        /// <value>
        /// The record identifier.
        /// </value>
        public UInt64 RecordId
        {
            get;
            set;
        }

		public int BlockSize
		{
			get;
			set;
		}

		public int ChunkOffset
		{
			get;
			set;
		}

		public string Provider
		{
			get;
			set;
		}

        /// <summary>
        /// Gets the date time.
        /// Relative offset 16, size 8
        /// </summary>
        /// <value>
        /// The date time.
        /// </value>
        public DateTime RecordTime
        {
            get;
            set;
        }

		public DateTime TimeCreated
		{
			get;
			set;
		}

        public string Message
        {
            get;
            set;
        }

		/// <summary>
		/// I think if this is 0x00, then a Guid will follow with boiler plate xml.
		/// Otherwise it should be a count of Values
		/// </summary>
		public int ValuesLength
		{
			get;
			set;
		}

		public Guid Guid
		{
			get;
			set;
		}

        public EventLevel Level
        {
            get;
			private set;
        }

		public int GetNextRecordOffset()
		{
			return this.ChunkOffset + this.BlockSize;
		}

		internal void SetEventLevel(byte value)
		{
			switch (value)
			{
				case 0x01:
					this.Level = EventLevel.Critical;
					break;
				case 0x02:
					this.Level = EventLevel.Error;
					break;
				case 0x03:
					this.Level = EventLevel.Warning;
					break;
				case 0x04:
				case 0x00:
				default:
					this.Level = EventLevel.Information;
					break;
				case 0x05:
					////Not sure I've seen this one
					this.Level = EventLevel.Verbose;
					break;
			}
		}
    }
}
