using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using PPTimizNext;
class Tests {
 static int count;
 static void Assert(bool condition,string name) { if(!condition) throw new Exception("FAIL: "+name); Console.WriteLine("PASS: "+name); count++; }
 static byte[] Read(string file,string entry) { using(var zip=ZipFile.OpenRead(file)) using(var stream=zip.GetEntry(entry).Open()) using(var mem=new MemoryStream()) {stream.CopyTo(mem);return mem.ToArray();} }
 static void Add(ZipArchive zip,string name,byte[] data) { var e=zip.CreateEntry(name); using(var s=e.Open()) s.Write(data,0,data.Length); }
 static byte[] Text(string value) { return System.Text.Encoding.UTF8.GetBytes(value); }
 static void Main(string[] args) {
  string root=args[0]; Directory.CreateDirectory(root); string input=Path.Combine(root,"fixture.pptm");
  var random=new Random(12); byte[] noise=new byte[512*1024]; random.NextBytes(noise);
  byte[] png,jpg; using(var image=new Bitmap(3200,1200,PixelFormat.Format32bppArgb)) {
   using(var g=Graphics.FromImage(image)) {g.Clear(Color.Transparent); g.FillRectangle(Brushes.Blue,0,0,1600,1200);}
   using(var s=new MemoryStream()) {image.Save(s,ImageFormat.Png); png=s.ToArray();}
   using(var g=Graphics.FromImage(image)) for(int i=0;i<12000;i++) using(var b=new SolidBrush(Color.FromArgb(random.Next(256),random.Next(256),random.Next(256)))) g.FillRectangle(b,random.Next(3200),random.Next(1200),25,25);
   using(var s=new MemoryStream()) { var codec=ImageCodecInfo.GetImageEncoders().First(c=>c.FormatID==ImageFormat.Jpeg.Guid); using(var p=new EncoderParameters(1)) {p.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,100L); image.Save(s,codec,p);} jpg=s.ToArray(); }
  }
  using(var zip=ZipFile.Open(input,ZipArchiveMode.Create)) {
   Add(zip,"[Content_Types].xml",Text("<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'/>"));
   Add(zip,"ppt/presentation.xml",Text("<p:presentation xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main'/>"));
   Add(zip,"ppt/media/image1.png",png); Add(zip,"ppt/media/image2.jpg",jpg);
   Add(zip,"ppt/media/image3.svg",Text("<svg xmlns='http://www.w3.org/2000/svg'/>"));
   Add(zip,"ppt/vbaProject.bin",noise); Add(zip,"ppt/media/movie.mp4",noise);
   Add(zip,"ppt/slides/slide1.xml",Text("<slide><text>Contenu intact</text></slide>"));
  }
  byte[] before=File.ReadAllBytes(input); string output=Path.Combine(root,"result.pptm"); var result=Engine.Optimize(input,output,false);
  Assert(before.SequenceEqual(File.ReadAllBytes(input)),"original byte-for-byte unchanged");
  string standardOutput=Path.Combine(root,"standard.pptm"); Engine.Optimize(input,standardOutput,true);
  using(var s=new MemoryStream(Read(standardOutput,"ppt/media/image1.png"))) using(var image=new Bitmap(s)) Assert(image.Width==2560,"standard dimension limit");
  Assert(result.Output==output && result.After<result.Before,"smaller copy produced");
  Assert(Read(input,"ppt/vbaProject.bin").SequenceEqual(Read(output,"ppt/vbaProject.bin")),"VBA retained");
  Assert(Read(input,"ppt/media/movie.mp4").SequenceEqual(Read(output,"ppt/media/movie.mp4")),"video retained");
  Assert(Read(input,"ppt/media/image3.svg").SequenceEqual(Read(output,"ppt/media/image3.svg")),"vector retained");
  Assert(Read(input,"ppt/slides/slide1.xml").SequenceEqual(Read(output,"ppt/slides/slide1.xml")),"slide XML retained");
  using(var s=new MemoryStream(Read(output,"ppt/media/image1.png"))) using(var image=new Bitmap(s)) { Assert(image.Width==1920,"compact dimension limit"); Assert(image.GetPixel(1900,100).A==0,"PNG transparency retained"); }
  bool rejected=false; try { Engine.Optimize(input,input,false); } catch(IOException) { rejected=true; } Assert(rejected,"same path rejected");
  byte[] existing=File.ReadAllBytes(output); rejected=false; try {Engine.Optimize(input,output,false);} catch(IOException) {rejected=true;} Assert(rejected && existing.SequenceEqual(File.ReadAllBytes(output)),"existing destination never overwritten");
  rejected=false; try {Engine.Optimize(input,Path.Combine(root,"wrong.pptx"),false);} catch(IOException) {rejected=true;} Assert(rejected,"macro extension retained");
  string signed=Path.Combine(root,"signed.pptm"); File.Copy(input,signed); using(var zip=ZipFile.Open(signed,ZipArchiveMode.Update)) Add(zip,"_xmlsignatures/sig1.xml",Text("<signature/>"));
  rejected=false; try {Engine.Optimize(signed,Path.Combine(root,"signed-output.pptm"),true);} catch(NotSupportedException) {rejected=true;} Assert(rejected,"signed package rejected");
  string broken=Path.Combine(root,"broken.pptx"); using(var zip=ZipFile.Open(broken,ZipArchiveMode.Create)) {Add(zip,"[Content_Types].xml",Text("<invalid>"));Add(zip,"ppt/presentation.xml",Text("<p/>"));}
  rejected=false; try {Engine.Optimize(broken,Path.Combine(root,"broken-output.pptx"),true);} catch(System.Xml.XmlException) {rejected=true;} Assert(rejected && !File.Exists(Path.Combine(root,"broken-output.pptx")),"malformed XML never published");
  Assert(Directory.GetFiles(root,".pptimiz-*").Length==0,"temporary files cleaned");
  string minimal=Path.Combine(root,"no-gain.pptx"); using(var zip=ZipFile.Open(minimal,ZipArchiveMode.Create)) {Add(zip,"[Content_Types].xml",Text("<Types/>"));Add(zip,"ppt/presentation.xml",Text("<presentation/>"));}
  result=Engine.Optimize(minimal,Path.Combine(root,"no-gain-output.pptx"),true);
  Assert(result.Output==null && File.Exists(minimal) && !File.Exists(Path.Combine(root,"no-gain-output.pptx")),"no gain preserves original without output");
  string layouts=Path.Combine(root,"layouts.pptx");
  string relNs="http://schemas.openxmlformats.org/package/2006/relationships", office="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
  using(var zip=ZipFile.Open(layouts,ZipArchiveMode.Create)) {
   Add(zip,"[Content_Types].xml",Text("<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Override PartName='/ppt/slideLayouts/slideLayout2.xml' ContentType='layout'/></Types>"));
   Add(zip,"ppt/slideMasters/slideMaster1.xml",Text("<p:sldMaster xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main' xmlns:r='"+office+"'><p:sldLayoutIdLst><p:sldLayoutId id='1' r:id='rId1'/><p:sldLayoutId id='2' r:id='rId2'/></p:sldLayoutIdLst></p:sldMaster>"));
   Add(zip,"ppt/slideMasters/_rels/slideMaster1.xml.rels",Text("<Relationships xmlns='"+relNs+"'><Relationship Id='rId1' Type='"+office+"/slideLayout' Target='../slideLayouts/slideLayout1.xml'/><Relationship Id='rId2' Type='"+office+"/slideLayout' Target='../slideLayouts/slideLayout2.xml'/></Relationships>"));
   Add(zip,"ppt/slides/_rels/slide1.xml.rels",Text("<Relationships xmlns='"+relNs+"'><Relationship Id='rId1' Type='"+office+"/slideLayout' Target='../slideLayouts/slideLayout1.xml'/></Relationships>"));
   Add(zip,"ppt/slideLayouts/slideLayout1.xml",Text("<layout/>")); Add(zip,"ppt/slideLayouts/slideLayout2.xml",Text("<layout/>"));
   Add(zip,"ppt/slideLayouts/_rels/slideLayout2.xml.rels",Text("<Relationships xmlns='"+relNs+"'/>"));
  }
  Assert(LayoutCleaner.Clean(layouts)==1,"exactly one unused layout removed");
  using(var zip=ZipFile.OpenRead(layouts)) { Assert(zip.GetEntry("ppt/slideLayouts/slideLayout1.xml")!=null,"used layout retained"); Assert(zip.GetEntry("ppt/slideLayouts/slideLayout2.xml")==null && zip.GetEntry("ppt/slideLayouts/_rels/slideLayout2.xml.rels")==null,"unused layout and relationships removed"); }
  Assert(!System.Text.Encoding.UTF8.GetString(Read(layouts,"ppt/slideMasters/slideMaster1.xml")).Contains("rId2"),"master layout reference cleaned");
  Assert(!System.Text.Encoding.UTF8.GetString(Read(layouts,"[Content_Types].xml")).Contains("slideLayout2"),"content type declaration cleaned");
  Assert(LayoutCleaner.Clean(layouts)==0,"layout cleaning is idempotent");
  count+=SmartTests.Run(root);
  Console.WriteLine(count+" tests passed.");
 }
}
