using System;
using System.IO;

namespace Minecraft.Utility
{
	public class BigEndianReader : BinaryReader
	{
		public BigEndianReader(Stream input)
			: base(input) {  }
		
		public override short ReadInt16()
		{
			byte[] array = ReadBytes(2);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			return BitConverter.ToInt16(array, 0);
		}
		public override int ReadInt32()
		{
			byte[] array = ReadBytes(4);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			return BitConverter.ToInt32(array, 0);
		}
		public override long ReadInt64()
		{
			byte[] array = ReadBytes(8);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			return BitConverter.ToInt64(array, 0);
		}
		public override float ReadSingle()
		{
			byte[] array = ReadBytes(4);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			return BitConverter.ToSingle(array, 0);
		}
		public override double ReadDouble()
		{
			byte[] array = ReadBytes(8);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			return BitConverter.ToDouble(array, 0);
		}
	}
}
