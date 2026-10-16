using System;
using System.IO;
using System.Windows.Forms;
namespace PPTimizNext {
 static class Program {
  [STAThread] static int Main(string[] args) {
   Application.EnableVisualStyles();
   if(args.Length==0 || args[0]!="--from-powerpoint") { Application.Run(new Launcher(args.Length>0?args[0]:null)); return 0; }
   string snapshot=null;
   try {
    string input;
    string original=null;
    if(args.Length==3 && args[0]=="--from-powerpoint") { snapshot=Path.GetFullPath(args[1]); input=snapshot; original=Path.GetFullPath(args[2]); }
    else if(args.Length>0) input=Path.GetFullPath(args[0]);
    else using(var dialog=new OpenFileDialog { AutoUpgradeEnabled=false,Filter="PowerPoint|*.pptx;*.pptm",Title="Choisir la présentation à optimiser" }) { if(dialog.ShowDialog()!=DialogResult.OK) return 0; input=dialog.FileName; }
    CompressionProfile? standard=UI.Choose(); if(!standard.HasValue) return 0;
    string output=UI.Destination(original??input); if(output==null) return 0;
    Cursor.Current=Cursors.WaitCursor;
    var result=Engine.Optimize(input,output,standard.Value);
    Cursor.Current=Cursors.Default; MessageBox.Show(result.Summary(),"PPTimiz Next"); return 0;
   } catch(Exception ex) { UI.Error(ex); return 1; }
   finally { if(snapshot!=null && Path.GetDirectoryName(snapshot).Equals(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase) && Path.GetFileName(snapshot).StartsWith("pptimiz-",StringComparison.Ordinal) && File.Exists(snapshot)) { try { File.Delete(snapshot); } catch {} } }
  }
 }
}

