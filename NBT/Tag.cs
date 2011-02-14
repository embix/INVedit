using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NBT
{
	public class Tag : IEnumerable<Tag>
	{
		Tag parent;
		string name;
		object value;
		protected TagType type;
		
		#region Public members
		/// <summary>Gets the parent Tag, null if there is none.</summary>
		public Tag Parent {
			get { return parent; }
		}
		/// <summary>Gets or the name of the Tag in case it's a named Tag, otherwise null.</summary>
		public string Name {
			get { return name; }
		}
		/// <summary>Gets the value of the Tag in case it's not a TagList or TagCompound, otherwise null.</summary>
		public object Value {
			get { return value; }
		}
		/// <summary>Gets the TagType of this Tag.</summary>
		public TagType Type {
			get { return type; }
		}
		
		/// <summary>Gets the number of Tags contained in the TagList or TagCompound.</summary>
		/// <exception cref="NotSupportedException">If this isn't a TagList or TagCompound.</exception>
		public virtual int Count {
			get { throw new NotSupportedException(); }
		}
		/// <summary>Gets the TagType of this TagList.</summary>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		public virtual TagType ListType {
			get { throw new NotSupportedException(); }
		}
		
		/// <summary>Gets the Tag at the specific index.</summary>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		public virtual Tag this[int index] {
			get { throw new NotSupportedException(); }
		}
		/// <summary>Gets or sets the Tag with the specific name.</summary>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		/// <exception cref="KeyNotFoundException">If name doesn't exist in this TagCompound.</exception>
		public virtual Tag this[string name] {
			get { throw new NotSupportedException(); }
			set {
				if (value == null) throw new ArgumentNullException("value");
				if (Contains(name)) this[name].Remove();
				Add(name, value);
			}
		}
		#endregion
		
		#region Constructor
		internal Tag(object value) : this(null, null, value) {  }
		internal Tag(Tag parent, string name, object value)
		{
			this.parent = parent;
			this.name = name;
			this.value = value;
			type = TypeOf(value);
		}
		
		/// <summary>Creates a new TagCompound.</summary>
		/// <param name="name">Name of the Tag.</param>
		/// <returns>A new TagCompound.</returns>
		public static Tag Create(string name)
		{
			if (name == null) throw new ArgumentNullException("name");
			return new TagCompound(){ name = name };
		}
		#endregion
		
		#region Manipulation
		protected virtual void Remove(Tag tag)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Removes this Tag from its parent.</summary>
		/// <exception cref="NotSupportedException">If this Tag has no parent.</exception>
		public void Remove()
		{
			if (parent == null) throw new InvalidOperationException("This Tag has no parent.");
			parent.Remove(this);
			parent = null;
		}
		/// <summary>Removes all Tags from this TagList or TagCompound.</summary>
		/// <exception cref="NotSupportedException">If this isn't a TagList or TagCompound.</exception>
		public void Clear()
		{
			foreach (Tag tag in new List<Tag>(this)) tag.Remove();
		}
		#endregion
		
		#region List manipulation
		protected virtual void InsertTag(int index, Tag tag)
		{
			throw new NotSupportedException();
		}
		Tag InsertPure(int index, Tag tag)
		{
			if (tag.type != ListType) throw new ArgumentException("Can't add "+tag.type+" to a "+ListType+" TagList", "tag");
			InsertTag(index, tag);
			return tag;
		}
		
		/// <summary>Adds a new Tag to the TagList.</summary>
		/// <param name="value">The value of the Tag to be added.</param>
		/// <returns>The newly created Tag.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If value isn't a valid TagType or value isn't the same TagType as ListType.</exception>
		public Tag Add(object value)
		{
			return Insert(Count, value);
		}
		
		/// <summary>Clones an existing Tag and adds it to the TagList.</summary>
		/// <param name="tag">The Tag to be cloned and added.</param>
		/// <returns>The newly cloned Tag.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If Tag isn't the same TagType as ListType.</exception>
		public Tag Add(Tag tag)
		{
			return Insert(Count, tag);
		}
		
		/// <summary>Adds a new TagList to the TagList.</summary>
		/// <param name="listType">The ListType of the creating TagList.</param>
		/// <returns>The newly created TagList.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If ListType isn't TagList.</exception>
		public Tag AddList(TagType listType)
		{
			return InsertList(Count, listType);
		}
		
		/// <summary>Adds a new TagCompound to the TagList.</summary>
		/// <returns>The newly created TagList.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If ListType isn't TagCompound.</exception>
		public Tag AddCompound()
		{
			return InsertCompound(Count);
		}
		
		/// <summary>Inserts a new Tag into the TagList at the specific index.</summary>
		/// <param name="index">The index at which the Tag should be inserted.</param>
		/// <param name="value">The value of the Tag to be inserted.</param>
		/// <returns>The newly created Tag.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If value isn't a valid TagType or value isn't the same TagType as ListType.</exception>
		public Tag Insert(int index, object value)
		{
			if (!(this is TagList)) throw new NotSupportedException();
			if (value == null) throw new ArgumentNullException("value");
			if (TypeOf(value) == TagType.Invalid) throw new ArgumentException(value.GetType()+" isn't a valid TagType.");
			return InsertPure(index, new Tag(this, null, value));
		}
		
		/// <summary>Clones an existing Tag and inserts it into the TagList at the specific index.</summary>
		/// <param name="index">The index at which the Tag should be inserted.</param>
		/// <param name="tag">The Tag to be cloned and inserted.</param>
		/// <returns>The newly cloned Tag.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If Tag isn't the same TagType as ListType.</exception>
		public Tag Insert(int index, Tag tag)
		{
			if (!(this is TagList)) throw new NotSupportedException();
			if (tag == null) throw new ArgumentNullException("tag");
			return InsertPure(index, tag.Clone(this));
		}
		
		/// <summary>Creates a new TagList and inserts it into the TagList at the specific index.</summary>
		/// <param name="index">The index at which the Tag should be inserted.</param>
		/// <param name="listType">The ListType of the creating TagList.</param>
		/// <returns>The newly created TagList.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If ListType isn't TagList.</exception>
		public Tag InsertList(int index, TagType listType)
		{
			return InsertPure(index, new TagList(this, null, listType));
		}
		
		/// <summary>Creates a new TagCompound and inserts it into the TagList at the specific index.</summary>
		/// <param name="index">The index at which the Tag should be inserted.</param>
		/// <returns>The newly created TagList.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList.</exception>
		/// <exception cref="ArgumentException">If ListType isn't TagCompound.</exception>
		public Tag InsertCompound(int index)
		{
			return InsertPure(index, new TagCompound(this, null));
		}
		#endregion
		
		#region Compound manipulation
		protected virtual void AddTag(string name, Tag tag)
		{
			throw new NotSupportedException();
		}
		Tag AddPure(string name, Tag tag) {
			AddTag(name, tag);
			return tag;
		}
		
		/// <summary>Adds a new Tag to the TagCompound.</summary>
		/// <param name="name">The name of the Tag to be added.</param>
		/// <param name="value">The value of the Tag to be added.</param>
		/// <returns>The newly created Tag.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		/// <exception cref="ArgumentException">If value isn't a valid TagType.</exception>
		public Tag Add(string name, object value)
		{
			if (!(this is TagCompound)) throw new NotSupportedException();
			if (name == null) throw new ArgumentNullException("name");
			if (value == null) throw new ArgumentNullException("value");
			if (TypeOf(value) == TagType.Invalid) throw new ArgumentException(value.GetType()+" isn't a valid TagType.");
			return AddPure(name, new Tag(this, name, value));
		}
		
		/// <summary>Clones an existing Tag and adds it to the TagCompound.</summary>
		/// <param name="name">The name of the Tag to be added.</param>
		/// <param name="tag">The Tag to be cloned and added.</param>
		/// <returns>The newly cloned Tag.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		public Tag Add(string name, Tag tag)
		{
			if (!(this is TagCompound)) throw new NotSupportedException();
			if (name == null) throw new ArgumentNullException("name");
			if (tag == null) throw new ArgumentNullException("tag");
			return AddPure(name, tag.Clone(this, name));
		}
		
		/// <summary>Creates a new TagList and adds it to the TagCompound.</summary>
		/// <param name="name">The name of the Tag to be added.</param>
		/// <param name="listType">The ListType of the creating TagList.</param>
		/// <returns>The newly created TagList.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		public Tag AddList(string name, TagType listType)
		{
			if (name == null) throw new ArgumentNullException("name");
			return AddPure(name, new TagList(this, name, listType));
		}
		
		/// <summary>Creates a new TagCompound and adds it to the TagCompound.</summary>
		/// <param name="name">The name of the Tag to be added.</param>
		/// <returns>The newly created TagCompound.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		public Tag AddCompound(string name)
		{
			if (name == null) throw new ArgumentNullException("name");
			return AddPure(name, new TagCompound(this, name));
		}
		
		/// <summary>Renames a Tag by removing it and adding it again. Evetually removes existing Tag at target.</summary>
		/// <param name="from">The Tag to be renamed.</param>
		/// <param name="to">The target name.</param>
		public void Rename(string from, string to)
		{
			if (!Contains(from)) throw new KeyNotFoundException();
			if (from == to) return;
			if (Contains(to)) this[to].Remove();
			Tag tag = this[from];
			tag.Remove();
			Add(to, tag);
		}
		
		/// <summary>Returns if a Tag with the specific name exists.</summary>
		/// <param name="name">The Tag's name to search for.</param>
		/// <returns>If a Tag with the specific name exists.</returns>
		public virtual bool Contains(string name)
		{
			throw new NotSupportedException();
		}
		#endregion
		
		#region Saving/Loading
		/// <summary>Saves the TagCompound to a file.</summary>
		/// <param name="path">The file to be saved to.</param>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		/// <exception cref="InvalidOperationException">If Tag isn't a named Tag.</exception>
		public void Save(string path)
		{
			using (FileStream fs = File.OpenWrite(path)) Save(fs);
		}
		/// <summary>Saves the TagCompound to a stream.</summary>
		/// <param name="path">The stream to be saved to.</param>
		/// <exception cref="NotSupportedException">If this isn't a TagCompound.</exception>
		/// <exception cref="InvalidOperationException">If Tag isn't a named Tag.</exception>
		public void Save(Stream stream)
		{
			if (!(this is TagCompound)) throw new NotSupportedException();
			if (name == null) throw new InvalidOperationException();
			using (GZipStream gs = new GZipStream(stream, CompressionMode.Compress)) WriteTag(gs);
		}
		void WriteTag(Stream stream)
		{
			if (name != null) {
				stream.WriteByte((byte)type);
				byte[] array = Encoding.UTF8.GetBytes(name);
				stream.Write(Reverse(BitConverter.GetBytes((short)array.Length)), 0, 2);
				stream.Write(array, 0, array.Length);
			}
			switch (type) {
				case TagType.Byte: Write(stream, (byte)value); break;
				case TagType.Short: Write(stream, (short)value); break;
				case TagType.Int: Write(stream, (int)value); break;
				case TagType.Long: Write(stream, (long)value); break;
				case TagType.Float: Write(stream, (float)value); break;
				case TagType.Double: Write(stream, (double)value); break;
				case TagType.Byte_Array: Write(stream, (byte[])value); break;
				case TagType.String: Write(stream, (string)value); break;
				case TagType.List:
					stream.WriteByte((byte)ListType);
					Write(stream, Count);
					foreach (Tag tag in this) tag.WriteTag(stream);
					break;
				case TagType.Compound:
					foreach (Tag tag in this) tag.WriteTag(stream);
					stream.WriteByte(0);
					break;
			}
		}
		
		/// <summary>Loads a TagCompound from a file.</summary>
		/// <param name="path">The file to be loaded from.</param>
		/// <exception cref="FormatException">If root TagType isn't TagCompound.</exception>
		public static Tag Load(string path)
		{
			using (FileStream fs = File.OpenRead(path)) return Load(fs);
		}
		/// <summary>Loads a TagCompound from a stream.</summary>
		/// <param name="path">The stream to be loaded from.</param>
		/// <exception cref="FormatException">If root TagType isn't TagCompound.</exception>
		public static Tag Load(Stream stream)
		{
			using (GZipStream gs = new GZipStream(stream, CompressionMode.Decompress)) {
				Tag root = ReadNamedTag(gs);
				if (root == null) throw new FormatException("Invalid TagType");
				if (root.type != TagType.Compound) throw new FormatException("TagCompound expected");
				return root;
			}
		}
		static Tag ReadNamedTag(Stream stream)
		{
			TagType type = (TagType)ReadByte(stream);
			if (type == TagType.Invalid) return null;
			string name = ReadString(stream);
			Tag tag = ReadTag(stream, type);
			tag.name = name;
			return tag;
		}
		static Tag ReadTag(Stream stream, TagType type)
		{
			switch (type) {
				case TagType.Invalid: throw new FormatException("Invalid TagType");
				case TagType.Byte: return new Tag(ReadByte(stream));
				case TagType.Short: return new Tag(ReadShort(stream));
				case TagType.Int: return new Tag(ReadInt(stream));
				case TagType.Long: return new Tag(ReadLong(stream));
				case TagType.Float: return new Tag(ReadFloat(stream));
				case TagType.Double: return new Tag(ReadDouble(stream));
				case TagType.Byte_Array: return new Tag(ReadByteArray(stream));
				case TagType.String: return new Tag(ReadString(stream));
				case TagType.List:
					TagType listType = (TagType)ReadByte(stream);
					if (listType == TagType.Invalid) throw new FormatException("Invalid TagType");
					int count = ReadInt(stream);
					Tag list = new TagList(listType);
					for (int i = 0; i < count; ++i) {
						Tag tag = ReadTag(stream, listType);
						tag.parent = list;
						list.InsertPure(i, tag);
					} return list;
				case TagType.Compound:
					Tag compound = new TagCompound();
					while (true) {
						Tag tag = ReadNamedTag(stream);
						if (tag == null) break;
						tag.parent = compound;
						compound.AddPure(tag.name, tag);
					}
					return compound;
				default: throw new DivideByZeroException("Scheiß Wetter!");
			}
		}
		#endregion
		
		#region Reading/Writing
		static byte[] Reverse(byte[] array)
		{
			Array.Reverse(array);
			return array;
		}
		
		static byte[] ReadBytes(Stream stream, int length)
		{
			byte[] array = new byte[length];
			if (stream.Read(array, 0, length) < length)
				throw new EndOfStreamException();
			return array;
		}
		static byte ReadByte(Stream stream)
		{
			int b = stream.ReadByte();
			if (b == -1) throw new EndOfStreamException();
			return (byte)b;
		}
		static short ReadShort(Stream stream) { return BitConverter.ToInt16(Reverse(ReadBytes(stream, 2)), 0); }
		static int ReadInt(Stream stream) { return BitConverter.ToInt32(Reverse(ReadBytes(stream, 4)), 0); }
		static long ReadLong(Stream stream) { return BitConverter.ToInt64(Reverse(ReadBytes(stream, 8)), 0); }
		static float ReadFloat(Stream stream) { return BitConverter.ToSingle(Reverse(ReadBytes(stream, 4)), 0); }
		static double ReadDouble(Stream stream) { return BitConverter.ToDouble(Reverse(ReadBytes(stream, 8)), 0); }
		static byte[] ReadByteArray(Stream stream) { return ReadBytes(stream, ReadInt(stream)); }
		static string ReadString(Stream stream) { return Encoding.UTF8.GetString(ReadBytes(stream, ReadShort(stream))); }
		
		static void WriteBytes(Stream stream, byte[] array) { stream.Write(array, 0, array.Length); }
		static void Write(Stream stream, byte value) { stream.WriteByte(value); }
		static void Write(Stream stream, short value) { WriteBytes(stream, Reverse(BitConverter.GetBytes(value))); }
		static void Write(Stream stream, int value) { WriteBytes(stream, Reverse(BitConverter.GetBytes(value))); }
		static void Write(Stream stream, long value) { WriteBytes(stream, Reverse(BitConverter.GetBytes(value))); }
		static void Write(Stream stream, float value) { WriteBytes(stream, Reverse(BitConverter.GetBytes(value))); }
		static void Write(Stream stream, double value) { WriteBytes(stream, Reverse(BitConverter.GetBytes(value))); }
		static void Write(Stream stream, byte[] value) { Write(stream, value.Length); WriteBytes(stream, value); }
		static void Write(Stream stream, string value)
		{
			Write(stream, (short)Encoding.UTF8.GetByteCount(value));
			WriteBytes(stream, Encoding.UTF8.GetBytes(value));
		}
		#endregion
		
		#region Helper methods
		internal Tag Clone(Tag parent)
		{
			return Clone(parent, null);
		}
		internal virtual Tag Clone(Tag parent, string name)
		{
			return new Tag(parent, name, value);
		}
		
		static TagType TypeOf(object value) {
			if (value is byte) return TagType.Byte;
			if (value is short) return TagType.Short;
			if (value is int) return TagType.Int;
			if (value is long) return TagType.Long;
			if (value is float) return TagType.Float;
			if (value is double) return TagType.Double;
			if (value is byte[]) return TagType.Byte_Array;
			if (value is string) return TagType.String;
			return TagType.Invalid;
		}
		#endregion
		
		#region Interface: IEnumerable
		/// <summary>Returns an enumerator that iterates through the TagList or TagCompound.</summary>
		/// <returns>An Enumerator for the TagList or TagCompound.</returns>
		/// <exception cref="NotSupportedException">If this isn't a TagList or TagCompound.</exception>
		public virtual IEnumerator<Tag> GetEnumerator()
		{
			throw new NotSupportedException();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
		
		#region Operator: Tag => [Value]
		public static explicit operator byte(Tag tag) { return (byte)tag.value; }
		public static explicit operator short(Tag tag) { return (short)tag.value; }
		public static explicit operator int(Tag tag) { return (int)tag.value; }
		public static explicit operator long(Tag tag) { return (long)tag.value; }
		public static explicit operator float(Tag tag) { return (float)tag.value; }
		public static explicit operator double(Tag tag) { return (double)tag.value; }
		public static explicit operator byte[](Tag tag)
		{
			if (tag.value == null) throw new NullReferenceException();
			return (byte[])tag.value;
		}
		public static explicit operator string(Tag tag)
		{
			if (tag.value == null) throw new NullReferenceException();
			return (string)tag.value;
		}
		#endregion
	}
}