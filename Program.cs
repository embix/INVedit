using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace INVedit
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			
			try { Assembly.Load("NBT"); }
			catch { MessageBox.Show("Error", "Couldn't load NBT.dll."); return; }
			
			if (args.Length > 0) switch (args[0]) {
				case "-update":
					while (true) {
						try { File.Delete("INVedit.exe"); break; } catch {  }
						Thread.Sleep(0);
					} File.Copy("_INVedit.exe", "INVedit.exe");
					Process.Start("INVedit.exe", "-finish");
					return;
				case "-finish":
					while (true) {
						try { File.Delete("_INVedit.exe"); break; } catch {  }
						Thread.Sleep(0);
					} break;
			}
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm(args));
		}
	}
}
