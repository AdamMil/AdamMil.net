using System;
using System.IO;
using System.Text;

namespace AdamMil.UI.RichDocument
{

#region DocumentParser
/// <summary>A base class for parsers to convert various document types into <see cref="Document"/> objects.</summary>
public abstract class DocumentParser
{
  /// <include file="documentation.xml" path="/UI/DocumentParser/Parse/*"/>
  public abstract Document Parse(Stream data);
}
#endregion

#region HtmlParser
/// <summary>A parser to convert HTML documents into <see cref="Document"/> objects.</summary>
public class HtmlParser : DocumentParser
{
  /// <include file="documentation.xml" path="/UI/DocumentParser/Parse/*"/>
  public override Document Parse(Stream data)
  {
    throw new NotImplementedException();
  }
}
#endregion

#region RtfParser
/// <summary>A parser to convert RTF documents into <see cref="Document"/> objects.</summary>
public class RtfParser : DocumentParser
{
  /// <include file="documentation.xml" path="/UI/DocumentParser/Parse/*"/>
  public override Document Parse(Stream data)
  {
    throw new NotImplementedException();
  }
}
#endregion

} // namespace AdamMil.UI.RichDocument