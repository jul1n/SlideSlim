using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: ComVisible(false)]
namespace PPTimizNext {
 [ComImport, Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744"), TypeLibType(4160), InterfaceType(ComInterfaceType.InterfaceIsDual)]
 public interface IExtensibility {
  [DispId(1)] void OnConnection([In,MarshalAs(UnmanagedType.IDispatch)] object application,[In] int mode,[In,MarshalAs(UnmanagedType.IDispatch)] object instance,[In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
  [DispId(2)] void OnDisconnection([In] int mode,[In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
  [DispId(3)] void OnAddInsUpdate([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
  [DispId(4)] void OnStartupComplete([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
  [DispId(5)] void OnBeginShutdown([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)] ref Array custom);
 }
 [ComImport, Guid("000C0396-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
 public interface IRibbon { [DispId(1)] [return:MarshalAs(UnmanagedType.BStr)] string GetCustomUI([In,MarshalAs(UnmanagedType.BStr)] string ribbonID); }
 [ComVisible(true), Guid("BB9E5421-19BE-4C58-9415-E28F11551B1A"), ProgId("PPTimizNext.Connect"), ClassInterface(ClassInterfaceType.AutoDispatch)]
 public sealed class AddIn : IExtensibility, IRibbon {
  object application; bool busy;
  public void OnConnection(object app,int mode,object instance,ref Array custom) { application=app; Trace("OnConnection"); }
  public void OnDisconnection(int mode,ref Array custom) { application=null; }
  public void OnAddInsUpdate(ref Array custom) {} public void OnStartupComplete(ref Array custom) {} public void OnBeginShutdown(ref Array custom) {}
  static void Trace(string text) { try { File.AppendAllText(Path.Combine(Path.GetDirectoryName(typeof(AddIn).Assembly.Location),"addin-load.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine); } catch {} }
  public string GetCustomUI(string id) { Trace("GetCustomUI: "+id); return "<customUI xmlns='http://schemas.microsoft.com/office/2006/01/customui'><ribbon><tabs><tab idMso='TabHome'><group id='pptimizNextGroup' label='PPTimiz Next'><button id='pptimizOptimize' label='Optimiser une copie' size='large' imageMso='PicturesCompress' onAction='OnOptimize'/><button id='pptimizAccessibility' label='Vérifier l’accessibilité' imageMso='GroupAccessibility' onAction='OnAccessibility'/></group></tab></tabs></ribbon></customUI>"; }
  public void OnAccessibility(object control) { try { dynamic app=application; app.CommandBars.ExecuteMso("AccessibilityChecker"); } catch(Exception ex) { UI.Error(ex); } }
  public void OnOptimize(object control) {
   if(busy) return; string snapshot=null;
   try {
    dynamic app=application; dynamic presentation=app.ActivePresentation;
    string original=(string)presentation.FullName; string ext=Path.GetExtension(original).ToLowerInvariant();
    if(ext!=".pptx" && ext!=".pptm") throw new NotSupportedException("Enregistrez d’abord la présentation au format .pptx ou .pptm.");
    CompressionProfile? standard=UI.Choose(); if(!standard.HasValue) return;
    string destination=UI.Destination(original); if(destination==null) return;
    snapshot=Path.Combine(Path.GetTempPath(),"pptimiz-"+Guid.NewGuid().ToString("N")+ext);
    busy=true; Cursor.Current=Cursors.WaitCursor;
    // All Office calls remain on the Office callback thread. SaveCopyAs includes current edits.
    presentation.SaveCopyAs(snapshot,ext==".pptm"?25:24,-1);
    var result=Engine.Optimize(snapshot,destination,standard.Value);
    MessageBox.Show(result.Summary(),"PPTimiz Next",MessageBoxButtons.OK,MessageBoxIcon.Information);
   } catch(Exception ex) { UI.Error(ex); }
   finally { busy=false; Cursor.Current=Cursors.Default; if(snapshot!=null && File.Exists(snapshot)) { try { File.Delete(snapshot); } catch {} } }
  }
 }
 public static class UI {
  public static void Error(Exception ex) { MessageBox.Show("Traitement interrompu. L’original est conservé.\n\n"+ex.Message,"PPTimiz Next",MessageBoxButtons.OK,MessageBoxIcon.Error); }
  public static CompressionProfile? Choose() {
   using(var form=new Form { Text="PPTimiz Next", Width=550, Height=310, FormBorderStyle=FormBorderStyle.FixedDialog, StartPosition=FormStartPosition.CenterScreen, MaximizeBox=false, MinimizeBox=false }) {
    var title=new Label { Left=20,Top=15,Width=500,Height=45,Text="Optimiser une copie et conserver l’original.\nUne synthèse sera affichée à la fin." };
    var standard=new RadioButton { Left=20,Top=70,Width=500,Text="Standard — projection, 150 ppp" };
    var compact=new RadioButton { Left=20,Top=105,Width=500,Text="Intermédiaire Full HD — taille affichée en 1080p pixels",Checked=true };
    var maximum=new RadioButton { Left=20,Top=140,Width=500,Text="Maximum HD — 1 280 × 720 pixels, compression renforcée" };
    var note=new Label { Left=20,Top=177,Width=500,Height=30,Text="Le mode Maximum réduit davantage la qualité des images." };
    var ok=new Button { Left=310,Top=220,Width=100,Text="Optimiser",DialogResult=DialogResult.OK };
    var cancel=new Button { Left=420,Top=220,Width=100,Text="Annuler",DialogResult=DialogResult.Cancel };
    form.Controls.AddRange(new Control[]{title,standard,compact,maximum,note,ok,cancel}); form.AcceptButton=ok; form.CancelButton=cancel;
    return form.ShowDialog()==DialogResult.OK?(CompressionProfile?)(standard.Checked?CompressionProfile.Standard:maximum.Checked?CompressionProfile.MaximumHD:CompressionProfile.FullHD):null;
   }
  }
  public static string Destination(string input) {
   using(var form=new Form { Text="Enregistrer la copie optimisée",Width=720,Height=210,FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterScreen,MaximizeBox=false,MinimizeBox=false }) {
    var label=new Label { Left=20,Top=20,Width=660,Height=25,Text="Chemin de la nouvelle copie (modifiable) :" };
    var path=new TextBox { Left=20,Top=55,Width=660,Text=Engine.UniqueOutput(input) };
    var ok=new Button { Left=450,Top=110,Width=110,Text="Enregistrer",DialogResult=DialogResult.OK };
    var cancel=new Button { Left=570,Top=110,Width=110,Text="Annuler",DialogResult=DialogResult.Cancel };
    form.Controls.AddRange(new Control[]{label,path,ok,cancel}); form.AcceptButton=ok; form.CancelButton=cancel;
    if(form.ShowDialog()!=DialogResult.OK) return null;
    string output=Path.GetFullPath(path.Text);
    if(File.Exists(output)) throw new IOException("Choisissez un nouveau nom : aucun fichier existant ne sera remplacé.");
    return output;
   }
  }
 }
}

