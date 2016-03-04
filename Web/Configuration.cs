/*
AdamMil.Web is a library providing helpful classes for web development using
the .NET Framework.

http://www.adammil.net/
Written 2015 by Adam Milazzo.

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/
using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using AdamMil.Configuration;
using AdamMil.Utilities;

namespace AdamMil.Web.Configuration
{

#region CompressionMapCollection
/// <summary>Implements a collection of <see cref="CompressionMapElement"/> objects.</summary>
public sealed class CompressionMapCollection : CustomElementCollection<CompressionMapElement>
{
  /// <summary>Gets the name of the file from which the default elements should be taken. If null or empty, elements will be taken from an
  /// internal media type compression map.
  /// </summary>
  [ConfigurationProperty("defaultFile")]
  public string DefaultFile
  {
    get { return (string)this["defaultFile"]; }
  }

  /// <inheritdoc/>
  protected override CompressionMapElement CreateElement()
  {
    return new CompressionMapElement();
  }

  /// <inheritdoc/>
  protected override object GetElementKey(CompressionMapElement element)
  {
    if(element == null) throw new ArgumentNullException();
    return element.MediaTypePattern;
  }

  /// <inheritdoc/>
  protected override bool ThrowOnDuplicate
  {
    get { return false; } // allow the user to overwrite default mappings without removing them first
  }

  /// <inheritdoc/>
  protected override void Init()
  {
    base.Init();

    using(Stream schemaStream = WebSection.GetManifestResourceStream("Resources/Compression.xsd"))
    {
      XmlSchema schema = XmlSchema.Read(schemaStream, (o, e) =>
      {
        throw new ConfigurationErrorsException("Error reading default media type compression map. " + e.Message, e.Exception);
      });
      Stream defaultStream = null;
      try
      {
        if(!string.IsNullOrEmpty(DefaultFile)) defaultStream = File.OpenRead(DefaultFile);
        else defaultStream = WebSection.GetManifestResourceStream("Resources/Compression.xml");

        XmlReaderSettings settings = new XmlReaderSettings()
        {
          CloseInput = true, IgnoreComments = true, IgnoreWhitespace = true, ValidationType = ValidationType.Schema
        };
        settings.Schemas.Add(schema);
        using(XmlReader reader = XmlReader.Create(defaultStream, settings))
        {
          if(reader.Read()) // if there's a root element...
          {
            if(reader.NodeType == XmlNodeType.XmlDeclaration) reader.Read();
            while(reader.Read() && reader.NodeType == XmlNodeType.Element) // for each 'entry' element...
            {
              BaseAdd(new CompressionMapElement()
              {
                MediaTypePattern = reader.GetAttribute("mediaType"), Compress = reader.GetBoolAttribute("compress", true),
              });
              if(!reader.IsEmptyElement) reader.Read();
            }
          }
        }
      }
      finally
      {
        Utility.Dispose(defaultStream);
      }
    }

    ResetModified(); // mark the default elements as not part of the changes that would need to be serialized
  }
}
#endregion

#region CompressionMapElement
/// <summary>Implements a <see cref="ConfigurationElement"/> that specifies whether resources matching a media type pattern should be
/// compressed.
/// </summary>
public sealed class CompressionMapElement : ConfigurationElement
{
  /// <summary>Gets whether the extension is considered the canonical extension for the media type.</summary>
  [ConfigurationProperty("compress", DefaultValue=true), TypeConverter(typeof(BooleanConverter))]
  public bool Compress
  {
    get { return (bool)this["compress"]; }
    internal set { this["compress"] = value; }
  }

  /// <summary>Gets the media type pattern.</summary>
  [ConfigurationProperty("mediaType", IsKey=true, IsRequired=true, DefaultValue="*")]
  [RegexStringValidator(@"^[a-zA-Z0-9!#\$%&'\*\+\-\.\^_`\|~]+(?:/[a-zA-Z0-9!#\$%&'\*\+\-\.\^_`\|~]+)?$")]
  public string MediaTypePattern
  {
    get { return (string)this["mediaType"]; }
    internal set { this["mediaType"] = value; }
  }
}
#endregion

#region MediaMapCollection
/// <summary>Implements a collection of <see cref="MediaMapElement"/> objects.</summary>
public sealed class MediaMapCollection : CustomElementCollection<MediaMapElement>
{
  /// <summary>Initializes a new <see cref="MediaMapCollection"/>.</summary>
  public MediaMapCollection() : base(new KeyComparer()) { }

  /// <summary>Gets the file extension to use when constructing a file name from an unknown media type, excluding the leading period, or
  /// null or empty to use the default from <see cref="DefaultFile"/>.
  /// </summary>
  [ConfigurationProperty("defaultExtension"), RegexStringValidator(MediaMapElement.ExtensionPattern + "|^$")]
  public string DefaultExtensionOverride
  {
    get { return (string)this["defaultExtension"]; }
  }

  /// <summary>Gets the name of the file from which the default elements should be taken. If null or empty, elements will be taken from an
  /// internal media type map.
  /// </summary>
  [ConfigurationProperty("defaultFile")]
  public string DefaultFile
  {
    get { return (string)this["defaultFile"]; }
  }

  /// <summary>Gets the media type to report when guessing a media type from an unknown file extension, or null or empty to use the default
  /// from <see cref="DefaultFile"/>.
  /// </summary>
  [ConfigurationProperty("defaultMediaType"), RegexStringValidator(MediaMapElement.MediaTypePattern + "|^$")]
  public string DefaultMediaTypeOverride
  {
    get { return (string)this["defaultMediaType"]; }
  }

  /// <summary>Gets the file extension to use when constructing a file name from an unknown media type, excluding the leading period, or
  /// null if there is no default.
  /// </summary>
  public string GetDefaultExtension()
  {
    return StringUtility.Coalesce(DefaultExtensionOverride, defaultExtension);
  }

  /// <summary>Gets the media type to report when guessing a media type from an unknown file extension, or null if there is no default.</summary>
  public string GetDefaultMediaType()
  {
    return StringUtility.Coalesce(DefaultMediaTypeOverride, defaultMediaType);
  }

  /// <inheritdoc/>
  protected override MediaMapElement CreateElement()
  {
    return new MediaMapElement();
  }

  /// <inheritdoc/>
  protected override object GetElementKey(MediaMapElement element)
  {
    if(element == null) throw new ArgumentNullException();
    string mediaType = element.MediaType, extension = element.Extension;
    return string.IsNullOrEmpty(mediaType) ? extension : string.IsNullOrEmpty(extension) ? mediaType : mediaType + "<" + extension;
  }

  /// <inheritdoc/>
  protected override bool ThrowOnDuplicate
  {
    get { return false; } // allow the user to overwrite default mappings without removing them first
  }

  /// <inheritdoc/>
  protected override void Init()
  {
    base.Init();

    using(Stream schemaStream = WebSection.GetManifestResourceStream("Resources/MediaTypes.xsd"))
    {
      XmlSchema schema = XmlSchema.Read(schemaStream, (o, e) =>
      {
        throw new ConfigurationErrorsException("Error reading default media type map. " + e.Message, e.Exception);
      });
      Stream defaultStream = null;
      try
      {
        if(!string.IsNullOrEmpty(DefaultFile)) defaultStream = File.OpenRead(DefaultFile);
        else defaultStream = WebSection.GetManifestResourceStream("Resources/MediaTypes.xml");

        XmlReaderSettings settings = new XmlReaderSettings()
        {
          CloseInput = true, IgnoreComments = true, IgnoreWhitespace = true, ValidationType = ValidationType.Schema
        };
        settings.Schemas.Add(schema);
        using(XmlReader reader = XmlReader.Create(defaultStream, settings))
        {
          if(reader.Read()) // if there's a root element...
          {
            if(reader.NodeType == XmlNodeType.XmlDeclaration) reader.Read();
            defaultExtension = reader.GetAttribute("defaultExtension");
            defaultMediaType  = reader.GetAttribute("defaultMediaType");
            while(reader.Read() && reader.NodeType == XmlNodeType.Element) // for each 'entry' element...
            {
              BaseAdd(new MediaMapElement()
              {
                MediaType = reader.GetAttribute("mediaType"), CanonicalMediaType = reader.GetBoolAttribute("canonicalMediaType", true),
                Extension = reader.GetAttribute("extension"), CanonicalExtension = reader.GetBoolAttribute("canonicalExtension")
              });
              if(!reader.IsEmptyElement) reader.Read();
            }
          }
        }
      }
      finally
      {
        Utility.Dispose(defaultStream);
      }
    }

    ResetModified(); // mark the default elements as not part of the changes that would need to be serialized
  }

  #region KeyComparer
  sealed class KeyComparer : System.Collections.IComparer
  {
    public int Compare(object ao, object bo)
    {
      string a = ao as string, b = bo as string;
      if(string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : -1;
      else if(string.IsNullOrEmpty(a)) return 1;

      int aMediaEnd, aExtensionStart, bMediaEnd, bExtensionStart;
      Parse(a, out aMediaEnd, out aExtensionStart);
      Parse(b, out bMediaEnd, out bExtensionStart);

      if(aMediaEnd == -1) // if A has only an extension...
      {
        return bExtensionStart == -1 ? 1 : // if B has only a media type, then there can be no match
          string.Compare(a, aExtensionStart, b, bExtensionStart, int.MaxValue, StringComparison.OrdinalIgnoreCase);
      }
      else if(aExtensionStart == -1) // if A has only a media type...
      {
        return bMediaEnd == -1 ? -1 : string.Compare(a, 0, b, 0, Math.Max(aMediaEnd, bMediaEnd), StringComparison.OrdinalIgnoreCase);
      }
      else // A has both a media type and an extension
      {
        int cmp = bMediaEnd == -1 ? 0 : string.Compare(a, 0, b, 0, Math.Max(aMediaEnd, bMediaEnd), StringComparison.OrdinalIgnoreCase);
        if(cmp == 0)
        {
          cmp = bExtensionStart == -1 ? 0 :
            string.Compare(a, aExtensionStart, b, bExtensionStart, int.MaxValue, StringComparison.OrdinalIgnoreCase);
        }
        return cmp;
      }
    }

    static void Parse(string key, out int mediaEnd, out int extensionStart)
    {
      int pipe = key.IndexOf('<');
      if(pipe == -1)
      {
        if(key.IndexOf('/') == -1) { mediaEnd = -1; extensionStart = 0; }
        else { mediaEnd = key.Length; extensionStart = -1; }
      }
      else
      {
        mediaEnd       = pipe;
        extensionStart = pipe+1;
      }
    }
  }
  #endregion

  string defaultExtension, defaultMediaType;
}
#endregion

#region MediaMapElement
/// <summary>Implements a <see cref="ConfigurationElement"/> mapping a media type to and/or from a file extension.</summary>
public sealed class MediaMapElement : ConfigurationElement
{
  /// <summary>Gets whether the extension is considered the canonical extension for the media type.</summary>
  [ConfigurationProperty("canonicalExtension", DefaultValue=false), TypeConverter(typeof(BooleanConverter))]
  public bool CanonicalExtension
  {
    get { return (bool)this["canonicalExtension"]; }
    internal set { this["canonicalExtension"] = value; }
  }

  /// <summary>Gets whether the media type is considered the canonical media type for the extension.</summary>
  [ConfigurationProperty("canonicalMediaType", DefaultValue=true), TypeConverter(typeof(BooleanConverter))]
  public bool CanonicalMediaType
  {
    get { return (bool)this["canonicalMediaType"]; }
    internal set { this["canonicalMediaType"] = value; }
  }

  /// <summary>Gets the file extension, without a leading period.</summary>
  [ConfigurationProperty("extension", IsKey=true)]
  public string Extension
  {
    get { return (string)this["extension"]; }
    internal set { this["extension"] = value; }
  }

  /// <summary>Gets the media type.</summary>
  [ConfigurationProperty("mediaType", IsKey=true)]
  public string MediaType
  {
    get { return (string)this["mediaType"]; }
    internal set { this["mediaType"] = value; }
  }

  /// <inheritdoc/>
  protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
  {
    if(reader == null) throw new ArgumentNullException();
    elementName = reader.LocalName;
    base.DeserializeElement(reader, serializeCollectionKey);
  }

  /// <inheritdoc/>
  protected override void PostDeserialize()
  {
    base.PostDeserialize();

    if(elementName != "clear")
    {
      bool extMatch = extensionRe.IsMatch(Extension), mediaMatch = mediaTypeRe.IsMatch(MediaType);
      if(elementName == "remove")
      {
        if(!extMatch && !mediaMatch)
        {
          throw new ConfigurationErrorsException("At least one of the extension or mediaType attributes is required.");
        }
      }
      else
      {
        if(!extMatch)
        {
          if(string.IsNullOrEmpty(Extension)) throw new ConfigurationErrorsException("The extension attribute is required.");
          else throw new ConfigurationErrorsException("Extension \"" + Extension + "\" doesn't match pattern " + ExtensionPattern);
        }
        if(!mediaMatch)
        {
          if(string.IsNullOrEmpty(MediaType)) throw new ConfigurationErrorsException("The mediaType attribute is required.");
          else throw new ConfigurationErrorsException("Media type \"" + MediaType + "\" doesn't match pattern " + MediaTypePattern);
        }
      }
    }
  }

  string elementName;

  // we can't use RegexStringValidator on the properties, unfortunately, because it seems to validate all properties when one property is
  // set programmatically, preventing us from initializing the element one property at a time
  internal const string ExtensionPattern = @"^[^\.].*$";
  internal const string MediaTypePattern = @"^[a-zA-Z0-9!#\$%&'\*\+\-\.\^_`\|~]+/[a-zA-Z0-9!#\$%&'\*\+\-\.\^_`\|~]+$";
  static readonly Regex extensionRe = new Regex(ExtensionPattern, RegexOptions.Compiled | RegexOptions.Singleline);
  static readonly Regex mediaTypeRe = new Regex(MediaTypePattern, RegexOptions.Compiled | RegexOptions.Singleline);
}
#endregion

#region WebSection
/// <summary>Implements a <see cref="ConfigurationSection"/> that contains the web configuration.</summary>
public sealed class WebSection : ConfigurationSection
{
  /// <summary>Gets a collection of <see cref="CompressionMapElement"/> that represent the configured media type compression map.</summary>
  [ConfigurationProperty("compression"), ConfigurationCollection(typeof(CompressionMapCollection))]
  public CompressionMapCollection CompressionMap
  {
    get { return (CompressionMapCollection)this["compression"]; }
  }

  /// <summary>Gets a collection of <see cref="MediaMapElement"/> that represent the configured media type map.</summary>
  [ConfigurationProperty("mediaTypeMap"), ConfigurationCollection(typeof(MediaMapCollection))]
  public MediaMapCollection MediaTypeMap
  {
    get { return (MediaMapCollection)this["mediaTypeMap"]; }
  }

  /// <summary>Gets the <see cref="WebSection"/> containing the web configuration, or null if no web configuration section
  /// exists.
  /// </summary>
  public static WebSection Get()
  {
    return ConfigurationManager.GetSection("AdamMil.Web") as WebSection;
  }

  internal static Stream GetManifestResourceStream(string path)
  {
    string name = typeof(MediaTypes).Namespace + "." + path.Replace('/', '.');
    return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
  }
}
#endregion

} // namespace AdamMil.Web.Configuration
