using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Resources;

namespace INVedit
{
	internal static class Data
	{
		static readonly Dictionary<string, Image> images = new Dictionary<string, Image>();
		public static readonly ImageList list = new ImageList(){ ColorDepth = ColorDepth.Depth32Bit };
		public static readonly Items items = new Items();
		public static readonly Dictionary<string, Group> groups = new Dictionary<string, Group>();
		public static Image unknown;
		public static int version = int.MinValue;
		
		public static void Init(string path)
		{
			ResourceManager resources = new ResourceManager("INVedit.Resources", typeof(Data).Assembly);
			unknown = (Image)resources.GetObject("unknown");
			list.Images.Add(unknown);
			
			string[] lines = File.ReadAllLines(path);
			for (int i = 1; i <= lines.Length; ++i) {
				try {
					string line = lines[i-1].TrimStart();
					if (line=="" || line[0]=='#') continue;
					string[] split;
					if (line[0] == ':') {
						split = line.Split(new char[]{' '}, 2, StringSplitOptions.RemoveEmptyEntries);
						switch (split[0].ToLower()) {
							case ":version":
								try { version = int.Parse(split[1]); }
								catch (Exception e) { throw new DataException("Failed to parse option "+split[0]+" at line "+i+" in file '"+path+"'.", e); }
								break;
							default:
								throw new DataException("Unknown option '"+split[0]+"' at line "+i+" in file '"+path+"'.");
						} continue;
					}
					split = line.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
					Exception ex = new DataException("Invalid number of colums at line "+i+" in file '"+path+"'.");
					if (line[0]=='~') { if (split.Length != 4) throw ex; }
					else { if (split.Length < 4 || split.Length > 5) throw ex; }
					string name = split[1].Replace('_', ' ');
					if (line[0]=='~') {
						short icon;
						try { icon = short.Parse(split[2]); }
						catch (Exception e) { throw new DataException("Failed to parse column 'ICON' at line "+i+" in file '"+path+"'.", e); }
						if (!items.ContainsKey(icon)) throw new DataException("Invalid item id '"+icon+"' in column 'ICON' at line "+i+" in file '"+path+"'.");
						int imageIndex = items[icon][0].imageIndex;
						string[] l = split[3].Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
						Group group = new Group(name, imageIndex);
						foreach (string nn in l) {
							string n = nn;
							short s;
							short d = -1;
							if (n.IndexOf('~') != -1) {
								string[] sp = n.Split('~');
								n = sp[0];
								try { d = short.Parse(sp[1]); }
								catch (Exception e) { throw new DataException("Failed to parse column 'ITEMS' at line "+i+" in file '"+path+"'.", e); }
							}
							try { s = short.Parse(n); }
							catch (Exception e) { throw new DataException("Failed to parse column 'ITEMS' at line "+i+" in file '"+path+"'.", e); }
							if (items.ContainsKey(s)) {
								if (d == -1) foreach (Item item in items[s].Values) group.Add(item);
								else if (items[s].ContainsKey(d)) group.Add(items[s][d]);
								else MessageBox.Show("Invalid item damage '"+d+"' in column 'ITEMS' at line "+i+" in file '"+path+"'.",
								                     "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							} else MessageBox.Show("Invalid item id '"+s+"' in column 'ITEMS' at line "+i+" in file '"+path+"'.",
							                       "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						}
						groups.Add(name, group);
					} else {
						Image image;
						try { image = LoadImage(split[2]); }
						catch (Exception e) { throw new DataException("Failed to load image '"+split[2]+"' at line "+i+" in file '"+path+"' ("+e.Message+").", e); }
						short id;
						try { id = short.Parse(split[0]); }
						catch (Exception e) { throw new DataException("Failed to parse column 'ID' at line "+i+" in file '"+path+"'.", e); }
						string[] cords = split[3].Split(',');
						if (cords.Length != 2) throw new DataException("Failed to parse column 'CORDS' at line "+i+" in file '"+path+"'.");
						int x, y;
						try {
							x = int.Parse(cords[0]);
							y = int.Parse(cords[1]);
						} catch (Exception e) { throw new DataException("Failed to parse column 'CORDS' at line "+i+" in file '"+path+"'.", e); }
						if (x < 0 || y < 0 || x*16+16 > image.Width || y*16+16 > image.Height)
							throw new DataException("Invalid image cords "+x+","+y+" at line "+i+" in file '"+path+"'.");
						byte stack = 64;
						short damage = 0;
						short maxDamage = 0;
						if (split.Length == 5) {
							string str = split[4];
							char chr = ' ';
							if (str[0]=='+' || str[0]=='x') {
								chr = str[0];
								str = str.Substring(1, str.Length-1);
							}
							short val;
							try { val = short.Parse(str); }
							catch (Exception e) { throw new DataException("Failed to parse column 'DAMAGE' at line "+i+" in file '"+path+"'.", e); }
							switch (chr) {
								case '+':
									if (val <= 0) throw new DataException("Failed to parse column 'DAMAGE' at line "+i+" in file '"+path+"'.");
									stack = 1;
									maxDamage = val;
									break;
								case 'x':
									if (val <= 0) throw new DataException("Failed to parse column 'DAMAGE' at line "+i+" in file '"+path+"'.");
									if (val > 255) throw new DataException("Failed to parse column 'DAMAGE' at line "+i+" in file '"+path+"'.");
									stack = (byte)val;
									break;
								default:
									damage = val;
									break;
							}
						}
						items.Add(new Item(id, name, stack, damage, maxDamage, image, x, y));
					}
				} catch (Exception e) {
					if (MessageBox.Show(e.Message, "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
						return;
				}
			}
		}
		
		static Image LoadImage(string path)
		{
			if (images.ContainsKey(path)) return images[path];
			using (Bitmap bmp = new Bitmap(path)) {
				Image image = new Bitmap(bmp);
				images[path] = image;
				return image;
			}
		}
		
		internal class Items : Dictionary<short, Dictionary<short, Item>>
		{
			public void Add(Item item)
			{
				Dictionary<short, Item> list;
				if (ContainsKey(item.id)) list = this[item.id];
				else { list = new Dictionary<short, Item>(); Add(item.id, list); }
				list.Add(item.damage, item);
			}
		}
		
		internal class Item
		{
			public readonly short id;
			public readonly string name;
			public readonly byte stack;
			public readonly short damage;
			public readonly short maxDamage;
			public readonly int imageIndex;
			
			internal Item(short id, string name, byte stack, short damage, short maxDamage, Image image, int x, int y)
			{
				this.id = id;
				this.name = name;
				this.stack = stack;
				this.damage = damage;
				this.maxDamage = maxDamage;
				
				Bitmap b = new Bitmap(16, 16);
				using (Graphics g = Graphics.FromImage(b))
					g.DrawImage(image, new Rectangle(0, 0, 16, 16), new Rectangle(x*16, y*16, 16, 16), GraphicsUnit.Pixel);
				
				imageIndex = Data.list.Images.Count;
				Data.list.Images.Add(b);
			}
		}
		
		internal class Group
		{
			public readonly string name;
			public readonly int imageIndex;
			public readonly List<Item> items = new List<Item>();
			
			internal Group(string name, int imageIndex)
			{
				this.name = name;
				this.imageIndex = imageIndex;
			}
			
			internal void Add(Item item) {
				items.Add(item);
			}
		}
	}
	
	public class DataException : Exception
	{
		public DataException() : base() {  }
		public DataException(string message) : base(message) {  }
		public DataException(string message, Exception innerException) : base(message, innerException) {  }
	}
}
