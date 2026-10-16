using System;
using System.Windows.Forms;
using PPTimizNext;
class Smoke {
 static int radios,selected; static string choice;
 static void Visit(Control parent) { foreach(Control c in parent.Controls) { var r=c as RadioButton; if(r!=null) { radios++; if(r.Checked) { selected++; choice=r.Text; } } Visit(c); } }
 [STAThread] static int Main() {
  using(var f=new Launcher(null)) { f.Show(); Application.DoEvents(); Visit(f);
   if(!f.Visible || !f.AllowDrop || radios!=3 || selected!=1 || choice!="Full HD") return 1;
   f.Close(); Console.WriteLine("PASS: independent window, file drop, three mutually exclusive profiles, Full HD default.");
  } return 0;
 }
}
