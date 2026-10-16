using System;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Xml;
using PPTimizNext;
static class SmartTests {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
 static void Add(ZipArchive z,string name,byte[] bytes){using(var s=z.CreateEntry(name).Open())s.Write(bytes,0,bytes.Length);}
 static byte[] Text(string text){return Encoding.UTF8.GetBytes(text);}
 public static int Run(string root){
  string input=Path.Combine(root,"smart.pptx"),output=Path.Combine(root,"smart-result.pptx");byte[] png;
  using(var b=new Bitmap(2000,1000))using(var g=Graphics.FromImage(b)){g.Clear(Color.White);var random=new Random(15);for(int i=0;i<12000;i++)using(var brush=new SolidBrush(Color.FromArgb(random.Next(256),random.Next(256),random.Next(256))))g.FillRectangle(brush,random.Next(2000),random.Next(1000),20,20);using(var m=new MemoryStream()){b.Save(m,ImageFormat.Png);png=m.ToArray();}}
  Check(png.Length>65536,"opaque PNG fixture exercises JPEG conversion");
  string p="http://schemas.openxmlformats.org/presentationml/2006/main",a="http://schemas.openxmlformats.org/drawingml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships",rels="http://schemas.openxmlformats.org/package/2006/relationships";
  Func<string,string> picture=id=>"<p:pic><p:blipFill><a:blip r:embed='"+id+"'/><a:srcRect l='25000' r='25000'/></p:blipFill><p:spPr><a:xfrm><a:ext cx='1828800' cy='914400'/></a:xfrm></p:spPr></p:pic>";
  string slide="<p:sld xmlns:p='"+p+"' xmlns:a='"+a+"' xmlns:r='"+r+"'><p:cSld><p:spTree>"+picture("rId1")+picture("rId2")+"</p:spTree></p:cSld></p:sld>";
  using(var z=ZipFile.Open(input,ZipArchiveMode.Create)){
   Add(z,"[Content_Types].xml",Text("<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='png' ContentType='image/png'/><Override PartName='/ppt/media/a.png' ContentType='image/png'/><Override PartName='/ppt/media/b.png' ContentType='image/png'/></Types>"));
   Add(z,"ppt/presentation.xml",Text("<p:presentation xmlns:p='"+p+"'><p:sldSz cx='12192000' cy='6858000'/></p:presentation>"));
   Add(z,"ppt/slides/slide1.xml",Text(slide));Add(z,"ppt/media/a.png",png);Add(z,"ppt/media/b.png",png);
   Add(z,"ppt/slides/_rels/slide1.xml.rels",Text("<Relationships xmlns='"+rels+"'><Relationship Id='rId1' Type='"+r+"/image' Target='../media/a.png'/><Relationship Id='rId2' Type='"+r+"/image' Target='../media/b.png'/></Relationships>"));
  }
  var result=Engine.Optimize(input,output,CompressionProfile.MaximumHD);
  Check(result.Duplicates==1,"identical images are stored only once");
  using(var z=ZipFile.OpenRead(output)){
   ImageGeometry.ValidateRelationships(z);
   var images=z.Entries.Where(e=>e.FullName.StartsWith("ppt/media/")).ToArray();Check(images.Length==1&&images[0].FullName.EndsWith(".jpg"),"opaque PNG converted into a correctly named JPEG part");
   using(var s=images[0].Open())using(var image=Image.FromStream(s))Check(image.Width==384&&image.Height==192,"display size and crop determine required pixels");
   var doc=ImageGeometry.Read(z.GetEntry("ppt/slides/_rels/slide1.xml.rels"));var targets=doc.DocumentElement.ChildNodes.OfType<XmlElement>().Select(n=>n.GetAttribute("Target")).ToArray();Check(targets[0]==targets[1],"both relationships reference the deduplicated image");
   var types=ImageGeometry.Read(z.GetEntry("[Content_Types].xml"));Check(types.DocumentElement.ChildNodes.OfType<XmlElement>().Count(n=>n.LocalName=="Override")==1,"duplicate content type overrides removed");
   Check(types.DocumentElement.ChildNodes.OfType<XmlElement>().Any(n=>n.GetAttribute("ContentType")=="image/jpeg"),"JPEG MIME type declared");
   using(var reader=new StreamReader(z.GetEntry("ppt/slides/slide1.xml").Open()))Check(reader.ReadToEnd()==slide,"crop and slide structure remain unchanged");
  }
  string transparent=Path.Combine(root,"transparent-max.pptm");var trans=Engine.Optimize(Path.Combine(root,"fixture.pptm"),transparent,CompressionProfile.MaximumHD);
  using(var z=ZipFile.OpenRead(transparent))using(var s=z.GetEntry("ppt/media/image1.png").Open())using(var image=new Bitmap(s))Check(image.GetPixel(image.Width-2,10).A==0,"maximum mode preserves real transparency");
  string full=Path.Combine(root,"smart-fullhd.pptx");
  var fullResult=Engine.Optimize(input,full,CompressionProfile.FullHD);
  Check(fullResult.Duplicates==1,"Full HD deduplicates images");
  using(var z=ZipFile.OpenRead(full)) {
   ImageGeometry.ValidateRelationships(z);
   var images=z.Entries.Where(e=>e.FullName.StartsWith("ppt/media/")).ToArray();
   Check(images.Length==1 && images[0].FullName.EndsWith(".jpg"),"Full HD converts opaque PNG to JPEG");
   using(var stream=images[0].Open())using(var image=Image.FromStream(stream)) Check(image.Width==576 && image.Height==288,"Full HD uses slide size and crop at 1080p rather than 1920 pixels per image");
  }
  string alphaFull=Path.Combine(root,"transparent-full.pptm"); Engine.Optimize(Path.Combine(root,"fixture.pptm"),alphaFull,CompressionProfile.FullHD);
  using(var z=ZipFile.OpenRead(alphaFull))using(var stream=z.GetEntry("ppt/media/image1.png").Open())using(var image=new Bitmap(stream))Check(image.GetPixel(image.Width-2,10).A==0,"Full HD preserves actual transparency");
  return count;
 }
}

