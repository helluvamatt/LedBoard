using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LedBoard.Shared.PNG
{
	public class AnimatedPng
	{
        private readonly List<Frame> _Frames;

        private Frame _DefaultFrame;
        private IHDRChunk _HeaderChunk;
        private AcTLChunk _AnimationControlChunk;

        public AnimatedPng()
		{
            _DefaultFrame = new Frame();
            _Frames = new List<Frame>();
		}

        public bool HasAnimation { get; private set; }
        public bool DefaultFrameIsAnimated { get; private set; }

        public int FrameCount => _Frames.Count;
        public uint PlayCount => _AnimationControlChunk.NumPlays;
        public uint Width => _HeaderChunk.Width;
        public uint Height => _HeaderChunk.Height;

        public void LoadApng(Stream stream)
		{
            if (!IsPng(stream))
			{
                throw new InvalidDataException("Invalid file signature.");
			}

            _HeaderChunk = new IHDRChunk(stream);
            if (_HeaderChunk.ChunkType != "IHDR")
			{
                throw new InvalidDataException("IHDR chunk must be the first chunk.");
			}

            _DefaultFrame = new Frame();
            HasAnimation = true;

            Chunk chunk;
            Frame frame = null;
            var otherChunks = new List<Chunk>();
            bool isIDATParsed = false;
            do
            {
                if (stream.Position == stream.Length)
                {
                    throw new InvalidDataException("IEND chunk expected.");
                }

                chunk = new Chunk(stream);

                switch (chunk.ChunkType)
                {
                    case "IHDR":
                        throw new InvalidDataException("Duplicate IHDR chunk detected.");
                    case "acTL":
                        if (!HasAnimation) throw new InvalidDataException("acTL chunk must be located before any IDAT or fdAT chunks.");
                        _AnimationControlChunk = new AcTLChunk(chunk);
                        break;
                    case "IDAT":
                        if (_AnimationControlChunk == null) HasAnimation = false;
                        _DefaultFrame.HeaderChunk = _HeaderChunk;
                        _DefaultFrame.AddDataChunk(new IDATChunk(chunk));
                        isIDATParsed = true;
                        break;
                    case "fcTL":
                        if (!HasAnimation) continue;
                        if (frame != null && !frame.DataChunks.Any()) throw new InvalidDataException("Frame can only have one fcTL chunk.");
                        if (isIDATParsed)
						{
                            if (frame != null) _Frames.Add(frame);
                            frame = new Frame
                            {
                                HeaderChunk = _HeaderChunk,
                                FrameControlChunk = new FcTLChunk(chunk),
                            };
						}
                        else
						{
                            _DefaultFrame.FrameControlChunk = new FcTLChunk(chunk);
						}
                        break;
                    case "fdAT":
                        if (!HasAnimation) continue;
                        if (frame == null || frame.FrameControlChunk == null) throw new InvalidDataException("fcTL chunk expected.");
                        frame.AddDataChunk(new FdATChunk(chunk).ToIDATChunk());
                        break;
                    case "IEND":
                        if (frame != null) _Frames.Add(frame);
                        if (_DefaultFrame.DataChunks.Any()) _DefaultFrame.EndChunk = new IENDChunk(chunk);
                        foreach (var f in _Frames)
						{
                            f.EndChunk = new IENDChunk(chunk);
						}
                        break;
                    default:
                        otherChunks.Add(chunk);
                        break;
                }
            } while (chunk.ChunkType != "IEND");

            if (_DefaultFrame.FrameControlChunk != null)
            {
                _Frames.Insert(0, _DefaultFrame);
                DefaultFrameIsAnimated = true;
            }

            foreach (var f in _Frames)
			{
                foreach (var otherChunk in otherChunks)
				{
                    f.AddOtherChunk(otherChunk);
				}
			}
        }

        public void SetDefaultFrame(Stream pngStream)
		{
            AnimatedPng apng = new AnimatedPng();
            apng.LoadApng(pngStream);
            _DefaultFrame = apng._DefaultFrame;
            DefaultFrameIsAnimated = false;
        }

        public void AddFrame(Stream pngStream, TimeSpan frameDelay)
		{
            AnimatedPng apng = new AnimatedPng();
            apng.LoadApng(pngStream);

            if (_HeaderChunk != null && (apng.Width > _HeaderChunk.Width || apng.Height > _HeaderChunk.Height)) throw new InvalidDataException("Frame must be less than or equal to the size of the other frames.");

            // Set IHDR chunk if needed
            if (_HeaderChunk == null) _HeaderChunk = apng._HeaderChunk;

            // Create acTL Chunk if needed
            if (_AnimationControlChunk == null) _AnimationControlChunk = new AcTLChunk();

            uint sequenceNumber = (_Frames.Count == 0) ? 0 : (uint)(_Frames[_Frames.Count - 1].FrameControlChunk.SequenceNumber + _Frames[_Frames.Count - 1].DataChunks.Count());
            
            // Create fcTL Chunk
            FcTLChunk fctl = new FcTLChunk
            {
                SequenceNumber = sequenceNumber,
                Width = apng.Width,
                Height = apng.Height,
                XOffset = 0,
                YOffset = 0,
                DelayNumerator = (ushort)frameDelay.TotalMilliseconds,
                DelayDenominator = 1000,
                Dispose = DisposeOp.None,
                Blend = BlendOp.Source
            };

            // Set the default image if needed
            if (!_DefaultFrame.DataChunks.Any())
            {
                _DefaultFrame = apng._DefaultFrame;
                _DefaultFrame.FrameControlChunk = fctl;
                DefaultFrameIsAnimated = true;
            }

            // Add all the frames from the png
            if (!apng.HasAnimation)
            {
                Frame frame = apng._DefaultFrame;
                frame.FrameControlChunk = fctl;
                foreach (Chunk chunk in frame.OtherChunks)
                {
                    if (!_DefaultFrame.OtherChunks.Contains(chunk)) _DefaultFrame.AddOtherChunk(chunk);
                }
                frame.ClearOtherChunks();
                _Frames.Add(frame);
            }
            else
            {
                for (int i = 0; i < apng.FrameCount; ++i)
                {
                    Frame frame = apng._Frames[i];
                    frame.FrameControlChunk.SequenceNumber = sequenceNumber;
                    foreach (Chunk chunk in frame.OtherChunks)
                    {
                        if (!_DefaultFrame.OtherChunks.Contains(chunk)) _DefaultFrame.AddOtherChunk(chunk);
                    }
                    frame.ClearOtherChunks();
                    _Frames.Add(frame);
                }
            }

            // Apply every chunk in otherChunks to every frame.
            if (_DefaultFrame != _Frames[0])
            {
                foreach (Frame frame in _Frames)
                {
                    foreach (Chunk otherChunk in _DefaultFrame.OtherChunks)
                    {
                        frame.AddOtherChunk(otherChunk);
                    }
                }
            }
            else
            {
                for (int i = 1; i < FrameCount; i++)
                {
                    foreach (Chunk otherChunk in _DefaultFrame.OtherChunks)
					{
                        _Frames[i].AddOtherChunk(otherChunk);
					}
                }
            }

            _AnimationControlChunk.NumFrames = (uint)_Frames.Count;
        }

        public void WriteFrameTo(Stream stream, int frameIndex)
		{
            _Frames[frameIndex].WriteTo(stream);
		}

        public void WriteDefaultFrameTo(Stream stream)
		{
            _DefaultFrame.WriteTo(stream);
		}

        public BlendOp GetFrameBlend(int frameIndex)
		{
            return _Frames[frameIndex].FrameControlChunk.Blend;
		}

        public DisposeOp GetFrameDispose(int frameIndex)
		{
            return _Frames[frameIndex].FrameControlChunk.Dispose;
		}

        public TimeSpan GetFrameDelay(int frameIndex)
		{
            var fctl = _Frames[frameIndex].FrameControlChunk;
            return TimeSpan.FromSeconds((double)fctl.DelayNumerator / (fctl.DelayDenominator > 0 ? fctl.DelayDenominator : 100));
		}

        public FrameRect GetFrameRect(int frameIndex)
		{
            var fctl = _Frames[frameIndex].FrameControlChunk;
            return new FrameRect
            {
                X = fctl.XOffset,
                Y = fctl.YOffset,
                Width = fctl.Width,
                Height = fctl.Height,
            };
        }

        public void WriteApngTo(Stream stream)
		{
            stream.WriteBytes(Frame.Signature);
            _HeaderChunk.WriteTo(stream);
            if (_AnimationControlChunk != null) _AnimationControlChunk.WriteTo(stream);
            foreach (Chunk otherChunk in _DefaultFrame.OtherChunks)
			{
                otherChunk.WriteTo(stream);
			}

            // Write default/first frame, logic is based on if the default frame is part of the animation
            uint sequenceNumber = 0;
            int frameWriteStartIndex = 0;
            if (!DefaultFrameIsAnimated)
			{
                foreach (IDATChunk chunk in _DefaultFrame.DataChunks)
				{
                    chunk.WriteTo(stream);
				}
			}
            else
			{
                _Frames[0].FrameControlChunk.SequenceNumber = sequenceNumber++;
                _Frames[0].FrameControlChunk.WriteTo(stream);
                foreach (IDATChunk chunk in _Frames[0].DataChunks)
				{
                    chunk.WriteTo(stream);
				}
                frameWriteStartIndex = 1;
			}

            // Write the rest of the frames
            for (int i = frameWriteStartIndex; i < FrameCount; i++)
			{
                _Frames[i].FrameControlChunk.SequenceNumber = sequenceNumber++;
                _Frames[i].FrameControlChunk.WriteTo(stream);
                foreach (IDATChunk idatChunk in _Frames[i].DataChunks)
				{
                    FdATChunk fdATChunk = new FdATChunk(idatChunk, sequenceNumber++);
                    fdATChunk.WriteTo(stream);
				}
            }

            // Write IEND
            _DefaultFrame.EndChunk.WriteTo(stream);
        }

        public static bool IsPng(Stream stream)
		{
			try
			{
                return ByteArrayExtensions.Equals(stream.ReadBytes(Frame.Signature.Length), Frame.Signature);
            }
			catch
			{
                return false;
			}
		}

        public static AnimatedPng FromFile(string filePath)
		{
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
                var apng = new AnimatedPng();
                apng.LoadApng(stream);
                return apng;
			}
        }
    }

    internal class Frame
	{
        public static byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly List<IDATChunk> _IDATChunks = new List<IDATChunk>();
        private readonly List<Chunk> _OtherChunks = new List<Chunk>();

        public IHDRChunk HeaderChunk { get; set; }
        public FcTLChunk FrameControlChunk { get; set; }
        public IENDChunk EndChunk { get; set; }
        public IEnumerable<Chunk> OtherChunks => _OtherChunks;
        public IEnumerable<IDATChunk> DataChunks => _IDATChunks;

        public void AddOtherChunk(Chunk chunk)
        {
            _OtherChunks.Add(chunk);
        }

        public void ClearOtherChunks()
		{
            _OtherChunks.Clear();
		}

        public void AddDataChunk(IDATChunk chunk)
        {
            _IDATChunks.Add(chunk);
        }

        public void WriteTo(Stream stream)
		{
            var ihdrChunk = new IHDRChunk(HeaderChunk);
            if (FrameControlChunk != null)
            {
                // Fix frame size with fcTL data.
                ihdrChunk.Width = FrameControlChunk.Width;
                ihdrChunk.Height = FrameControlChunk.Height;
            }
            stream.WriteBytes(Signature);
            ihdrChunk.WriteTo(stream);
            _OtherChunks.ForEach(o => o.WriteTo(stream));
            _IDATChunks.ForEach(i => i.WriteTo(stream));
            EndChunk.WriteTo(stream);
        }
    }

    #region Chunks

    internal class Chunk
	{
        public Chunk(Chunk other)
		{
            Length = other.Length;
            ChunkType = other.ChunkType;
            Crc = other.Crc;

            byte[] otherData = new byte[other.ChunkData.Length];
            Array.Copy(other.ChunkData, otherData, otherData.Length);
            ChunkData = otherData;
        }

        public Chunk(Stream stream)
		{
            Length = stream.ReadUInt32();
            ChunkType = Encoding.ASCII.GetString(stream.ReadBytes(4));
            ChunkData = stream.ReadBytes((int)Length);
            Crc = stream.ReadUInt32();
        }

        protected Chunk(uint length, string chunkType, IEnumerable<byte> chunkData)
		{
            Length = length;
            ChunkType = chunkType;
            ChunkData = chunkData.ToArray();
            ComputeCrc();
		}

        public uint Length { get; }

        public string ChunkType { get; }

        public byte[] ChunkData { get; }

        public uint Crc { get; private set; }

		#region ChunkData setters

		protected void SetByte(int offset, byte value)
		{
            ChunkData[offset] = value;
            ComputeCrc();
		}

        protected void SetInt32(int offset, int value)
		{
            ChunkData.SetInt32(offset, value);
            ComputeCrc();
		}

        protected void SetUInt32(int offset, uint value)
		{
            ChunkData.SetUInt32(offset, value);
            ComputeCrc();
		}

        protected void SetUInt16(int offset, ushort value)
		{
            ChunkData.SetUInt16(offset, value);
            ComputeCrc();
		}

		#endregion

		#region ChunkData getters

        protected int GetInt32(int offset) => ChunkData.GetInt32(offset);
        protected uint GetUInt32(int offset) => ChunkData.GetUInt32(offset);
        protected short GetInt16(int offset) => ChunkData.GetInt16(offset);
        protected ushort GetUInt16(int offset) => ChunkData.GetUInt16(offset);

        #endregion

        private void ComputeCrc()
		{
            var crcBytes = new List<byte>();
            crcBytes.AddRange(Encoding.ASCII.GetBytes(ChunkType));
            crcBytes.AddRange(ChunkData);
            Crc = crcBytes.CalculateCrc();
        }

        public void WriteTo(Stream stream)
		{
            stream.WriteUInt32(Length);
            stream.WriteBytes(Encoding.ASCII.GetBytes(ChunkType));
            stream.WriteBytes(ChunkData);
            stream.WriteUInt32(Crc);
        }
	}

    internal class IHDRChunk : Chunk
	{
        public IHDRChunk(Stream stream) : base(stream) { }
        public IHDRChunk(Chunk other) : base(other) { }

        public uint Width
        {
            get => GetUInt32(0);
            set => SetUInt32(0, value);
        }

        public uint Height
		{
            get => GetUInt32(4);
            set => SetUInt32(4, value);
		}

        public byte BitDepth => ChunkData[8];

        public byte ColorType => ChunkData[9];

        public byte CompressionMethod => ChunkData[10];

        public byte FilterMethod => ChunkData[11];

        public byte InterlaceMethod => ChunkData[12];
    }

    internal class IENDChunk : Chunk
	{
        public IENDChunk(Stream stream) : base(stream) { }
        public IENDChunk(Chunk other) : base(other) { }
    }

    internal class IDATChunk : Chunk
	{
        public IDATChunk(Stream stream) : base(stream) { }
        public IDATChunk(Chunk other) : base(other) { }
    }

    internal class AcTLChunk : Chunk
	{
        private const int LEN = 8;
        private const string NAME = "acTL";

        public AcTLChunk(Stream stream) : base(stream) { }
        public AcTLChunk(Chunk other) : base(other) { }
        public AcTLChunk() : base(LEN, NAME, Enumerable.Repeat((byte)0, LEN)) { }

        public uint NumFrames
		{
            get => GetUInt32(0);
            set => SetUInt32(0, value);
		}

        public uint NumPlays
		{
            get => GetUInt32(4);
            set => SetUInt32(4, value);
		}
    }

    internal class FcTLChunk : Chunk
	{
        private const int LEN = 26;
        private const string NAME = "fcTL";

        public FcTLChunk(Stream stream) : base(stream) { }
        public FcTLChunk(Chunk other) : base(other) { }
        public FcTLChunk() : base(LEN, NAME, Enumerable.Repeat((byte)0, LEN)) { }

        public uint SequenceNumber
        {
            get => GetUInt32(0);
            set => SetUInt32(0, value);
        }

        public uint Width
        {
            get => GetUInt32(4);
            set => SetUInt32(4, value);
        }

        public uint Height
        {
            get => GetUInt32(8);
            set => SetUInt32(8, value);
        }

        public uint XOffset
        {
            get => GetUInt32(12);
            set => SetUInt32(12, value);
        }

        public uint YOffset
        {
            get => GetUInt32(16);
            set => SetUInt32(16, value);
        }

        public ushort DelayNumerator
        {
            get => GetUInt16(20);
            set => SetUInt16(20, value);
        }

        public ushort DelayDenominator
        {
            get => GetUInt16(22);
            set => SetUInt16(22, value);
        }

        public DisposeOp Dispose
        {
            get => (DisposeOp)ChunkData[24];
            set => SetByte(24, (byte)value);
        }

        public BlendOp Blend
        {
            get => (BlendOp)ChunkData[25];
            set => SetByte(25, (byte)value);
        }
    }

    internal class FdATChunk : Chunk
	{
        private readonly static byte[] IDAT = new[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' };

        public FdATChunk(Stream stream) : base(stream) { }
        public FdATChunk(Chunk other) : base(other) { }

        public FdATChunk(IDATChunk datChunk, uint sequenceNumber) : base(4 + datChunk.Length, "fdAT", BitConverter.GetBytes(sequenceNumber).ConvertEndian().Concat(datChunk.ChunkData)) { }

        public uint SequenceNumber => GetUInt32(0);
        public byte[] FrameData => ChunkData.Skip(4).ToArray();

        public IDATChunk ToIDATChunk()
        {
            var crcBytes = new List<byte>();
            crcBytes.AddRange(IDAT);
            crcBytes.AddRange(FrameData);
            uint newCrc = crcBytes.CalculateCrc();
            using (var ms = new MemoryStream())
            {
                ms.WriteUInt32(Length - 4);
                ms.WriteBytes(IDAT);
                ms.WriteBytes(FrameData);
                ms.WriteUInt32(newCrc);
                ms.Position = 0;
                return new IDATChunk(ms);
            }
        }
    }

    #endregion

    #region Enums

    public enum DisposeOp
    {
        None = 0,
        Background = 1,
        Previous = 2,
    }

    public enum BlendOp
    {
        Source = 0,
        Over = 1,
    }

    #endregion

    #region Helpers

    internal static class ByteArrayExtensions
	{
        public static byte[] ConvertEndian(this byte[] i)
        {
            if (i.Length % 2 != 0) throw new Exception("byte array length must multiply of 2");
            if (BitConverter.IsLittleEndian) Array.Reverse(i);
            return i;
        }

        public static int GetInt32(this byte[] data, int offset)
		{
            return BitConverter.ToInt32(ConvertEndian(Extract(data, offset, 4)), 0);
        }

        public static void SetInt32(this byte[] data, int offset, int value)
		{
            Array.Copy(ConvertEndian(BitConverter.GetBytes(value)), 0, data, offset, 4);
        }

        public static uint GetUInt32(this byte[] data, int offset)
        {
            return BitConverter.ToUInt32(ConvertEndian(Extract(data, offset, 4)), 0);
        }

        public static void SetUInt32(this byte[] data, int offset, uint value)
        {
            Array.Copy(ConvertEndian(BitConverter.GetBytes(value)), 0, data, offset, 4);
        }

        public static short GetInt16(this byte[] data, int offset)
        {
            return BitConverter.ToInt16(ConvertEndian(Extract(data, offset, 2)), 0);
        }

        public static void SetInt16(this byte[] data, int offset, short value)
        {
            Array.Copy(ConvertEndian(BitConverter.GetBytes(value)), 0, data, offset, 2);
        }

        public static ushort GetUInt16(this byte[] data, int offset)
        {
            return BitConverter.ToUInt16(ConvertEndian(Extract(data, offset, 2)), 0);
        }

        public static void SetUInt16(this byte[] data, int offset, ushort value)
        {
            Array.Copy(ConvertEndian(BitConverter.GetBytes(value)), 0, data, offset, 2);
        }

        private static byte[] Extract(this byte[] data, int offset, int length)
		{
            byte[] extracted = new byte[length];
            Array.Copy(data, offset, extracted, 0, length);
            return extracted;
		}

		public static bool Equals(byte[] byte1, byte[] byte2)
        {
            if (byte1.Length != byte2.Length) return false;
            for (int i = 0; i < byte1.Length; i++)
            {
                if (byte1[i] != byte2[i]) return false;
            }
            return true;
        }

        #region CRC

        #region Consts

        private static readonly UInt32[] CrcTable =
        {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,
            0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
            0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
            0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
            0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
            0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
            0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
            0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
            0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
            0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
            0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
            0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,
            0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,
            0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
            0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
            0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
            0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
            0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
            0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
            0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
            0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
            0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
            0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
            0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
            0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
            0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
            0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
            0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
            0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
            0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
            0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
            0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
            0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,
            0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
            0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
            0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
            0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
            0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
            0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
            0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
            0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
            0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
            0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
            0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
        };

        #endregion Consts

        public static uint CalculateCrc(this IEnumerable<byte> data)
        {
            uint crc = data.Aggregate(0xffffffff, (current, t) => (current >> 8) ^ CrcTable[(current & 0xff) ^ t]);
            crc ^= 0xffffffff;
            return crc;
        }

		#endregion
	}

	internal static class StreamExtensions
	{
        #region Read

        public static byte[] ReadBytes(this Stream ms, int count)
        {
            var buffer = new byte[count];
            if (ms.Read(buffer, 0, count) != count) throw new Exception("End reached.");
            return buffer;
        }

        public static char ReadChar(this Stream ms)
        {
            return BitConverter.ToChar(ReadBytes(ms, 2).ConvertEndian(), 0);
        }

        public static short ReadInt16(this Stream ms)
        {
            return BitConverter.ToInt16(ReadBytes(ms, 2).ConvertEndian(), 0);
        }

        public static int ReadInt32(this Stream ms)
        {
            return BitConverter.ToInt32(ReadBytes(ms, 4).ConvertEndian(), 0);
        }

        public static long ReadInt64(this Stream ms)
        {
            return BitConverter.ToInt64(ReadBytes(ms, 8).ConvertEndian(), 0);
        }

        public static ushort ReadUInt16(this Stream ms)
        {
            return BitConverter.ToUInt16(ReadBytes(ms, 2).ConvertEndian(), 0);
        }

        public static uint ReadUInt32(this Stream ms)
        {
            return BitConverter.ToUInt32(ReadBytes(ms, 4).ConvertEndian(), 0);
        }

        public static ulong ReadUInt64(this Stream ms)
        {
            return BitConverter.ToUInt64(ReadBytes(ms, 8).ConvertEndian(), 0);
        }

        #endregion Read

        #region Write

        public static void WriteBytes(this Stream ms, byte[] value)
        {
            ms.Write(value, 0, value.Length);
        }

        public static void WriteInt16(this Stream ms, short value)
        {
            ms.Write(BitConverter.GetBytes(value).ConvertEndian(), 0, 2);
        }

        public static void WriteInt32(this Stream ms, int value)
        {
            ms.Write(BitConverter.GetBytes(value).ConvertEndian(), 0, 4);
        }

        public static void WriteInt64(this Stream ms, long value)
        {
            ms.Write(BitConverter.GetBytes(value).ConvertEndian(), 0, 8);
        }

        public static void WriteUInt16(this Stream ms, ushort value)
        {
            ms.Write(BitConverter.GetBytes(value).ConvertEndian(), 0, 2);
        }

        public static void WriteUInt32(this Stream ms, uint value)
        {
            ms.Write(BitConverter.GetBytes(value).ConvertEndian(), 0, 4);
        }

        public static void WriteUInt64(this Stream ms, ulong value)
        {
            ms.Write(BitConverter.GetBytes(value).ConvertEndian(), 0, 8);
        }

        #endregion Write
    }

	#endregion

	#region Structs

    public struct FrameRect
	{
        public uint X { get; set; }
        public uint Y { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
	}

	#endregion
}
