using System;
using System.Windows.Forms;
using System.IO;

namespace INVedit
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm(args));
		}
	}
}
