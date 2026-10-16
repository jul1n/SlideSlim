using System;
using System.Windows.Forms;
using PPTimizNext;
class Smoke {
 [STAThread] static int Main() {
  using(var f=new Launcher(null)) {
   f.Show(); Application.DoEvents();
   if(!f.Visible || !f.AllowDrop || f.Controls.Count<8) return 1;
   bool profiles=false; foreach(Control c in f.Controls) { var box=c as ComboBox; if(box!=null) profiles=box.Items.Count==3 && box.SelectedIndex==1; }
   if(!profiles) return 2;
   f.Close(); Console.WriteLine("PASS: independent window opens, file drop enabled, three profiles available, Full HD default.");
  } return 0;
 }
}
