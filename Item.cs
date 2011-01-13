using System;
using System.Drawing;

namespace INVedit
{
	public class Item
	{
		Data.Item item {
			get {
				if (Data.items.ContainsKey(ID)) {
					if (Data.items[ID].ContainsKey(Damage))
						return Data.items[ID][Damage];
					else return Data.items[ID][0];
				} else return null;
			}
		}
		
		public short ID { get; set; }
		public byte Count { get; set; }
		public byte Slot { get; set; }
		public short Damage { get; set; }
		
		public bool Known { get { return (item != null); } }
		public bool Alternative {
			get {
				if (!Known) return false;
				return (Data.items[ID].Count > 1);
			}
		}
		public string Name {
			get {
				if (!Known) return "Unknown item "+ID;
				return item.name;
			}
		}
		public byte Stack {
			get {
				if (!Known) return 64;
				return item.stack;
			}
		}
		public short MaxDamage {
			get {
				if (!Known) return 0;
				return item.maxDamage;
			}
		}
		public Image Image {
			get {
				if (!Known) return Data.unknown;
				return Data.list.Images[item.imageIndex];
			}
		}
		
		public Item(short id)
			: this(id, 1, 0, 0) {  }
		public Item(short id, byte count)
			: this(id, count, 0, 0) {  }
		public Item(short id, byte count, byte slot)
			: this(id, count, slot, 0) {  }
		public Item(short id, byte count, byte slot, short damage)
		{
			ID = id;
			Count = count;
			Slot = slot;
			Damage = damage;
		}
	}
}
