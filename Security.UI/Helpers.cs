/*
AdamMil.Security.UI is a .NET library providing common user interface widgets
for OpenPGP-based software.
Copyright (C) 2008-2011 Adam Milazzo (http://www.adammil.net/)

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
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

#region ListItem
/// <summary>Represents a list item with a value and the text to display for that value. The list item is meant to be
/// used with list boxes and combo boxes.
/// </summary>
/// <typeparam name="ValueType">The type of the value.</typeparam>
public class ListItem<ValueType>
{
  /// <summary>Initializes a new <see cref="ListItem{T}"/> with the given value and text.</summary>
  public ListItem(ValueType value, string text)
  {
    this.value = value;
    this.text  = text;
  }

  /// <summary>Gets or sets the text to display for this item.</summary>
  public string Text
  {
    get { return text; }
    set { text = value; }
  }

  /// <summary>Gets or sets the value associated with this item.</summary>
  public ValueType Value
  {
    get { return value; }
    set { this.value = value; }
  }

  /// <summary>Returns the text to display for this item.</summary>
  public override string ToString()
  {
    return text;
  }

  string text;
  ValueType value;
}
#endregion

#region KeyItem
/// <summary>Represents a <see cref="ListItem{T}"/> that stores a <see cref="PrimaryKey"/> and displays the key ID and
/// primary user ID as the item text.
/// </summary>
public sealed class KeyItem : ListItem<PrimaryKey>
{
  /// <summary>Initializes a new <see cref="KeyItem"/> with the given key.</summary>
  public KeyItem(PrimaryKey key) : base(key, PGPUI.GetKeyName(key)) { }
}
#endregion

#region KeyEventHandler
/// <summary>An event handler that related to a primary key.</summary>
public delegate void KeyEventHandler(object sender, PrimaryKey key);
#endregion

#region PasswordStrength
/// <summary>Represents the estimated strength of a password.</summary>
/// <remarks>This is only an estimate of the password strength, assuming a brute force character-based attack. If the
/// user uses a password that contains publically available information like his wife's birthday, it will be possible
/// to guess the password in much less time than is required for a brute force search. Similarly, if the user uses a
/// passphrase consisting of simple, common words, it may be brute forced easily.
/// </remarks>
public enum PasswordStrength
{
  /// <summary>The password is blank.</summary>
  Blank,
  /// <summary>The password is very weak (less than 5 characters in length, less than 3 unique characters, or less
  /// than 53 bits of estimated search space).
  /// </summary>
  VeryWeak,
  /// <summary>The password is weak (less than 7 characters in length, less than 5 unique characters, or less than 57
  /// bits of estimated search space).
  /// </summary>
  Weak,
  /// <summary>The password is not too weak, but not strong (less than 8 characters in length, less than 6 unique
  /// characters, or less than 65 bits of estimated search space).
  /// </summary>
  Moderate,
  /// <summary>The password is strong (less than 12 characters in length, less than 10 unique characters, or less than
  /// 80 bits of estimated search space).
  /// </summary>
  Strong,
  /// <summary>The password is very strong (at least 12 characters in length, with at least 10 unique characters, and
  /// at least 80 bits of estimated search space).
  /// </summary>
  VeryStrong
}
#endregion

/// <summary>This static class contains helpers for PGP UI applications.</summary>
public static class PGPUI
{
  /// <summary>Represents an arbitrary action to be executed.</summary>
  public delegate void Action();

  /// <summary>Given two <see cref="SecureTextBox"/> controls containing passwords, determines whether the passwords
  /// in the two controls are equal, case-sensitively.
  /// </summary>
  public static bool ArePasswordsEqual(SecureTextBox pass1, SecureTextBox pass2)
  {
    bool passwordsMatch = true;

    if(pass1.TextLength != pass2.TextLength)
    {
      passwordsMatch = false;
    }
    else
    {
      SecureString ss1 = null, ss2 = null;
      IntPtr bstr1 = IntPtr.Zero, bstr2 = IntPtr.Zero;

      try
      {
        ss1 = pass1.GetText();
        ss2 = pass2.GetText();
        bstr1 = Marshal.SecureStringToBSTR(ss1);
        bstr2 = Marshal.SecureStringToBSTR(ss2);

        unsafe
        {
          char* p1 = (char*)bstr1.ToPointer(), p2 = (char*)bstr2.ToPointer();

          int length = ss1.Length;
          for(int i=0; i<length; p1++, p2++, i++)
          {
            if(*p1 != *p2)
            {
              passwordsMatch = false;
              break;
            }
          }
        }
      }
      finally
      {
        Marshal.ZeroFreeBSTR(bstr1);
        Marshal.ZeroFreeBSTR(bstr2);
        ss1.Dispose();
        ss2.Dispose();
      }
    }

    return passwordsMatch;
  }

  /// <summary>Performs the given action with standard error handling.</summary>
  /// <param name="actionGerund">The name of the action, in the form of a gerund (i.e. using the "ing" suffix), or null to not
  /// name the action.
  /// </param>
  /// <param name="action">The action to execute.</param>
  /// <remarks><see cref="OperationCanceledException"/> will be ignored, while other exceptions will cause an error dialog to be
  /// displayed.
  /// </remarks>
  public static void DoWithErrorHandling(string actionGerund, Action action)
  {
    if(action == null) throw new ArgumentNullException();
    try { action(); }
    catch(OperationCanceledException) { }
    catch(Exception ex) { ShowErrorDialog(actionGerund, ex); }
  }

  /// <summary>Given a <see cref="UserAttribute"/>, including those of type <see cref="UserId"/>, returns the text to
  /// display as the name of the attribute.
  /// </summary>
  public static string GetAttributeName(UserAttribute attr)
  {
    UserId userId = attr as UserId;
    return userId != null ? userId.Name : attr is UserImage ? "Photo ID" : "Unknown user attribute";
  }

  /// <summary>Returns a list of strings containing the URIs of a set of commonly-used public key servers.</summary>
  public static string[] GetDefaultKeyServers()
  {
    return new string[]
    {
      "hkp://pgp.mit.edu", "hkp://www.stinkfoot.org", "hkp://kerckhoffs.surfnet.nl", "hkp://wwwkeys.pgp.net"
    };
  }

  /// <summary>Given a <see cref="PrimaryKey"/>, returns a string that can be used to represent the key to a user.</summary>
  public static string GetKeyName(PrimaryKey key)
  {
    if(key == null) throw new ArgumentNullException();
    return key.PrimaryUserId.Name + " (0x" + key.ShortKeyId + ")";
  }

  /// <summary>Given a <see cref="Subkey"/>, returns a string that can be used to represent the key to a user.</summary>
  public static string GetKeyName(Subkey key)
  {
    if(key == null) throw new ArgumentNullException();

    bool signing = key.HasCapabilities(KeyCapabilities.Sign);
    bool encryption = key.HasCapabilities(KeyCapabilities.Encrypt);
    string type = signing && encryption ? " signing/encryption" :
                  encryption            ? " encryption" :
                  signing               ? " signing" : null;

    return key.Length.ToString() + "-bit " + key.KeyType + type + " key, created on " +
           key.CreationTime.ToShortDateString();
  }

  /// <summary>Given a <see cref="Key"/>, gets a string that describes the validity of a key.</summary>
  public static string GetKeyValidityDescription(Key key)
  {
    if(key == null) throw new ArgumentNullException();
    return key.Revoked ? "revoked" : key.Expired ? "expired" : PGPUI.GetTrustDescription(key.CalculatedTrust);
  }

  /// <include file="documentation.xml" path="/UI/Helpers/GetPasswordStrength/*"/>
  public unsafe static PasswordStrength GetPasswordStrength(SecureString password, bool assumeHumanInput)
  {
    if(password.Length == 0) return PasswordStrength.Blank;

    IntPtr bstr = IntPtr.Zero;
    try
    {
      int uniqueChars = 0;
      bool hasLC=false, hasUC=false, hasNum=false, hasPunct=false;

      bstr = Marshal.SecureStringToBSTR(password);
      char* chars = (char*)bstr.ToPointer();
      int length = password.Length;
      bool* histo = stackalloc bool[97]; // 96 usable characters, plus one for "other" characters

      // loop through and categorize each character
      for(int i=0; i<length; i++)
      {
        char c = chars[i];
        CharType type = GetCharType(c);
        switch(type)
        {
          case CharType.Lowercase: hasLC = true; break;
          case CharType.Uppercase: hasUC = true; break;
          case CharType.Number: hasNum = true; break;
          case CharType.Punctuation: hasPunct = true; break;
        }

        // keep track of the number of unique characters, so we can say that "aaaaaaaaaaaaaaaaaaaaaa" is weak
        int histoIndex = c >= 32 && c < 127 ? c-32 : 96;
        if(!histo[histoIndex])
        {
          histo[histoIndex] = true;
          uniqueChars++;
        }
      }

      // free the password now that we've got the info we need
      Marshal.ZeroFreeBSTR(bstr);
      bstr = IntPtr.Zero;

      // clear the histogram from memory
      for(int i=0; i<97; i++) histo[i] = false;

      // calculate the number of possibilities per character. humans don't randomly choose from all possible
      // characters, so password crackers know to try the most common characters first
      int possibilitiesPerChar = 0;
      if(hasLC) possibilitiesPerChar += assumeHumanInput ? 19 : 26; // there are about 7 letters unlikely to be used
      if(hasUC) possibilitiesPerChar += assumeHumanInput ? 19 : 26;
      if(hasNum) possibilitiesPerChar += assumeHumanInput ? 9 : 10; // humans don't choose numbers randomly
      if(hasPunct) possibilitiesPerChar += assumeHumanInput ? 20 : 34; // humans don't choose from all 34 punctuation chars
      int bits = (int)Math.Truncate(Math.Log(Math.Pow(possibilitiesPerChar, password.Length), 2));

      // this code (written 2008, tweaked 2011) assumes:
      // BITS  Crack Time     Crack Time on Special Hardware (assumed 1000x speedup)
      // ----  -------------- ------------------------------------------------------
      // 40    Instant        Instant
      // 52    4 hours        15 seconds
      // 56    2.5 days       3.8 minutes
      // 60    40 days        1 hour
      // 64    1.8 years      16 hours
      // 68    29.9 years     10.9 days
      // 72    475 years      5.8 months
      // 76    7,500 years    7.6 years
      // 80    120,000 years  122 years

      if(password.Length <= 4 || bits <= 52 || uniqueChars <= 3) return PasswordStrength.VeryWeak;
      else if(password.Length <= 6 || bits <= 62 || uniqueChars <= 4) return PasswordStrength.Weak;
      else if(password.Length <= 8 || bits <= 72 || uniqueChars <= 5) return PasswordStrength.Moderate;
      else if(password.Length <= 12 || bits <= 80 || uniqueChars <= 9) return PasswordStrength.Strong;
      else return PasswordStrength.VeryStrong;
    }
    finally
    {
      if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
    }
  }

  /// <summary>Given a <see cref="PasswordStrength"/>, returns a description of the strength level.</summary>
  public static string GetPasswordStrengthDescription(PasswordStrength strength)
  {
    switch(strength)
    {
      case PasswordStrength.Blank: return "extremely weak!";
      case PasswordStrength.VeryWeak: return "very weak!";
      case PasswordStrength.Weak: return "weak!";
      case PasswordStrength.Moderate: return "moderate";
      case PasswordStrength.Strong: return "strong";
      case PasswordStrength.VeryStrong: return "very strong";
      default: throw new NotImplementedException("Unknown password strength.");
    }
  }

  /// <summary>Given an <see cref="OpenPGPSignatureType"/>, returns a description of the signature.</summary>
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

  /// <summary>Given a <see cref="TrustLevel"/>, returns a description of the trust level.</summary>
  public static string GetTrustDescription(TrustLevel level)
  {
    switch(level)
    {
      case TrustLevel.Full:     return "full";
      case TrustLevel.Marginal: return "marginal";
      case TrustLevel.Never:    return "none";
      case TrustLevel.Ultimate: return "ultimate";
      case TrustLevel.Unknown:  return "unknown";
      default: throw new NotImplementedException("Unknown trust level.");
    }
  }

  /// <summary>Given a <see cref="KeyEventArgs"/>, determines whether the key press is one that should close simple
  /// dialog.
  /// </summary>
  public static bool IsCloseKey(KeyEventArgs e)
  {
    return e.KeyCode == Keys.Escape && e.Modifiers == Keys.None || e.KeyCode == Keys.F4 && e.Modifiers == Keys.Alt;
  }

  /// <summary>Determines whether the given string contains a valid email address.</summary>
  public static bool IsValidEmail(string email)
  {
    string[] parts = email.Split('@');
    if(parts.Length != 2) return false;

    string local = parts[0], domain = parts[1];

    // if the local portion is quoted, strip off the quotes
    if(local.Length > 2 && local[0] == '"' && local[local.Length-1] == '"') local = local.Substring(1, local.Length-2);

    return emailLocalRe.IsMatch(local) && domainRe.IsMatch(domain);
  }

  /// <summary>Determines whether the given keyword matches the given primary key.</summary>
  public static bool KeyMatchesKeyword(PrimaryKey key, string keyword)
  {
    if(key == null || keyword == null) throw new ArgumentNullException();
    if(string.IsNullOrEmpty(keyword)) return false;

    foreach(UserId id in key.UserIds)
    {
      if(id.Name.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) != -1) return true;
    }

    if(key.EffectiveId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) != -1) return true;

    return false;
  }

  /// <summary>Given a desired file name, changes the name by removing characters that are not supported by the
  /// operating system.
  /// </summary>
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

  /// <summary>Displays an error dialog in response to an exception.</summary>
  /// <param name="actionGerund">The name of the action, in the form of a gerund (i.e. using the "ing" suffix), or null to not
  /// name the action.
  /// </param>
  /// <param name="exception">The related exception, or null to not display exception details.</param>
  public static void ShowErrorDialog(string actionGerund, Exception exception)
  {
    MessageBox.Show("An error occurred" + (string.IsNullOrEmpty(actionGerund) ? null : " while " + actionGerund) + "." +
                    (exception == null ? null : " The error was: " + exception.Message), "Error occurred",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
  }

  /// <summary>Displays the result of an import operation to the user.</summary>
  public static void ShowImportResults(ImportedKey[] results)
  {
    int failCount = 0;
    foreach(ImportedKey key in results)
    {
      if(!key.Successful) failCount++;
    }

    MessageBox.Show((results.Length - failCount).ToString() + " key(s) imported successfully." +
                      (failCount == 0 ? null : "\n" + failCount.ToString() + " key(s) failed."), "Import results",
                    MessageBoxButtons.OK, failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
  }

  /// <summary>Given two <see cref="SecureTextBox"/> controls containing passwords, checks that the passwords match.
  /// Message boxes will be displayed to the user if any problems are found with the passwords. True is returned if
  /// the passwords should be used, and false if not.
  /// </summary>
  public static bool ValidatePasswords(SecureTextBox pass1, SecureTextBox pass2)
  {
    if(!PGPUI.ArePasswordsEqual(pass1, pass2))
    {
      MessageBox.Show("The passwords you have entered do not match.", "Password mismatch", MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
      return false;
    }

    return true;
  }

  /// <summary>Given two <see cref="SecureTextBox"/> controls containing passwords, checks that the passwords match
  /// and are sufficiently strong. Message boxes will be displayed to the user if any problems are found with the
  /// passwords. True is returned if the passwords should be used, and false if not.
  /// </summary>
  public static bool ValidateAndCheckPasswords(SecureTextBox pass1, SecureTextBox pass2)
  {
    if(!ValidatePasswords(pass1, pass2))
    {
      return false;
    }
    else if(pass1.TextLength == 0)
    {
      if(MessageBox.Show("You didn't enter a password! This is extremely insecure, as anybody can use your key. Are "+
                         "you sure you don't want a password?", "Password is blank!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return false;
      }
    }
    else if(pass1.GetPasswordStrength() < PasswordStrength.Moderate)
    {
      if(MessageBox.Show("You entered a weak password! This is not secure, as your password can be cracked in a "+
                         "relatively short period of time, allowing somebody access to your key. Are you sure you "+
                         "want a to use a weak password?", "Password is weak!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>Given a name, optional email address, and optional comment, determines whether they constitute a valid
  /// user ID. Message boxes may be displayed to the user if any problems are found with the user ID. True is returned
  /// if the values should be used, and false if not.
  /// </summary>
  public static bool ValidateUserId(string realName, string email, string comment)
  {
    if(string.IsNullOrEmpty(realName))
    {
      MessageBox.Show("You must enter your name.", "Name required", MessageBoxButtons.OK, MessageBoxIcon.Error);
      return false;
    }
    else if(!string.IsNullOrEmpty(email) && !PGPUI.IsValidEmail(email))
    {
      MessageBox.Show(email + " is not a valid email address.", "Invalid email",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
      return false;
    }

    return true;
  }

  enum CharType
  {
    Lowercase, Uppercase, Number, Punctuation, Space
  }

  static CharType GetCharType(char c)
  {
    if(char.IsLower(c)) return CharType.Lowercase;
    else if(char.IsUpper(c)) return CharType.Uppercase;
    else if(char.IsDigit(c)) return CharType.Number;
    else if(char.IsWhiteSpace(c)) return CharType.Space;
    else return CharType.Punctuation;
  }

  /// <summary>Matches the local portion of an email (the portion before the @ sign).</summary>
  static readonly Regex emailLocalRe = new Regex(@"^[\w\d\.!#$%/?|^{}`~&'+=-]+(?:\.[\w\d\.!#$%/?|^{}`~&'+=-])*$",
                                                 RegexOptions.ECMAScript);
  /// <summary>Matches an email domain name (the portion of an email address after the @ sign).</summary>
  static readonly Regex domainRe = new Regex(@"^(?:[a-zA-Z\d]+(?:[\.\-][a-zA-Z\d]+)*|\[\d{1,3}(?:\.\d{1,3}){3}])$",
                                             RegexOptions.ECMAScript);
}

} // namespace AdamMil.Security.UI