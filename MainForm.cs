using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Resources;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;

using NBT;

namespace INVedit
{
	public partial class MainForm : Form
	{
		static string appdata;
		static MainForm() {
			if (Platform.Current == Platform.Windows)
				appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"/.minecraft";
			else if (Platform.Current == Platform.Mac)
				appdata = Environment.GetEnvironmentVariable("HOME")+"/Library/Application Support/minecraft";
			else
				appdata = Environment.GetEnvironmentVariable("HOME")+"/.minecraft";
		}
		
		List<CheckBox> groups = new List<CheckBox>();
		
		string url = "http://copy.mcft.net/mc/INVedit";
		WebClient client = new WebClient();
		List<string> download;
		List<byte[]> files;
		int current;
		
		public MainForm(string[] files)
		{
			InitializeComponent();
			
			Data.Init("items.txt");
			
			client.DownloadStringCompleted += VersionCompleted;
			client.DownloadDataCompleted += FileCompleted;
			client.DownloadProgressChanged += FileProgress;
			
			boxItems.LargeImageList = Data.list;
			boxItems.ItemDrag += ItemDrag;
			
			foreach (Data.Group group in Data.groups.Values) {
				CheckBox box = new CheckBox();
				box.Size = new Size(26, 26);
				box.Location = new Point(Width-189, 29 + groups.Count*27);
				box.ImageList = Data.list;
				box.ImageIndex = group.imageIndex;
				box.Appearance = Appearance.Button;
				box.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				box.Checked = true;
				box.Tag = group;
				box.MouseDown += ItemMouseDown;
				Controls.Add(box);
				groups.Add(box);
			}
			
			UpdateItems();
			
			foreach (string file in files)
				if (File.Exists(file)) Open(file);
		}
		
		void Open(string file)
		{
			Page page = new Page();
			Open(page,file);
			tabControl.TabPages.Add(page);
			tabControl.SelectedTab = page;
		}
		
