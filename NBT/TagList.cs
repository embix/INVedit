using System;
using System.Collections.Generic;

namespace NBT
{
	class TagList : Tag
	{
		internal TagType listType;
		internal List<Tag> list = new List<Tag>();
		
		#region Public members
		public override int Count {
			get { return list.Count; }
		}
		public override TagType ListType {
			get { return listType; }
		}
		
		public override Tag this[int index] {
			get { return list[index]; }
		}
		#endregion
		
		#region Constructor
		internal TagList(TagType listType) : this(null, null, listType) {  }
		internal TagList(Tag parent, string name, TagType listType) : base(parent, name, null)
		{
			this.listType = listType;
			type = TagType.List;
		}
		#endregion
		
		#region Manipulation
		protected override void Remove(Tag tag)
		{
			list.Remove(tag);
		}
		
		protected override void InsertTag(int index, Tag tag)
		{
			list.Insert(index, tag);
		}
		#endregion
		
		#region Helper methods
		internal override Tag Clone(Tag parent, string name)
		{
			Tag tag = new TagList(parent, name, ListType);
			foreach (Tag t in this) tag.Add(t);
			return tag;
		}
		#endregion
		
		#region Interface: IEnumerator
		public override IEnumerator<Tag> GetEnumerator()
		{
			return list.GetEnumerator();
		}
		#endregion
	}
}
