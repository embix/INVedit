using System;
using System.Collections.Generic;

namespace NBT
{
	class TagCompound : Tag
	{
		internal Dictionary<string, Tag> dict = new Dictionary<string, Tag>();
		
		#region Public members
		public override int Count {
			get { return dict.Count; }
		}
		
		public override Tag this[string name] {
			get { return dict[name]; }
		}
		#endregion
		
		#region Constructor
		internal TagCompound() : this(null, null) {  }
		internal TagCompound(Tag parent, string name) : base(parent, name, null)
		{
			type = TagType.Compound;
		}
		#endregion
		
		#region Manipulation
		protected override void Remove(Tag tag)
		{
			dict.Remove(tag.Name);
		}
		protected override void AddTag(string name, Tag tag)
		{
			dict.Add(name, tag);
		}
		
		public override bool Contains(string name)
		{
			return dict.ContainsKey(name);
		}
		#endregion
		
		#region Helper methods
		internal override Tag Clone(Tag parent, string name)
		{
			Tag tag = new TagCompound(parent, name);
			foreach (Tag t in this) tag.Add(t.Name, t);
			return tag;
		}
		#endregion
		
		#region Interface: IEnumerator
		public override IEnumerator<Tag> GetEnumerator()
		{
			return dict.Values.GetEnumerator();
		}
		#endregion
	}
}
