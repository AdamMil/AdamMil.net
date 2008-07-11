/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008 Adam Milazzo (http://www.adammil.net/)

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
using System.IO;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

#region ListItem
public class ListItem<ValueType>
{
  public ListItem(ValueType value, string text)
  {
    this.value = value;
    this.text  = text;
  }

  public string Text
  {
    get { return text; }
    set { text = value; }
  }

  public ValueType Value
  {
    get { return value; }
    set { this.value = value; }
  }

  public override string ToString()
  {
    return text;
  }

  ValueType value;
  string text;
}
#endregion

#region KeyItem
public sealed class KeyItem : ListItem<PrimaryKey>
{
  public KeyItem(PrimaryKey key) : base(key, PGPUI.GetKeyName(key)) { }
}
#endregion

public static class PGPUI
{
  public static string GetAttributeName(UserAttribute attr)
  {
    UserId userId = attr as UserId;
    return userId != null ? userId.Name : attr is UserImage ? "Photo ID" : "Unknown user attribute";
  }

  public static string[] GetDefaultKeyServers()
  {
    return new string[]
    {
      "hkp://keyserver.mine.nu", "hkp://pgp.mit.edu", "hkp://wwwkeys.pgp.net", "ldap://certserver.pgp.com"
    };
  }

  public static string GetKeyName(PrimaryKey key)
  {
    if(key == null) throw new ArgumentNullException();
    return key.PrimaryUserId.Name + " (0x" + key.ShortKeyId + ")";
  }

  public static string GetKeyValidityDescription(Key key)
  {
    if(key == null) throw new ArgumentNullException();
    return key.Revoked ? "revoked" : key.Expired ? "expired" : PGPUI.GetTrustDescription(key.CalculatedTrust);
  }

  public static string GetSignatureDescription(OpenPGPSignatureType type)
  {
    switch(type)
    {
      case OpenPGPSignatureType.CanonicalBinary: case OpenPGPSignatureType.CanonicalText:
        return "data";
      case OpenPGPSignatureType.CasualCertification:
        return "casual certification";
      case OpenPGPSignatureType.CertificateRevocation:
        return "certificate revocation";
      case OpenPGPSignatureType.ConfirmationSignature:
        return "confirmation";
      case OpenPGPSignatureType.DirectKeySignature:
        return "data binding";
      case OpenPGPSignatureType.GenericCertification:
        return "certification";
      case OpenPGPSignatureType.PersonaCertification:
        return "unvalidated certification";
      case OpenPGPSignatureType.PositiveCertification:
        return "full certification";
      case OpenPGPSignatureType.PrimaryKeyBinding: case OpenPGPSignatureType.SubkeyBinding:
        return "key binding";
      case OpenPGPSignatureType.PrimaryKeyRevocation:
        return "key revocation";
      case OpenPGPSignatureType.Standalone:
        return "standalone";
      case OpenPGPSignatureType.SubkeyRevocation:
        return "subkey revocation";
      case OpenPGPSignatureType.TimestampSignature:
        return "timestamp";
      default:
        return "unknown";
    }
  }

  public static string GetTrustDescription(TrustLevel level)
  {
    switch(level)
    {
      case TrustLevel.Full:     return "full";
      case TrustLevel.Marginal: return "marginal";
      case TrustLevel.Never:    return "none";
      case TrustLevel.Ultimate: return "ultimate";
      default:                  return "unknown";
    }
  }

  public static string MakeSafeFilename(string str)
  {
    char[] badChars = Path.GetInvalidFileNameChars();

    System.Text.StringBuilder sb = new System.Text.StringBuilder(str.Length);
    foreach(char c in str)
    {
      if(Array.IndexOf(badChars, c) == -1) sb.Append(c);
    }
    return sb.ToString();
  }
}

} // namespace AdamMil.Security.UI