namespace EvtxReader.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using EvtxReader.Exceptions;
	using Models;

	/// <summary>
	/// https://github.com/libyal/libevtx/tree/master/documentation
	/// machine logs are stored here
	/// %SystemRoot%\System32\Winevt\Logs\
	/// </summary>
	public class EvtxStreamReader : IDisposable
    {
		private const long elfFileSignature = 28548172056259653;
		private const long elfFileHeaderLength = 4096;

        private string sourceFile;
        private BinaryReader reader;
		private byte[] chunkBuffer;
		private long chunkCount;
		private int chunkIndex;
		private long recoveryChunkOffset;
		private int recoveryRecordOffset;

		public EvtxStreamReader(string sourceFile, long recoveryChunkOffset = -1, int recoveryRecordOffset = -1)
        {
            this.sourceFile = sourceFile;
			this.chunkBuffer = new byte[ElfChunk.MaxLength];

			this.recoveryChunkOffset = recoveryChunkOffset;
			this.recoveryRecordOffset = recoveryRecordOffset;

			this.reader = new BinaryReader(new FileStream(this.sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public IEnumerable<EventRecord> ReadRecords()
        {
			////Validate header, and get chunk count
			long signature = this.reader.ReadInt64();
			if (signature != elfFileSignature)
			{
				throw new InvalidDataException("Invalid File Signature!");
			}

			this.reader.BaseStream.Position = 16;
			this.chunkCount = this.reader.ReadInt64();

			if (this.recoveryChunkOffset >= elfFileHeaderLength)
			{
				this.reader.BaseStream.Position = this.recoveryChunkOffset;
				this.chunkIndex = ((int)(this.recoveryChunkOffset - elfFileHeaderLength) / (int)ElfChunk.MaxLength) + 1;
			}
			else
			{
				this.chunkIndex = 0;
				////ElfFile header ends at 4096
				this.reader.BaseStream.Position = elfFileHeaderLength;
			}

            foreach (ElfChunk elfChunk in this.ReadElfChunks())
            {
				int startingOffset = this.recoveryRecordOffset > -1 ? this.recoveryRecordOffset : (int)ElfChunk.HeaderSize;

                foreach (EventRecord record in elfChunk.ReadEventRecords(startingOffset))
                {
                    yield return record;
                }
            }
        }

        private IEnumerable<ElfChunk> ReadElfChunks()
        {
            while (this.chunkIndex <= this.chunkCount &&
				this.reader.BaseStream.Position + ElfChunk.MaxLength <= this.reader.BaseStream.Length)
            {
                if (this.reader.BaseStream.Position + ElfChunk.HeaderSize >= this.reader.BaseStream.Length)
                {
					throw new TruncatedFileException(string.Format("File is not large enough for ElfChnk at offset {0}", this.reader.BaseStream.Position));
                }

				long elfChnkOffset = this.reader.BaseStream.Position;
				this.chunkBuffer.Initialize();
				this.reader.Read(this.chunkBuffer, 0, this.chunkBuffer.Length);

				////verify the header data
				long chunkSignature = BitConverter.ToInt64(this.chunkBuffer, 0);

                if (chunkSignature != ElfChunk.HeaderSignature)
                {
					throw new InvalidElfChnkSignature(string.Format("Invalid ElfChnk signature found at offset {0}", elfChnkOffset));
                }

                ElfChunk elfChunk = new ElfChunk(this.chunkBuffer, elfChnkOffset);
				this.chunkIndex++;

                yield return elfChunk;
            }
        }

        public void Dispose()
        {
            if (this.reader?.BaseStream?.CanRead == true)
            {
                this.reader.Dispose();
            }
        }
    }
}
