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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;
using System.Text;

namespace AdamMil.Security.UI
{

/// <summary>Implements a relatively secure text box, that stores the entered text into a <see cref="SecureString"/>
/// rather than a regular string.
/// </summary>
public class SecureTextBox : TextBox
{
  /// <summary>Initializes a new <see cref="SecureTextBox"/>.</summary>
  public SecureTextBox()
  {
    ImeMode               = ImeMode.Disable;
    UseSystemPasswordChar = true;
  }

  /// <summary>Gets or sets whether the text will be limited to low ASCII characters (from ASCII code 32 to 126). The
  /// default value is false.
  /// </summary>
  [Category("Behavior")]
  [DefaultValue(false)]
  public bool RestrictToLowAscii
  {
    get { return restrictToAscii; }
    set { restrictToAscii = value; }
  }

  /// <summary>Gets a dummy string, not the text entered by the user. This property only exists because it exists on
  /// the base class. Attempting to set this property will cause an exception to be thrown.
  /// </summary>
  [Browsable(false)]
  public override string Text
  {
    get { return base.Text; }
    set { throw new NotSupportedException(); }
  }

  /// <summary>Removes all text from the <see cref="SecureTextBox"/>. This method should be used rather than the method on the
  /// base class (<see cref="TextBox"/>).
  /// </summary>
  public new void Clear()
  {
    text.Clear();
    base.Text = string.Empty;
  }

  /// <summary>Throws a <see cref="NotSupportedException"/>. Do not call the method on the base class (<see cref="TextBox"/>).</summary>
  public new void Copy()
  {
    throw new NotSupportedException();
  }

  /// <summary>Throws a <see cref="NotSupportedException"/>. Do not call the method on the base class (<see cref="TextBox"/>).</summary>
  public new void Cut()
  {
    throw new NotSupportedException();
  }

  /// <summary>Returns an estimate of the text strength, assuming it's a password entered by a human.</summary>
  /// <seealso cref="PGPUI.GetPasswordStrength"/>
  public PasswordStrength GetPasswordStrength()
  {
    return GetPasswordStrength(true);
  }

  /// <include file="documentation.xml" path="/UI/Helpers/GetPasswordStrength/*[@name != 'password']"/>
  public PasswordStrength GetPasswordStrength(bool assumeHumanInput)
  {
    return PGPUI.GetPasswordStrength(text, assumeHumanInput);
  }

  /// <summary>Returns a copy of the <see cref="SecureString"/> in which the text is stored. The copy return should be
  /// disposed when you are done with it.
  /// </summary>
  public SecureString GetText()
  {
    return text.Copy();
  }

  /// <summary>Inserts the clipboard contents into the password, overwriting the current selection if there is one. Do not call
  /// the method on the base class (<see cref="TextBox"/>).
  /// </summary>
  public new void Paste()
  {
    if(Clipboard.ContainsText())
    {
      string text = Clipboard.GetText();
      if(!string.IsNullOrEmpty(text))
      {
        Paste(text);
        text = null;
        GC.Collect(); // try to remove the text from memory
      }
    }
  }

  /// <summary>Inserts the given text into the password, overwriting the current selection if there is one. Do not call the
  /// method on the base class (<see cref="TextBox"/>).
  /// </summary>
  public new void Paste(string text)
  {
    if(SelectionLength != 0) DeleteSelection(true);
    if(text != null)
    {
      int start = Math.Min(text.Length, SelectionStart);
      for(int i=text.Length-1; i >= 0; i--)
      {
        char c = text[i];
        if(!IsCharRestricted(c) && c >= 32 && !char.IsControl(c)) this.text.InsertAt(SelectionStart, c);
      }
      base.Text = base.Text.Insert(SelectionStart, new string('*', text.Length));
    }
  }

  /// <summary>Sets the text to a copy of the given <see cref="SecureString"/>.</summary>
  public void SetText(SecureString text)
  {
    if(text == null) throw new ArgumentException();
    this.text = text.Copy();
    base.Text = new string('*', text.Length);
    SelectionLength = 0;
    SelectionStart  = text.Length;
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose/*"/>
  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    text.Dispose();
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyDown/*"/>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    if(e.Modifiers != Keys.None)
    {
      if(e.KeyCode == Keys.A && e.Modifiers == Keys.Control) // Ctrl-A selects all text
      {
        SelectAll();
        e.Handled = e.SuppressKeyPress = true;
        return;
      }
      // for modified keys, only allow cursor movement, alt-keys, and shifted characters
      else if(!(e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Home || e.KeyCode == Keys.End ||
                e.Modifiers == Keys.Alt ||
                (e.Modifiers == Keys.Shift &&
                 e.KeyCode != Keys.Insert && e.KeyCode != Keys.Delete && e.KeyCode != Keys.Back)))
      {
        e.Handled = e.SuppressKeyPress = true;
        return;
      }
    }
    else if(e.KeyCode == Keys.Delete)
    {
      if(SelectionLength != 0) DeleteSelection(false); // delete the selection if there is one
      else if(SelectionStart < TextLength) text.RemoveAt(SelectionStart); // otherwise, delete the character at the cursor
    }
    else if(e.KeyCode == Keys.Back)
    {
      if(SelectionLength != 0) DeleteSelection(false); // delete the selection if there is one
      else if(SelectionStart != 0) text.RemoveAt(SelectionStart-1); // otherwise, delete the character before the cursor
    }

    base.OnKeyDown(e);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnKeyPress/*"/>
  protected override void OnKeyPress(KeyPressEventArgs e)
  {
    if(IsCharRestricted(e.KeyChar)) // don't allow restricted characters
    {
      e.Handled = true;
      return;
    }

    if(e.KeyChar >= 32 && !char.IsControl(e.KeyChar)) // if it's not a control character...
    {
      if(SelectionLength != 0) DeleteSelection(false); // replace the selection if there is one
      text.InsertAt(SelectionStart, e.KeyChar); // then insert at the cursor position

      e.KeyChar = '*'; // don't send the real characters to the base class
    }

    base.OnKeyPress(e);
  }

  /// <inheritdoc/>
  protected override void WndProc(ref Message m)
  {
    // we need to intercept clipboard commands to avoid the textbox getting out of sync with the SecureString
    switch(m.Msg)
    {
      case 0x0300: DeleteSelection(true); break; // WM_CUT
      case 0x0302: Paste(); break; // WM_PASTE
      case 0x0303: Clear(); break; // WM_CLEAR
      case 0x0304: // WM_UNDO
        m.Result = new IntPtr(0); // we don't support undo
        return;
      default:
        base.WndProc(ref m);
        return;
    }

    m.Result = new IntPtr(1);
  }

  /// <summary>Removes all characters in the selection.</summary>
  void DeleteSelection(bool includeBaseText)
  {
    for(int i=0, end=Math.Min(SelectionLength, text.Length-SelectionStart); i<end; i++) text.RemoveAt(SelectionStart);
    if(includeBaseText) base.Text = base.Text.Remove(SelectionStart, SelectionLength);
  }

  /// <summary>Determines whether the character is outside the allowable range of low ASCII characters.</summary>
  bool IsCharRestricted(char c)
  {
    return RestrictToLowAscii && (c < 32 || c >= 127) && c != '\b'; // we'll allow backspace
  }

  SecureString text = new SecureString();
  bool restrictToAscii;
}

} // namespace AdamMil.Security.UI