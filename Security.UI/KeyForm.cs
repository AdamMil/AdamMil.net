using System;
using System.ComponentModel;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form helps the user choose a single key from a list.</summary>
public partial class KeyForm : Form
{
  /// <summary>Creates a new <see cref="KeyForm"/>. You must call <see cref="Initialize"/> to initialize the form.</summary>
  public KeyForm()
  {
    InitializeComponent();
  }

  /// <summary>Initializes a new <see cref="KeyForm"/> with the keys from which the user can choose.</summary>
  public KeyForm(string description, PrimaryKey[] keys) : this()
  {
    Initialize(description, keys);
  }

  /// <summary>Gets the key selected by the user.</summary>
  [Browsable(false)]
  public PrimaryKey SelectedKey
  {
    get { return keys.SelectedIndex == -1 ? null : ((KeyItem)keys.SelectedItem).Value; }
  }

  /// <summary>Initializes this form with the keys from which the user can choose.</summary>
  public void Initialize(string description, PrimaryKey[] keys)
  {
    if(keys == null) throw new ArgumentNullException();
    if(keys.Length == 0) throw new ArgumentException("No keys were given.");

    if(!string.IsNullOrEmpty(description)) lblDescription.Text = description;

    this.keys.Items.Clear();
    foreach(PrimaryKey key in keys) this.keys.Items.Add(new KeyItem(key));
    this.keys.SelectedIndex = 0;
  }
}

} // namespace AdamMil.Security.UI