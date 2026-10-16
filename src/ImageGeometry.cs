using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
namespace PPTimizNext {
 public sealed class ImageBounds { public double Width,Height; }
 public static class ImageGeometry {
  public static XmlDocument Read(ZipArchiveEntry entry) { var doc=new XmlDocument { XmlResolver=null,PreserveWhitespace=true }; using(var s=entry.Open()) using(var r=XmlReader.Create(s,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null })) doc.Load(r); return doc; }
  public static string Owner(string rels) { if(rels=="_rels/.rels")return "";int i=rels.LastIndexOf("/_rels/",StringComparison.Ordinal);return i<0?"":rels.Substring(0,i+1)+rels.Substring(i+7,rels.Length-i-12); }
  public static string Resolve(string owner,string target) { return Uri.UnescapeDataString(new Uri(new Uri("http://package/"+owner),target).AbsolutePath.TrimStart('/')); }
  public static byte[] Bytes(XmlDocument doc) { using(var m=new MemoryStream()) { using(var w=XmlWriter.Create(m,new XmlWriterSettings { Encoding=new System.Text.UTF8Encoding(false),CloseOutput=false }))doc.Save(w);return m.ToArray(); } }
  static double Number(XmlElement node,string name,double fallback) { double n;return node!=null && double.TryParse(node.GetAttribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out n)?n:fallback; }
  static double Percent(XmlElement node,string name) { if(node==null)return 0;string text=node.GetAttribute(name);double n;if(text.EndsWith("%"))return double.TryParse(text.TrimEnd('%'),NumberStyles.Float,CultureInfo.InvariantCulture,out n)?n/100:0;return double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out n)?n/100000:0; }
  public static Dictionary<string,ImageBounds> Analyze(ZipArchive zip,CompressionProfile profile) {
   var result=new Dictionary<string,ImageBounds>(StringComparer.Ordinal);
   var presentation=Read(zip.GetEntry("ppt/presentation.xml"));var size=presentation.SelectSingleNode("//*[local-name()='sldSz']") as XmlElement;
   double sw=Number(size,"cx",12192000),sh=Number(size,"cy",6858000);
   double scale=profile==CompressionProfile.Standard?150.0/914400:Math.Min((profile==CompressionProfile.MaximumHD?1280.0:1920)/sw,(profile==CompressionProfile.MaximumHD?720.0:1080)/sh);
   foreach(var relEntry in zip.Entries.Where(e=>e.FullName.EndsWith(".rels",StringComparison.Ordinal))) {
    string owner=Owner(relEntry.FullName);if(!owner.StartsWith("ppt/",StringComparison.Ordinal))continue;
    var part=zip.GetEntry(owner);if(part==null||!owner.EndsWith(".xml",StringComparison.Ordinal))continue;
    var relations=Read(relEntry);var images=new Dictionary<string,string>();
    foreach(XmlElement rel in relations.DocumentElement.ChildNodes.OfType<XmlElement>())if(rel.GetAttribute("Type").EndsWith("/image",StringComparison.Ordinal)&&rel.GetAttribute("TargetMode")!="External")images[rel.GetAttribute("Id")]=Resolve(owner,rel.GetAttribute("Target"));
    if(images.Count==0)continue;var doc=Read(part);
    foreach(XmlElement blip in doc.SelectNodes("//*[local-name()='blip']")) {
     var attr=blip.Attributes.Cast<XmlAttribute>().FirstOrDefault(a=>a.LocalName=="embed");string image;
     if(attr==null||!images.TryGetValue(attr.Value,out image))continue;
     XmlElement shape=null;for(XmlNode n=blip.ParentNode;n!=null;n=n.ParentNode)if(n.LocalName=="pic"||n.LocalName=="sp"){shape=n as XmlElement;break;}
     var transform=shape==null?null:shape.SelectSingleNode("./*[local-name()='spPr']/*[local-name()='xfrm']") as XmlElement;
     var extent=transform==null?null:transform.SelectSingleNode("./*[local-name()='ext']") as XmlElement;
     double width=Number(extent,"cx",sw),height=Number(extent,"cy",sh);
     if(shape!=null)for(XmlNode n=shape.ParentNode;n!=null;n=n.ParentNode)if(n.LocalName=="grpSp") {
      var g=n.SelectSingleNode("./*[local-name()='grpSpPr']/*[local-name()='xfrm']");if(g==null)continue;
      var ge=g.SelectSingleNode("./*[local-name()='ext']") as XmlElement;var ce=g.SelectSingleNode("./*[local-name()='chExt']") as XmlElement;
      width*=Number(ge,"cx",1)/Math.Max(1,Number(ce,"cx",1));height*=Number(ge,"cy",1)/Math.Max(1,Number(ce,"cy",1));
     }
     var crop=blip.ParentNode.SelectSingleNode("./*[local-name()='srcRect']") as XmlElement;
     width=width*scale/Math.Max(0.001,1-Percent(crop,"l")-Percent(crop,"r"));height=height*scale/Math.Max(0.001,1-Percent(crop,"t")-Percent(crop,"b"));
     ImageBounds bounds;if(!result.TryGetValue(image,out bounds)){bounds=new ImageBounds();result[image]=bounds;}
     bounds.Width=Math.Max(bounds.Width,width);bounds.Height=Math.Max(bounds.Height,height);
    }
   }
   return result;
  }
  public static byte[] RewriteRelationships(ZipArchiveEntry entry,Dictionary<string,string> renames) {
   var doc=Read(entry);bool changed=false;string owner=Owner(entry.FullName);
   foreach(XmlElement rel in doc.DocumentElement.ChildNodes.OfType<XmlElement>()) {
    if(rel.GetAttribute("TargetMode")=="External")continue;string next;
    if(renames.TryGetValue(Resolve(owner,rel.GetAttribute("Target")),out next)) {
     var source=new Uri("http://package/"+owner);var destination=new Uri("http://package/"+next);
     rel.SetAttribute("Target",source.MakeRelativeUri(destination).ToString());changed=true;
    }
   }
   return changed?Bytes(doc):null;
  }
  public static byte[] RewriteContentTypes(ZipArchiveEntry entry,Dictionary<string,string> renames) {
   var doc=Read(entry);bool hasJpeg=false;
   foreach(XmlElement item in doc.DocumentElement.ChildNodes.OfType<XmlElement>()) {
    if(item.LocalName=="Default"&&item.GetAttribute("Extension")=="jpg") { item.SetAttribute("ContentType","image/jpeg");hasJpeg=true; }
    string next;if(item.LocalName=="Override"&&renames.TryGetValue(Uri.UnescapeDataString(item.GetAttribute("PartName").TrimStart('/')),out next)){item.SetAttribute("PartName","/"+next);item.SetAttribute("ContentType",next.EndsWith(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"image/jpeg");}
   }
   if(!hasJpeg){var item=doc.CreateElement("Default",doc.DocumentElement.NamespaceURI);item.SetAttribute("Extension","jpg");item.SetAttribute("ContentType","image/jpeg");doc.DocumentElement.AppendChild(item);}
   var seen=new HashSet<string>(StringComparer.Ordinal);foreach(var item in doc.DocumentElement.ChildNodes.OfType<XmlElement>().Where(n=>n.LocalName=="Override").ToArray())if(!seen.Add(item.GetAttribute("PartName")))item.ParentNode.RemoveChild(item);
   return Bytes(doc);
  }
  public static void ValidateRelationships(ZipArchive zip) {
   foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".rels",StringComparison.Ordinal))) {
    var doc=Read(entry);string owner=Owner(entry.FullName);
    foreach(XmlElement rel in doc.DocumentElement.ChildNodes.OfType<XmlElement>())if(rel.GetAttribute("TargetMode")!="External") {
     string target=Resolve(owner,rel.GetAttribute("Target"));if(zip.GetEntry(target)==null)throw new InvalidDataException("Relation interne manquante : "+entry.FullName+" -> "+target);
    }
   }
  }
 }
}
