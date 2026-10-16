using System;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace PPTimizNext {
 public enum CompressionProfile { Standard, FullHD, MaximumHD }
 public sealed class Result {
  public long Before, After; public int Images, Layouts, Duplicates; public CompressionProfile Profile; public string Output; public List<string> Warnings = new List<string>();
  public string Summary() {
   string mode=Profile==CompressionProfile.MaximumHD?"Maximum HD — 1 280 × 720":Profile==CompressionProfile.FullHD?"Intermédiaire Full HD — taille affichée en 1080p":"Standard — 150 ppp";
   return String.Format("Traitement terminé\nProfil : {7}\n\nTaille initiale : {0:N2} Mo\nTaille finale : {1:N2} Mo\nGain : {2:N1} %\nImages optimisées : {3}\nImages identiques mutualisées : {8}\nDispositions inutilisées supprimées : {6}\n\n{4}\n\nL’original est conservé.{5}", Before/1048576.0, After/1048576.0, Before == 0 ? 0 : 100.0*(Before-After)/Before, Images, Output == null ? "Aucune copie créée : aucun gain de taille." : Output, Warnings.Count == 0 ? "" : "\n\nRemarques :\n"+String.Join("\n",Warnings), Layouts,mode,Duplicates);
  }
 }
 public static class Engine {
  public static Result Optimize(string input, string output, bool standard) {
   return Optimize(input,output,standard?CompressionProfile.Standard:CompressionProfile.FullHD);
  }
  public static Result Optimize(string input, string output, CompressionProfile profile) {
   input=Path.GetFullPath(input); output=Path.GetFullPath(output);
   if(String.Equals(input,output,StringComparison.OrdinalIgnoreCase)) throw new IOException("La sortie doit être différente de l’original.");
   if(File.Exists(output)) throw new IOException("Le fichier de sortie existe déjà.");
   string ext=Path.GetExtension(input).ToLowerInvariant();
   if(ext!=".pptx" && ext!=".pptm") throw new NotSupportedException("Formats pris en charge : .pptx et .pptm.");
   if(!String.Equals(ext,Path.GetExtension(output),StringComparison.OrdinalIgnoreCase)) throw new IOException("Conservez la même extension.");
   Result result=new Result(); result.Profile=profile;result.Before=new FileInfo(input).Length;
   string temp=Path.Combine(Path.GetDirectoryName(output),".pptimiz-"+Guid.NewGuid().ToString("N")+".tmp");
   try {
    using(var src=ZipFile.OpenRead(input)) {
     if(src.GetEntry("[Content_Types].xml")==null || src.GetEntry("ppt/presentation.xml")==null) throw new InvalidDataException("Présentation Open XML invalide.");
     if(src.Entries.Any(e=>e.FullName.StartsWith("_xmlsignatures/",StringComparison.OrdinalIgnoreCase))) throw new NotSupportedException("Cette présentation possède une signature numérique. Optimisation refusée pour la préserver.");
     var bounds=ImageGeometry.Analyze(src,profile);
     var replacements=new Dictionary<string,byte[]>(StringComparer.Ordinal);
     var renames=new Dictionary<string,string>(StringComparer.Ordinal);
     var identical=new Dictionary<string,string>(StringComparer.Ordinal);
     foreach(var entry in src.Entries.Where(e=>e.FullName.StartsWith("ppt/media/",StringComparison.Ordinal))) {
      string suffix=Path.GetExtension(entry.FullName).ToLowerInvariant();if(suffix!=".jpg"&&suffix!=".jpeg"&&suffix!=".png")continue;
      try {
       byte[] bytes;using(var s=entry.Open())using(var m=new MemoryStream()){s.CopyTo(m);bytes=m.ToArray();}
       ImageBounds size;bounds.TryGetValue(entry.FullName,out size);bool jpeg;
       byte[] smaller=Compress(bytes,suffix,profile,size,out jpeg);
       if(smaller.Length<bytes.Length){replacements[entry.FullName]=smaller;result.Images++;if(jpeg&&suffix==".png"){
        string name="ppt/media/pptimiz-"+Guid.NewGuid().ToString("N")+".jpg";renames[entry.FullName]=name;
       }}
       if(profile!=CompressionProfile.Standard) {
        byte[] final; if(!replacements.TryGetValue(entry.FullName,out final))final=bytes;
        string format=renames.ContainsKey(entry.FullName)||suffix==".jpeg"||suffix==".jpg"?"jpeg":"png";
        string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=format+Convert.ToBase64String(sha.ComputeHash(final));
        string canonical;if(identical.TryGetValue(hash,out canonical)){renames[entry.FullName]=canonical;result.Duplicates++;}
        else identical[hash]=renames.ContainsKey(entry.FullName)?renames[entry.FullName]:entry.FullName;
       }
      }catch(Exception ex){result.Warnings.Add(entry.FullName+" conservée : "+ex.Message);}
     }
     using(var dst=ZipFile.Open(temp,ZipArchiveMode.Create)) {
      var written=new HashSet<string>(StringComparer.Ordinal);
      foreach(var entry in src.Entries) {
       string name; if(!renames.TryGetValue(entry.FullName,out name))name=entry.FullName;
       if(!written.Add(name))continue;
       byte[] bytes;
       if(!replacements.TryGetValue(entry.FullName,out bytes)) {
        if(renames.Count>0&&entry.FullName.EndsWith(".rels",StringComparison.Ordinal))bytes=ImageGeometry.RewriteRelationships(entry,renames);
        else if(renames.Count>0&&entry.FullName=="[Content_Types].xml")bytes=ImageGeometry.RewriteContentTypes(entry,renames);
        if(bytes==null)using(var stream=entry.Open())using(var mem=new MemoryStream()){stream.CopyTo(mem);bytes=mem.ToArray();}
       }
       var target=dst.CreateEntry(name,CompressionLevel.Optimal); target.LastWriteTime=entry.LastWriteTime;
       using(var stream=target.Open()) stream.Write(bytes,0,bytes.Length);
      }
     }
     // Read every resulting entry, validate XML, and verify the exact package inventory.
     using(var check=ZipFile.OpenRead(temp)) {
      if(!src.Entries.Select(e=>renames.ContainsKey(e.FullName)?renames[e.FullName]:e.FullName).Distinct(StringComparer.Ordinal).OrderBy(n=>n).SequenceEqual(check.Entries.Select(e=>e.FullName).OrderBy(n=>n))) throw new InvalidDataException("Structure du paquet modifiée.");
      foreach(var entry in check.Entries) using(var stream=entry.Open()) {
       if(entry.FullName.EndsWith(".xml",StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)) {
        var settings=new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null }; using(var reader=XmlReader.Create(stream,settings)) while(reader.Read()) {}
       } else stream.CopyTo(Stream.Null);
      }
     }
    }
    result.Layouts=LayoutCleaner.Clean(temp);
    using(var check=ZipFile.OpenRead(temp))ImageGeometry.ValidateRelationships(check);
    using(var check=ZipFile.OpenRead(temp)) foreach(var entry in check.Entries) using(var stream=entry.Open()) {
     if(entry.FullName.EndsWith(".xml",StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)) {
      using(var reader=XmlReader.Create(stream,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null })) while(reader.Read()) {}
     } else stream.CopyTo(Stream.Null);
    }
    result.After=new FileInfo(temp).Length;
    if(result.After<result.Before) { File.Move(temp,output); result.Output=output; }
    else { result.After=result.Before; result.Images=0; result.Layouts=0; result.Duplicates=0; }
    return result;
   } finally { if(File.Exists(temp)) File.Delete(temp); }
  }
  static byte[] Compress(byte[] bytes,string suffix,CompressionProfile profile,ImageBounds bounds,out bool jpeg) {
   jpeg=suffix!=".png";
   using(var memory=new MemoryStream(bytes)) using(var image=Image.FromStream(memory)) {
    // Animated images, EXIF-oriented photos and unusual pixel formats remain unchanged.
    if(image.FrameDimensionsList.Any(d=>image.GetFrameCount(new FrameDimension(d))>1))return bytes;
    if(image.PropertyIdList.Contains(0x112)&&BitConverter.ToUInt16(image.GetPropertyItem(0x112).Value,0)!=1)return bytes;
    int limit=profile==CompressionProfile.Standard?2560:profile==CompressionProfile.FullHD?1920:1280;
    double ratio=Math.Min(1.0,bounds==null?(double)limit/Math.Max(image.Width,image.Height):Math.Max(bounds.Width/image.Width,bounds.Height/image.Height));
    bool convert=profile!=CompressionProfile.Standard&&suffix==".png"&&bytes.Length>65536;
    if(suffix==".png" && ratio==1 && !convert) return bytes;
    int width=Math.Max(1,(int)Math.Round(image.Width*ratio)), height=Math.Max(1,(int)Math.Round(image.Height*ratio));
    using(var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb)) {
     if(image.HorizontalResolution>0 && image.VerticalResolution>0) bitmap.SetResolution(image.HorizontalResolution,image.VerticalResolution);
     using(var graphics=Graphics.FromImage(bitmap)) using(var attributes=new ImageAttributes()) {
      graphics.CompositingMode=CompositingMode.SourceCopy; graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
      attributes.SetWrapMode(WrapMode.TileFlipXY);
      graphics.DrawImage(image,new Rectangle(0,0,width,height),0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
     }
     using(var output=new MemoryStream()) {
      jpeg=suffix!=".png"||(convert&&Opaque(bitmap));
      if(!jpeg) bitmap.Save(output,ImageFormat.Png);
      else { var codec=ImageCodecInfo.GetImageEncoders().First(c=>c.FormatID==ImageFormat.Jpeg.Guid); using(var parameters=new EncoderParameters(1)) { parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,profile==CompressionProfile.Standard?90L:profile==CompressionProfile.FullHD?82L:72L); bitmap.Save(output,codec,parameters); } }
      return output.ToArray();
     }
    }
   }
  }
  static bool Opaque(Bitmap bitmap) {
   var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{byte[] row=new byte[bitmap.Width*4];for(int y=0;y<bitmap.Height;y++){System.Runtime.InteropServices.Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);for(int i=3;i<row.Length;i+=4)if(row[i]!=255)return false;}return true;}
   finally{bitmap.UnlockBits(data);}
  }
  public static string UniqueOutput(string input) {
   string stem=Path.Combine(Path.GetDirectoryName(input),Path.GetFileNameWithoutExtension(input)+"_optimise"); string ext=Path.GetExtension(input);
   for(int i=0;;i++) { string candidate=stem+(i==0?"":"_"+i)+ext; if(!File.Exists(candidate)) return candidate; }
  }
 }
}