		void Open(Page page, string file)
		{
			try {
				FileInfo info = new FileInfo(file);
				page.file = info.FullName;
				if (info.Name == "level.dat") { page.Text = info.Directory.Name; }
				else { page.Text = info.Name; }
				Tag tag = NBT.Tag.Load(file);
				if (tag.Type==TagType.Compound && tag.Contains("Data")) { tag = tag["Data"]; }
				if (tag.Type==TagType.Compound && tag.Contains("Player")) { tag = tag["Player"]; }
				if (tag.Type==TagType.Compound && tag.Contains("Inventory")) { tag = tag["Inventory"]; }
				if (tag.Name != "Inventory") { throw new Exception("Can't find Inventory tag."); }
				Inventory.Load(tag, page.slots);
				Text = "INVedit - "+page.Text;
				btnSave.Enabled = true;
				btnCloseTab.Enabled = true;
				btnReload.Enabled = true;
			} catch (Exception ex) { MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
		}
		
		void Save(Page page, string file)
		{
			try {
				FileInfo info = new FileInfo(file);
				page.file = info.FullName;
				Tag root,tag;
				if (info.Exists) {
					root = NBT.Tag.Load(page.file);
					tag = root;
				} else {
					if (info.Extension.ToLower() == ".dat") {
						MessageBox.Show("You can create a new Minecraft file. Select an existing one instead.",
						                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					} root = NBT.Tag.Create("Inventory");
					tag = root;
				} if (tag.Type==TagType.Compound && tag.Contains("Data")) { tag = tag["Data"]; }
				if (tag.Type==TagType.Compound && tag.Contains("Player")) { tag = tag["Player"]; }
				if (root.Name!="Inventory" && (tag.Type!=TagType.Compound || !tag.Contains("Inventory"))) { throw new Exception("Can't find Inventory tag."); }
				Inventory.Save(tag, page.slots);
				root.Save(page.file);
				if (info.Name == "level.dat") { page.Text = info.Directory.Name; }
				else { page.Text = info.Name; }
				Text = "INVedit - "+page.Text;
				btnReload.Enabled = true;
			} catch (Exception ex) { MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
		}
		
		protected override void OnDragEnter(DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = ((string[])e.Data.GetData(DataFormats.FileDrop));
				foreach (string file in files) {
					FileInfo info = new FileInfo(file);
					if (info.Extension.ToLower() == ".inv" || info.Extension.ToLower() == ".dat")
						e.Effect = DragDropEffects.Copy;
				}
			}
		}
		protected override void OnDragDrop(DragEventArgs e) {
			OnDragEnter(e);
			BringToFront();
			if (e.Effect == DragDropEffects.None) return;
			string[] files = ((string[])e.Data.GetData(DataFormats.FileDrop));
			foreach (string file in files)
				if (File.Exists(file)) Open(file);
		}
		
		void UpdateItems()
		{
			boxItems.BeginUpdate();
			boxItems.Clear();
			if (boxSearch.Text == "" || boxSearch.Font.Italic) {
				foreach (CheckBox box in groups) if (box.Checked)
					foreach (Data.Item item in ((Data.Group)box.Tag).items)
						boxItems.Items.Add(new ListViewItem(item.name, item.imageIndex){ Tag = new Item(item.id, 0, 0, item.damage) });
			} else {
				short id;
				if (short.TryParse(boxSearch.Text, out id)) {
					if (Data.items.ContainsKey(id))
						foreach (Data.Item item in Data.items[id].Values)
							boxItems.Items.Add(new ListViewItem(item.name, item.imageIndex){ Tag = new Item(item.id, 0, 0, item.damage) });
					else boxItems.Items.Add(new ListViewItem("Unknown item "+id, 0){ Tag = new Item(id) });
				} else foreach (CheckBox box in groups) if (box.Checked)
					foreach (Data.Item item in ((Data.Group)box.Tag).items)
						if (item.name.IndexOf(boxSearch.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
							boxItems.Items.Add(new ListViewItem(item.name, item.imageIndex){ Tag = new Item(item.id, 0, 0, item.damage) });
			}
			boxItems.EndUpdate();
		}
		
		void ItemMouseDown(object sender, MouseEventArgs e)
		{
			CheckBox self = (CheckBox)sender;
			bool changed = false;
			if (e.Button == MouseButtons.Left) {
				bool other = true;
				foreach (CheckBox box in groups)
					if (box.Checked == (self!=box))
						other = false;
				foreach (CheckBox box in groups)
					if (box.Checked == (self!=box) || other) {
					changed = true;
					box.Checked = (self==box) || other;
				}
			} else if (e.Button == MouseButtons.Right) {
				self.Checked = !self.Checked;
				changed = true;
			} else return;
			self.Select();
			if (changed) UpdateItems();
			if (e.Button == MouseButtons.Left)
				self.Checked = !self.Checked;
		}
		
		void BoxSearchEnter(object sender, EventArgs e)
		{
			if (!boxSearch.Font.Italic) return;
			boxSearch.Font = new Font(boxSearch.Font, FontStyle.Regular);
			boxSearch.ForeColor = SystemColors.ControlText;
			boxSearch.Text = "";
		}
		
		void BoxSearchLeave(object sender, EventArgs e)
		{
			if (boxSearch.Text != "") return;
			boxSearch.Font = new Font(boxSearch.Font, FontStyle.Italic);
			boxSearch.ForeColor = Color.Gray;
			boxSearch.Text = "Search...";
		}
		
		void BoxSearchTextChanged(object sender, EventArgs e)
		{
			UpdateItems();
		}
		
		void ItemDrag(object sender, ItemDragEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;
			Item item = (Item)((ListViewItem)e.Item).Tag;
			item = new Item(item.ID, 1, 0, item.Damage);
			DoDragDrop(item, DragDropEffects.Copy | DragDropEffects.Move);
		}
		
		void BtnNewClick(object sender, EventArgs e)
		{
			Page page = new Page();
			page.Text = "unnamed.inv";
			Text = "INVedit - unnamed.inv";
			tabControl.TabPages.Add(page);
			tabControl.SelectedTab = page;
			btnSave.Enabled = true;
			btnCloseTab.Enabled = true;
			btnReload.Enabled = false;
		}
		
		void BtnOpenClick(object sender, EventArgs e)
		{
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				Open(openFileDialog.FileName);
			}
		}
		
		void BtnSaveClick(object sender, EventArgs e)
		{
			Page page = (Page)tabControl.SelectedTab;
			saveFileDialog.FileName = page.file;
			if (saveFileDialog.ShowDialog() == DialogResult.OK) {
				Save(page, saveFileDialog.FileName);
			}
		}
		
		void BtnCloseTabClick(object sender, EventArgs e)
		{
			tabControl.TabPages.Remove(tabControl.SelectedTab);
			if (tabControl.TabPages.Count == 0) {
				btnSave.Enabled = false;
				btnCloseTab.Enabled = false;
			}
		}
		
		void BtnAboutClick(object sender, EventArgs e)
		{
			new AboutForm().ShowDialog();
		}
		
		void TabControlDragOver(object sender, DragEventArgs e)
		{
			Point point = tabControl.PointToClient(new Point(e.X, e.Y));
			TabPage hover = null;
			for (int i = 0; i < tabControl.TabPages.Count; ++i)
				if (tabControl.GetTabRect(i).Contains(point)) {
				hover = tabControl.TabPages[i]; break;
			}
			if (hover == null) return;
			if (!e.Data.GetDataPresent(typeof(Item))) return;
			tabControl.SelectedTab = hover;
		}
		
		void TabControlSelected(object sender, TabControlEventArgs e)
		{
			if (e.TabPage != null) {
				Text = "INVedit - "+e.TabPage.Text;
				btnReload.Enabled = (((Page)e.TabPage).file != null);
			} else {
				Text = "INVedit - Minecraft Inventory Editor";
				btnReload.Enabled = false;
			}
			
		}
		
		void BtnReloadClick(object sender, EventArgs e)
		{
			try {
				Page page = (Page)tabControl.SelectedTab;
				Open(page, page.file);
			} catch (Exception ex) { MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
		}
		
		void BtnOpenDropDownOpening(object sender, EventArgs e)
		{
			btnOpen.DropDownItems.Clear();
			ResourceManager resources = new ResourceManager("INVedit.Resources", GetType().Assembly);
			for (int i=1;i<=5;i+=1) {
				ToolStripItem item = btnOpen.DropDownItems.Add("Open World"+i);
				item.Image = (Image)resources.GetObject("world"+i);
				string file = appdata+"/saves/World"+i+"/level.dat";
				item.Enabled = File.Exists(file);
				item.Tag = file;
			} Image world = (Image)resources.GetObject("world");
			DirectoryInfo dirs = new DirectoryInfo(appdata+"/saves");
			if (dirs.Exists) foreach (DirectoryInfo dir in dirs.GetDirectories()) {
				if (dir.GetFiles("level.dat").Length > 0 && dir.Name != "World1" && dir.Name != "World2" &&
				    dir.Name != "World3" && dir.Name != "World4" && dir.Name != "World5") {
					ToolStripItem item = btnOpen.DropDownItems.Add("Open "+dir.Name, world);
					item.Tag = dir.FullName+"/level.dat";
				}
			} if (btnOpen.DropDownItems.Count>5) { btnOpen.DropDownItems.Insert(5, new ToolStripSeparator()); }
		}
		
		void BtnOpenDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			Open((string)e.ClickedItem.Tag);
		}
		
		void BtnSaveDropDownOpening(object sender, EventArgs e)
		{
			btnSave.DropDownItems.Clear();
			ResourceManager resources = new ResourceManager("INVedit.Resources", GetType().Assembly);
			for (int i=1;i<=5;i+=1) {
				ToolStripItem item = btnSave.DropDownItems.Add("Save World"+i);
				item.Image = (Image)resources.GetObject("world"+i);
				string file = appdata+"/saves/World"+i+"/level.dat";
				item.Enabled = File.Exists(file);
				item.Tag = file;
			} Image world = (Image)resources.GetObject("world");
			foreach (DirectoryInfo dir in new DirectoryInfo(appdata+"/saves").GetDirectories()) {
				if (dir.GetFiles("level.dat").Length > 0 && dir.Name != "World1" && dir.Name != "World2" &&
				    dir.Name != "World3" && dir.Name != "World4" && dir.Name != "World5") {
					ToolStripItem item = btnSave.DropDownItems.Add("Save "+dir.Name, world);
					item.Tag = dir.FullName+"/level.dat";
				}
			} if (btnOpen.DropDownItems.Count>5) { btnSave.DropDownItems.Insert(5, new ToolStripSeparator()); }
		}
		
		void BtnSaveDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			Page page = (Page)tabControl.SelectedTab;
			Save(page, (string)e.ClickedItem.Tag);
		}
		
		void BtnUpdateClick(object sender, EventArgs e)
		{
			switch (btnUpdate.Text) {
				case "Check for updates":
					btnUpdate.Text = "Checking ...";
					btnUpdate.Enabled = false;
					client.DownloadStringAsync(new Uri(url+"/version"));
					break;
				case "Download":
					btnUpdate.Text = "";
					btnUpdate.Enabled = false;
					barUpdate.Visible = true;
					barUpdate.Value = 0;
					barUpdate.Maximum = download.Count*100;
					int current = 0;
					files = new List<byte[]>();
					Uri uri = new Uri(url+"/"+download[current]);
					client.DownloadDataAsync(uri);
					break;
				case "Restart":
					for (int i=0;i<download.Count;++i) {
						string name = download[i];
						byte[] data = files[i];
						if (name == "INVedit.exe") { name = "_"+name; }
						File.WriteAllBytes(name,data);
					} if (File.Exists("_INVedit.exe")) {
						Process.Start("_INVedit.exe", "-update");
					} else { Process.Start("INVedit.exe"); }
					Application.Exit();
					break;
			}
		}
		void VersionCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			if (e.Error!=null) {
				btnUpdate.Text = "Error while checking for updates";
				return;
			} int version = 1;
			string[] lines = e.Result.Split(new string[]{ "\r\n" }, StringSplitOptions.None);
			download = new List<string>();
			foreach (string line in lines) {
				if (line == "") ++version;
				else if (version > Data.version &&
				         !download.Contains(line)) download.Add(line);
			} if (download.Count > 0) {
				btnUpdate.Text = "Download";
				btnUpdate.Enabled = true;
			} else {
				btnUpdate.Text = "No update available";
			}
		}
		void FileProgress(object sender, DownloadProgressChangedEventArgs e)
		{
			barUpdate.Value = current*100 + e.ProgressPercentage;
		}
		void FileCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			if (e.Error!=null) {
				btnUpdate.Text = "Download";
				btnUpdate.Enabled = true;
				barUpdate.Visible = false;
				throw e.Error;
			} files.Add(e.Result);
			++current;
			if (current == download.Count) {
				btnUpdate.Text = "Restart";
				btnUpdate.Enabled = true;
				barUpdate.Visible = false;
			} else {
				Uri uri = new Uri(url+"/"+download[current]);
				client.DownloadDataAsync(uri);
			}
		}
	}
}
