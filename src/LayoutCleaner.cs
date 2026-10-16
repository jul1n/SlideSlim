using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
namespace PPTimizNext {
 public static class LayoutCleaner {
  const string RelNs="http://schemas.openxmlformats.org/package/2006/relationships";
  const string OfficeRel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
  static XmlDocument Load(ZipArchiveEntry entry) {
   var doc=new XmlDocument { XmlResolver=null, PreserveWhitespace=true };
   using(var stream=entry.Open()) using(var reader=XmlReader.Create(stream,new XmlReaderSettings { XmlResolver=null,DtdProcessing=DtdProcessing.Prohibit })) doc.Load(reader);
   return doc;
  }
  static string Target(string owner,string target) {
   var uri=new Uri(new Uri("http://package/"+owner),target);
   if(uri.Host!="package") throw new InvalidDataException("Relation externe inattendue.");
   return Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
  }
  static string Owner(string rels) { int index=rels.LastIndexOf("/_rels/",StringComparison.Ordinal); return index<0?"":rels.Substring(0,index+1)+rels.Substring(index+7,rels.Length-index-12); }
  static void Save(ZipArchive zip,string name,XmlDocument doc) {
   zip.GetEntry(name).Delete(); var entry=zip.CreateEntry(name,CompressionLevel.Optimal);
   using(var stream=entry.Open()) using(var writer=XmlWriter.Create(stream,new XmlWriterSettings { Encoding=new System.Text.UTF8Encoding(false),Indent=false })) doc.Save(writer);
  }
  // Only remove a layout if its only inbound relationships come from slide masters.
  // All media, master designs and themes are retained; no transitive resource deletion.
  public static int Clean(string file) {
   using(var zip=ZipFile.Open(file,ZipArchiveMode.Update)) {
    var referenced=new HashSet<string>(StringComparer.Ordinal);
    var masterRels=new Dictionary<string,XmlDocument>();
    foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".rels",StringComparison.Ordinal)).ToArray()) {
     string owner=Owner(entry.FullName); var doc=Load(entry);
     bool master=owner.StartsWith("ppt/slideMasters/",StringComparison.Ordinal) && owner.EndsWith(".xml",StringComparison.Ordinal);
     if(master) masterRels.Add(entry.FullName,doc);
     foreach(XmlElement rel in doc.DocumentElement.ChildNodes.OfType<XmlElement>()) {
      if(rel.GetAttribute("TargetMode")=="External") continue;
      if(!master || !rel.GetAttribute("Type").EndsWith("/slideLayout",StringComparison.Ordinal)) referenced.Add(Target(owner,rel.GetAttribute("Target")));
     }
    }
    var remove=new HashSet<string>(StringComparer.Ordinal);
    var changed=new Dictionary<string,XmlDocument>();
    foreach(var pair in masterRels) {
     string owner=Owner(pair.Key); var masterEntry=zip.GetEntry(owner); if(masterEntry==null) continue;
     var master=Load(masterEntry); bool dirty=false;
     var layoutRelations=pair.Value.DocumentElement.ChildNodes.OfType<XmlElement>().Where(r=>r.GetAttribute("Type").EndsWith("/slideLayout",StringComparison.Ordinal)).ToArray();
     int remaining=layoutRelations.Length;
     foreach(var rel in layoutRelations) {
      if(!rel.GetAttribute("Type").EndsWith("/slideLayout",StringComparison.Ordinal) || rel.GetAttribute("TargetMode")=="External") continue;
      string target=Target(owner,rel.GetAttribute("Target"));
      // Leave at least one layout per master to keep each design usable for authoring.
      if(remaining<=1 || referenced.Contains(target) || zip.GetEntry(target)==null) continue;
      string id=rel.GetAttribute("Id");
      foreach(XmlElement node in master.GetElementsByTagName("sldLayoutId","http://schemas.openxmlformats.org/presentationml/2006/main").Cast<XmlElement>().ToArray()) if(node.GetAttribute("id",OfficeRel)==id) node.ParentNode.RemoveChild(node);
      rel.ParentNode.RemoveChild(rel); remove.Add(target); dirty=true; remaining--;
     }
     if(dirty) { changed[pair.Key]=pair.Value; changed[owner]=master; }
    }
    if(remove.Count==0) return 0;
    var content=Load(zip.GetEntry("[Content_Types].xml"));
    foreach(var node in content.DocumentElement.ChildNodes.OfType<XmlElement>().ToArray()) if(remove.Contains(Uri.UnescapeDataString(node.GetAttribute("PartName").TrimStart('/')))) node.ParentNode.RemoveChild(node);
    changed["[Content_Types].xml"]=content;
    foreach(string name in remove) {
     zip.GetEntry(name).Delete(); int slash=name.LastIndexOf('/');
     var relEntry=zip.GetEntry(name.Substring(0,slash+1)+"_rels/"+name.Substring(slash+1)+".rels"); if(relEntry!=null) relEntry.Delete();
    }
    foreach(var pair in changed) Save(zip,pair.Key,pair.Value);
    return remove.Count;
   }
  }
 }
}
