using System;
using System.IO;

namespace Minecraft.Utility
{
	public class BigEndianWriter : BinaryWriter
	{
		public BigEndianWriter(Stream output)
			: base(output) {  }
		
		public override void Write(short value)
		{
			byte[] array = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			Write(array);
		}
		public override void Write(int value)
		{
			byte[] array = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			Write(array);
		}
		public override void Write(long value)
		{
			byte[] array = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			Write(array);
		}
		public override void Write(float value)
		{
			byte[] array = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			Write(array);
		}
		public override void Write(double value)
		{
			byte[] array = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian) Array.Reverse(array);
			Write(array);
		}
	}
}
