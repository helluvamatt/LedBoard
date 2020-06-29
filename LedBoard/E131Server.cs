using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LedBoard
{
	internal class E131Server : IDisposable
	{
		public const int DEFAULT_PORT = 5568;

		private Socket _ServerSocket;
		private byte _CurrentSequence;

		public void Start(int port = DEFAULT_PORT)
		{
			_ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_ServerSocket.Bind(new IPEndPoint(0, port));
			Task.Run(() =>
			{
				var buffer = new byte[638];
				while (true)
				{
					try
					{
						_ServerSocket.Receive(buffer);
						HandlePacket(buffer);
					}
					catch (Exception)
					{
						break;
					}
				}
			});
		}

		public event Action<ushort, ushort, byte[]> UniversePacketReceived;

		private void HandlePacket(byte[] buffer)
		{
			E131Packet packet;
			using (var memory = new MemoryStream(buffer))
			{
				using (var reader = new BinaryReader(memory))
				{
					packet.ACNRoot_PreambleSize = reader.ReadUInt16BE();
					packet.ACNRoot_PostambleSize = reader.ReadUInt16BE();
					packet.ACNRoot_PID = reader.ReadBytes(12);
					packet.ACNRoot_FlagsLength = reader.ReadUInt16BE();
					packet.ACNRoot_Vector = reader.ReadUInt32BE();
					packet.ACNRoot_CIF = reader.ReadBytes(16);

					packet.Framing_FlagsLength = reader.ReadUInt16BE();
					packet.Framing_Vector = reader.ReadUInt32BE();
					packet.Framing_SourceName = reader.ReadBytes(64);
					packet.Framing_Priority = reader.ReadByte();
					packet.Framing_Reserved = reader.ReadUInt16BE();
					packet.Framing_SequenceNumber = reader.ReadByte();
					packet.Framing_Options = reader.ReadByte();
					packet.Framing_Universe = reader.ReadUInt16BE();

					packet.DMP_FlagsLength = reader.ReadUInt16BE();
					packet.DMP_Vector = reader.ReadByte();
					packet.DMP_Type = reader.ReadByte();
					packet.DMP_FirstAddress = reader.ReadUInt16BE();
					packet.DMP_AddressInc = reader.ReadUInt16BE();
					packet.DMP_PropCount = reader.ReadUInt16BE();
					packet.DMP_PropValues = reader.ReadBytes(513);
				}
			}
			int seqDiff = packet.Framing_SequenceNumber - _CurrentSequence;
			_CurrentSequence = packet.Framing_SequenceNumber;
			if (seqDiff > -20 && seqDiff <= 0) return; // Drop out-of-sequence
			UniversePacketReceived?.Invoke(packet.Framing_Universe, packet.DMP_PropCount, packet.DMP_PropValues);
		}

		public void Dispose()
		{
			_ServerSocket?.Dispose();
		}

		private struct E131Packet
		{
			public ushort ACNRoot_PreambleSize;
			public ushort ACNRoot_PostambleSize;
			public byte[] ACNRoot_PID; // 12
			public ushort ACNRoot_FlagsLength;
			public uint   ACNRoot_Vector;
			public byte[] ACNRoot_CIF; // 16

			public ushort Framing_FlagsLength;
			public uint   Framing_Vector;
			public byte[] Framing_SourceName; // 64
			public byte   Framing_Priority;
			public ushort Framing_Reserved;
			public byte   Framing_SequenceNumber;
			public byte   Framing_Options;
			public ushort Framing_Universe;

			public ushort DMP_FlagsLength;
			public byte   DMP_Vector;
			public byte   DMP_Type;
			public ushort DMP_FirstAddress;
			public ushort DMP_AddressInc;
			public ushort DMP_PropCount;
			public byte[] DMP_PropValues; // 513
		}
	}

	internal static class BinaryUtils
	{
		public static ushort ReadUInt16BE(this BinaryReader reader)
		{
			byte[] buffer = reader.ReadBytes(2);
			if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
			return BitConverter.ToUInt16(buffer, 0);
		}

		public static uint ReadUInt32BE(this BinaryReader reader)
		{
			byte[] buffer = reader.ReadBytes(4);
			if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
			return BitConverter.ToUInt32(buffer, 0);
		}
	}
}
