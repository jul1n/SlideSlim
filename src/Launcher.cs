using System;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
namespace PPTimizNext {
 public sealed class Launcher : Form {
  readonly TextBox path = new TextBox { Left=24,Top=100,Width=470,ReadOnly=true };
  readonly Button browse = new Button { Left=505,Top=98,Width=105,Text="Parcourir…" };
  readonly ComboBox profile = new ComboBox { Left=24,Top=170,Width=586,DropDownStyle=ComboBoxStyle.DropDownList };
  readonly Button run = new Button { Left=400,Top=278,Width=210,Height=38,Text="Créer une copie optimisée" };
  readonly Label status = new Label { Left=24,Top=330,Width=586,Height=55 };
  readonly ProgressBar progress = new ProgressBar { Left=24,Top=310,Width=350,Visible=false,Style=ProgressBarStyle.Marquee };
  bool busy;
  public Launcher(string input) {
   Text="PPTimiz Next — application indépendante"; ClientSize=new Size(635,390);
   Font=new Font("Segoe UI",10); StartPosition=FormStartPosition.CenterScreen;
   FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; AllowDrop=true;
   Controls.Add(new Label { Left=24,Top=20,Width=590,Height=65,Text="Compresser une présentation PowerPoint\nChoisissez un fichier ou déposez-le dans cette fenêtre." });
   Controls.Add(new Label { Left=24,Top=142,Width=500,Text="Qualité de la copie" });
   profile.Items.AddRange(new object[]{"Standard — 150 ppp","Intermédiaire Full HD — taille affichée en 1080p","Maximum HD — 1 280 × 720, compression renforcée"}); profile.SelectedIndex=1;
   Controls.Add(new Label { Left=24,Top=212,Width=586,Height=60,Text="Maximum HD réduit davantage la qualité des images.\nEnregistrez vos modifications dans PowerPoint avant de choisir le fichier." });
   Controls.AddRange(new Control[]{path,browse,profile,run,status,progress});
   browse.Click+=delegate { using(var d=new OpenFileDialog { Filter="Présentation PowerPoint|*.pptx;*.pptm",Title="Choisir la présentation" }) if(d.ShowDialog(this)==DialogResult.OK) SelectFile(d.FileName); };
   DragEnter+=delegate(object sender,DragEventArgs e) { if(!busy && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.Copy; };
   DragDrop+=delegate(object sender,DragEventArgs e) { var files=e.Data.GetData(DataFormats.FileDrop) as string[]; if(files!=null && files.Length==1) SelectFile(files[0]); else MessageBox.Show(this,"Déposez une seule présentation à la fois."); };
   run.Click+=delegate { Optimize(); };
   FormClosing+=delegate(object sender,FormClosingEventArgs e) { if(busy) { e.Cancel=true; status.Text="Traitement en cours. La fenêtre pourra être fermée à la fin."; } };
   if(!String.IsNullOrEmpty(input)) SelectFile(input);
  }
  void SelectFile(string file) {
   string ext=Path.GetExtension(file).ToLowerInvariant();
   if(!File.Exists(file) || (ext!=".pptx" && ext!=".pptm")) { MessageBox.Show(this,"Choisissez un fichier .pptx ou .pptm existant."); return; }
   path.Text=Path.GetFullPath(file); status.Text="L’original sera conservé. Le résultat sera enregistré dans une nouvelle copie.";
  }
  void Optimize() {
   if(busy) return;
   if(!File.Exists(path.Text)) { MessageBox.Show(this,"Choisissez d’abord une présentation."); return; }
   string input=path.Text; string output;
   using(var d=new SaveFileDialog { Filter="Présentation PowerPoint|*"+Path.GetExtension(input),DefaultExt=Path.GetExtension(input).TrimStart('.'),FileName=Path.GetFileName(Engine.UniqueOutput(input)),InitialDirectory=Path.GetDirectoryName(input),OverwritePrompt=true }) {
    if(d.ShowDialog(this)!=DialogResult.OK) return; output=d.FileName;
   }
   if(File.Exists(output)) { MessageBox.Show(this,"Choisissez un nouveau nom : aucun fichier existant ne sera remplacé."); return; }
   var selected=profile.SelectedIndex==0?CompressionProfile.Standard:profile.SelectedIndex==2?CompressionProfile.MaximumHD:CompressionProfile.FullHD;
   busy=true; browse.Enabled=run.Enabled=profile.Enabled=false; progress.Visible=true; status.Text="Compression en cours…";
   var worker=new BackgroundWorker();
   worker.DoWork+=delegate(object sender,DoWorkEventArgs e) { e.Result=Engine.Optimize(input,output,selected).Summary(); };
   worker.RunWorkerCompleted+=delegate(object sender,RunWorkerCompletedEventArgs e) {
    busy=false; browse.Enabled=run.Enabled=profile.Enabled=true; progress.Visible=false;
    status.Text=e.Error==null?"Traitement terminé. L’original est conservé.":"Traitement interrompu. L’original est conservé.";
    if(e.Error!=null) UI.Error(e.Error); else MessageBox.Show(this,(string)e.Result,"PPTimiz Next",MessageBoxButtons.OK,MessageBoxIcon.Information);
    worker.Dispose();
   }; worker.RunWorkerAsync();
  }
 }
}

