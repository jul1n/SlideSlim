using System;
using System.IO;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
namespace PPTimizNext {
 public sealed class Launcher : Form {
  static readonly Color Ink=Color.FromArgb(22,35,56), Muted=Color.FromArgb(92,107,128), Accent=Color.FromArgb(36,88,224);
  readonly Label fileName=new Label(), fileInfo=new Label(), status=new Label();
  readonly Button browse=new Button(), run=new Button();
  readonly RadioButton[] profiles=new RadioButton[3];
  readonly Panel[] cards=new Panel[3];
  readonly ProgressBar progress=new ProgressBar();
  readonly ToolTip tips=new ToolTip { AutoPopDelay=15000,InitialDelay=350,ReshowDelay=100 };
  string inputPath=""; bool busy;
  static Label TextAt(string text,int x,int y,int w,int h,float size,Color color,bool bold) {
   return new Label { Text=text,Left=x,Top=y,Width=w,Height=h,ForeColor=color,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),BackColor=Color.Transparent };
  }
  static void StyleButton(Button b,string text,Rectangle bounds,bool primary) {
   b.Text=text; b.Bounds=bounds; b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderSize=primary?0:1;
   b.FlatAppearance.BorderColor=Color.FromArgb(207,216,230); b.BackColor=primary?Accent:Color.White;
   b.ForeColor=primary?Color.White:Ink; b.Font=new Font("Segoe UI",10,FontStyle.Bold); b.Cursor=Cursors.Hand;
  }
  public Launcher(string input) {
   Text="SlideSlim — compresser une présentation"; ClientSize=new Size(720,480);
   Font=new Font("Segoe UI",10); ForeColor=Ink; BackColor=Color.FromArgb(245,247,251);
   AutoScaleMode=AutoScaleMode.Dpi; StartPosition=FormStartPosition.CenterScreen;
   FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; AllowDrop=true;
   var header=new Panel { Bounds=new Rectangle(0,0,720,86),BackColor=Ink };
   header.Controls.Add(TextAt("◈",24,18,44,48,26,Color.FromArgb(139,181,255),true));
   header.Controls.Add(TextAt("SlideSlim",75,20,400,45,23,Color.White,true));
   var local=TextAt("●  Local",594,34,105,24,10,Color.FromArgb(166,212,196),false);
   tips.SetToolTip(local,"Traitement sur cet ordinateur. Aucun document envoyé vers un serveur."); header.Controls.Add(local); Controls.Add(header);
   var drop=new Panel { Bounds=new Rectangle(24,110,672,102),BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,AllowDrop=true };
   var icon=TextAt("▧",17,20,50,56,29,Accent,false); drop.Controls.Add(icon);
   fileName.SetBounds(78,25,405,28); fileName.Text="Déposez une présentation"; fileName.Font=new Font("Segoe UI",12,FontStyle.Bold); fileName.AutoEllipsis=true;
   fileInfo.SetBounds(78,58,405,24); fileInfo.ForeColor=Muted; fileInfo.Text=".pptx · .pptm";
   StyleButton(browse,"＋",new Rectangle(552,24,94,50),false); browse.Font=new Font("Segoe UI",21); browse.AccessibleName="Choisir une présentation";
   tips.SetToolTip(browse,"Choisir une présentation PowerPoint. Enregistrez vos modifications dans PowerPoint avant de la sélectionner.");
   tips.SetToolTip(fileName,"Déposez un fichier .pptx ou .pptm ici, ou utilisez le bouton +.");
   drop.Controls.AddRange(new Control[]{fileName,fileInfo,browse}); Controls.Add(drop);
   string[] titles={"Standard","Full HD","Maximum"}; string[] specs={"150 ppp","1080p","720p"};
   string[] descriptions={"Qualité JPEG élevée (90). Les PNG sont conservés. Résolution adaptée à la taille affichée, à 150 ppp.","Équilibre conseillé : résolution calculée selon la place des images dans une diapositive projetée en 1920 × 1080. JPEG qualité 82, images identiques mutualisées.","Compression renforcée pour une projection en 1280 × 720. JPEG qualité 72. La qualité des images est davantage réduite."};

   for(int i=0;i<3;i++) {
    int index=i; var card=new Panel { Bounds=new Rectangle(24+i*230,238,212,112),BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Cursor=Cursors.Hand };
    var radio=new RadioButton { Text=titles[i],Bounds=new Rectangle(15,14,183,28),Font=new Font("Segoe UI",11,FontStyle.Bold),Cursor=Cursors.Hand,AccessibleName=titles[i] };
    profiles[i]=radio; cards[i]=card; card.Controls.Add(radio);
    var spec=TextAt(specs[i],18,62,128,26,12,Accent,true); card.Controls.Add(spec);

    tips.SetToolTip(card,descriptions[i]); tips.SetToolTip(radio,descriptions[i]); tips.SetToolTip(spec,descriptions[i]);
    radio.AccessibleDescription=descriptions[i];
    EventHandler choose=delegate { if(!busy) SetProfile(index); }; card.Click+=choose; spec.Click+=choose;
    radio.Click+=choose; radio.CheckedChanged+=delegate { if(radio.Checked) SetProfile(index); }; Controls.Add(card);
   }
   SetProfile(1);
   var safe=TextAt("✓  Original conservé",26,370,580,25,9,Muted,false);
   tips.SetToolTip(safe,"Le résultat est enregistré dans une nouvelle copie. Aucun fichier existant n’est remplacé. Les textes et formes restent modifiables."); Controls.Add(safe);
   var footer=new Panel { Bounds=new Rectangle(0,412,720,68),BackColor=Color.White };
   status.SetBounds(24,23,320,35); status.ForeColor=Muted; status.Font=new Font("Segoe UI",9); status.Text="Prêt";
   StyleButton(run,"Compresser une copie  →",new Rectangle(371,12,325,44),true);
   tips.SetToolTip(run,"Choisir où enregistrer la copie, puis lancer la compression. Une synthèse sera affichée à la fin.");
   progress.SetBounds(24,8,315,4); progress.Visible=false; progress.Style=ProgressBarStyle.Marquee;
   footer.Controls.AddRange(new Control[]{status,run,progress}); Controls.Add(footer);
   browse.Click+=delegate { using(var d=new OpenFileDialog { Filter="Présentation PowerPoint|*.pptx;*.pptm",Title="Choisir la présentation" }) if(d.ShowDialog(this)==DialogResult.OK) SelectFile(d.FileName); };
   DragEventHandler enter=delegate(object sender,DragEventArgs e) { if(!busy && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.Copy; };
   DragEventHandler dropped=delegate(object sender,DragEventArgs e) { var files=e.Data.GetData(DataFormats.FileDrop) as string[]; if(files!=null && files.Length==1) SelectFile(files[0]); else MessageBox.Show(this,"Déposez une seule présentation à la fois."); };
   DragEnter+=enter; DragDrop+=dropped; drop.DragEnter+=enter; drop.DragDrop+=dropped;
   run.Click+=delegate { Optimize(); };
   FormClosing+=delegate(object sender,FormClosingEventArgs e) { if(busy) { e.Cancel=true; status.Text="Compression en cours. Fermeture possible à la fin."; } };
   if(!String.IsNullOrEmpty(input)) SelectFile(input);
  }
  void SetProfile(int selected) {
   for(int i=0;i<3;i++) { if(profiles[i]==null) continue; profiles[i].Checked=i==selected; cards[i].BackColor=i==selected?Color.FromArgb(232,240,255):Color.White; }
  }
  void SelectFile(string file) {
   string ext=Path.GetExtension(file).ToLowerInvariant();
   if(!File.Exists(file) || (ext!=".pptx" && ext!=".pptm")) { MessageBox.Show(this,"Choisissez un fichier .pptx ou .pptm existant."); return; }
   inputPath=Path.GetFullPath(file); fileName.Text=Path.GetFileName(file); tips.SetToolTip(fileName,Path.GetFileName(file));
   fileInfo.Text=String.Format("{0:N2} Mo · {1} · prêt à optimiser",new FileInfo(file).Length/1000000.0,ext.TrimStart('.').ToUpperInvariant());
   status.Text="Prêt à compresser";
  }
  void Optimize() {
   if(busy) return;
   if(!File.Exists(inputPath)) { MessageBox.Show(this,"Choisissez d’abord une présentation."); return; }
   string input=inputPath; string output;
   using(var d=new SaveFileDialog { Filter="Présentation PowerPoint|*"+Path.GetExtension(input),DefaultExt=Path.GetExtension(input).TrimStart('.'),FileName=Path.GetFileName(Engine.UniqueOutput(input)),InitialDirectory=Path.GetDirectoryName(input),OverwritePrompt=true }) {
    if(d.ShowDialog(this)!=DialogResult.OK) return; output=d.FileName;
   }
   if(File.Exists(output)) { MessageBox.Show(this,"Choisissez un nouveau nom : aucun fichier existant ne sera remplacé."); return; }
   var selected=profiles[0].Checked?CompressionProfile.Standard:profiles[2].Checked?CompressionProfile.MaximumHD:CompressionProfile.FullHD;
   busy=true; browse.Enabled=run.Enabled=false; foreach(var card in cards) card.Enabled=false; progress.Visible=true; status.Text="Compression en cours…";
   var worker=new BackgroundWorker();
   worker.DoWork+=delegate(object sender,DoWorkEventArgs e) { e.Result=Engine.Optimize(input,output,selected).Summary(); };
   worker.RunWorkerCompleted+=delegate(object sender,RunWorkerCompletedEventArgs e) {
    busy=false; browse.Enabled=run.Enabled=true; foreach(var card in cards) card.Enabled=true; progress.Visible=false;
    status.Text=e.Error==null?"Terminé ! Votre original est conservé.":"Traitement interrompu. Votre original est conservé.";
    if(e.Error!=null) UI.Error(e.Error); else MessageBox.Show(this,(string)e.Result,"SlideSlim",MessageBoxButtons.OK,MessageBoxIcon.Information);
    worker.Dispose();
   }; worker.RunWorkerAsync();
  }
 }
}
