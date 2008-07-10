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
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace AdamMil.Security.UI
{

public partial class ChangePasswordForm : Form
{
  public ChangePasswordForm()
  {
    InitializeComponent();
    UpdatePasswordStrength();
  }

  public SecureString GetPassword()
  {
    return pass1.GetText();
  }

  void btnOK_Click(object sender, EventArgs e)
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

    if(!passwordsMatch)
    {
      MessageBox.Show("The passwords you have entered do not match.", "Password mismatch", MessageBoxButtons.OK,
                      MessageBoxIcon.Error);
      return;
    }
    else if(pass1.TextLength == 0)
    {
      if(MessageBox.Show("You didn't enter a password! This is extremely insecure, as anybody can use your key. Are "+
                         "you sure you don't want a password?", "Password is blank!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return;
      }
    }
    else if(pass1.GetPasswordStrength() < PasswordStrength.Moderate)
    {
      if(MessageBox.Show("You entered a weak password! This is not secure, as your password can be cracked in a "+
                         "relatively short period of time, allowing somebody access to your key. Are you sure you "+
                         "want a to use a weak password?", "Password is weak!", MessageBoxButtons.YesNo,
                         MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
      {
        return;
      }
    }

    DialogResult = DialogResult.OK;
  }

  void pass1_TextChanged(object sender, EventArgs e)
  {
    UpdatePasswordStrength();
  }

  void UpdatePasswordStrength()
  {
    string strength;
    switch(pass1.GetPasswordStrength())
    {
      case PasswordStrength.Blank: strength = "extremely weak!"; break;
      case PasswordStrength.VeryWeak: strength = "very weak!"; break;
      case PasswordStrength.Weak: strength = "weak!"; break;
      case PasswordStrength.Moderate: strength = "moderate"; break;
      case PasswordStrength.Strong: strength = "strong"; break;
      case PasswordStrength.VeryStrong: strength = "very strong"; break;
      default: throw new NotImplementedException("Unknown password strength.");
    }

    lblStrength.Text = "Estimated password strength: " + strength;
  }
}

} // namespace AdamMil.Security.UI