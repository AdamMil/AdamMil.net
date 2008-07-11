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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

public enum PasswordStrength
{
  Blank, VeryWeak, Weak, Moderate, Strong, VeryStrong
}

public class SecureTextBox : TextBox
{
  public SecureTextBox()
  {
    ImeMode               = ImeMode.Disable;
    UseSystemPasswordChar = true;
  }

  [Category("Behavior")]
  [DefaultValue(false)]
  public bool RestrictToLowAscii
  {
    get { return restrictToAscii; }
    set { restrictToAscii = value; }
  }
  
  [Browsable(false)]
  public override string Text
  {
    get { return base.Text; }
    set { throw new NotSupportedException(); }
  }

  public PasswordStrength GetPasswordStrength()
  {
    return GetPasswordStrength(true);
  }

  public unsafe PasswordStrength GetPasswordStrength(bool assumeHumanInput)
  {
    if(text.Length == 0) return PasswordStrength.Blank;

    IntPtr bstr = IntPtr.Zero;
    try
    {
      int uniqueChars = 0;
      bool hasLC=false, hasUC=false, hasNum=false, hasPunct=false;

      bstr = Marshal.SecureStringToBSTR(text);
      char* chars = (char*)bstr.ToPointer();
      int length = text.Length;
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
      if(hasPunct) possibilitiesPerChar += assumeHumanInput ? 20 : 34; // humans don't choose from all 34 punct chars
      int bits = (int)Math.Truncate(Math.Log(Math.Pow(possibilitiesPerChar, text.Length), 2));

      // this code (written 2008) assumes:
      // BITS  Crack Time     Crack Time on Special Hardware (assumed 10x speedup)
      // ----  -------------- ----------------------------------------------------
      // 40    Instant        Instant
      // 52    8 hours        45 minutes
      // 56    5 days         12 hours
      // 60    80 days        10 days
      // 64    3.5 years      4 months
      // 72    900 years      90 years
      // 80    220,000 years  22,000 years

      if(text.Length <= 4 || bits <= 52 || uniqueChars <= 3) return PasswordStrength.VeryWeak;
      else if(text.Length <= 6 || bits <= 56 || uniqueChars <= 4) return PasswordStrength.Weak;
      else if(text.Length <= 7 || bits <= 64 || uniqueChars <= 5) return PasswordStrength.Moderate;
      else if(text.Length <= 11 || bits <= 72 || uniqueChars <= 9) return PasswordStrength.Strong;
      else return PasswordStrength.VeryStrong;
    }
    finally
    {
      if(bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
    }
  }

  public SecureString GetText()
  {
    return text.Copy();
  }

  public void SetText(SecureString text)
  {
    if(text == null) throw new ArgumentException();
    this.text = text.Copy();
    base.Text = new string('*', text.Length);
    SelectionStart = SelectionLength = 0;
  }

  protected override void OnKeyDown(KeyEventArgs e)
  {
    if(e.Modifiers != Keys.None)
    {
      // for modified keys, only allow cursor movement and shifted characters
      if(!(e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Home || e.KeyCode == Keys.End ||
           (e.Modifiers == Keys.Shift &&
            e.KeyCode != Keys.Insert && e.KeyCode != Keys.Delete && e.KeyCode != Keys.Back)))
      {
        e.Handled = e.SuppressKeyPress = true;
        return;
      }
    }
    else if(e.KeyCode == Keys.Delete)
    {
      if(SelectionLength != 0) DeleteSelection(); // delete the selection if there is one
      else if(SelectionStart < TextLength) text.RemoveAt(SelectionStart); // otherwise, delete the character at the cursor
    }
    else if(e.KeyCode == Keys.Back)
    {
      if(SelectionLength != 0) DeleteSelection(); // delete the selection if there is one
      else if(SelectionStart != 0) text.RemoveAt(SelectionStart-1); // otherwise, delete the character before the cursor
    }

    base.OnKeyDown(e);
  }

  protected override void OnKeyPress(KeyPressEventArgs e)
  {
    if(IsCharRestricted(e.KeyChar)) // don't allow restricted characters
    {
      e.Handled = true;
      return;
    }

    if(e.KeyChar >= 32 && !char.IsControl(e.KeyChar)) // if it's not a control character...
    {
      if(SelectionLength != 0) DeleteSelection(); // replace the selection if there is one
      text.InsertAt(SelectionStart, e.KeyChar); // then insert at the cursor position

      e.KeyChar = '*'; // don't send the real characters to the base class
    }

    base.OnKeyPress(e);
  }

  enum CharType
  {
    Lowercase, Uppercase, Number, Punctuation
  }

  void DeleteSelection()
  {
    for(int i=0; i<SelectionLength; i++) text.RemoveAt(SelectionStart);
  }

  bool IsCharRestricted(char c)
  {
    return RestrictToLowAscii && (c < 32 || c >= 127);
  }

  SecureString text = new SecureString();
  bool restrictToAscii;

  static CharType GetCharType(char c)
  {
    if(char.IsLower(c)) return CharType.Lowercase;
    else if(char.IsUpper(c)) return CharType.Uppercase;
    else if(char.IsDigit(c)) return CharType.Number;
    else return CharType.Punctuation;
  }
}

} // namespace AdamMil.Security.UI