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
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using AdamMil.Security.PGP;

namespace AdamMil.Security.UI
{

/// <summary>This form allows the user to search for keys on a key server and import them.</summary>
public partial class KeyServerSearchForm : Form
{
  /// <summary>Creates a new <see cref="KeyServerSearchForm"/>. The form cannot be used until the <see cref="PGP"/>
  /// property has been set.
  /// </summary>
  public KeyServerSearchForm() : this(null, null) { }

  /// <summary>Initializes a new <see cref="KeyServerSearchForm"/> with the <see cref="PGPSystem"/> that will be used
  /// to search for keys. Keys selected by the user will be added to the default keyring.
  /// </summary>
  public KeyServerSearchForm(PGPSystem pgp) : this(pgp, null) { }

  /// <summary>Initializes a new <see cref="KeyServerSearchForm"/> with the <see cref="PGPSystem"/> that will be used
  /// to search for keys. Keys selected by the user will be added to the given keyring.
  /// </summary>
  public KeyServerSearchForm(PGPSystem pgp, Keyring importKeyring)
  {
    InitializeComponent();

    keyservers.Items.Clear();
    keyservers.Items.AddRange(PGPUI.GetDefaultKeyServers());
    keyservers.SelectedIndex = 0;

    this.pgp     = pgp;
    this.keyring = importKeyring;
  }

  /// <summary>Raised when an import completes successfully.</summary>
  public event EventHandler ImportCompleted;
  /// <summary>Raised when a search completes sucessfully.</summary>
  public event EventHandler SearchCompleted;

  /// <summary>Gets a collection of strings containing key server URIs, from which the user can choose.</summary>
  public ComboBox.ObjectCollection KeyServers
  {
    get { return keyservers.Items; }
  }

  /// <summary>Gets or sets the keyring into which selected keys will be imported. If null, the default keyring will be
  /// used.
  /// </summary>
  public Keyring Keyring
  {
    get { return keyring; }
    set { keyring = value; }
  }

  /// <summary>Gets or sets the <see cref="PGPSystem"/> that will be used to search for keys.</summary>
  public PGPSystem PGP
  {
    get { return pgp; }
    set
    {
      pgp = value;
      UpdateImportButton();
    }
  }

  /// <summary>Gets whether a search or import is currently in progress.</summary>
  public bool TaskInProgress
  {
    get { return thread != null; }
  }

  /// <summary>Begins a search on the given key server for the given search terms.</summary>
  public void BeginSearch(string terms, Uri keyServer)
  {
    if(terms == null || keyServer == null) throw new ArgumentNullException();
    if(PGP == null) throw new InvalidOperationException("The PGP property is not set.");
    if(TaskInProgress) throw new InvalidOperationException("A search or import is in progress.");

    terms = terms.Trim();
    if(string.IsNullOrEmpty(terms)) throw new ArgumentException("No search terms were provided.");

    progressBar.Style = ProgressBarStyle.Marquee;
    
    results.Items.Clear();
    searchServer = keyServer; // save the server used for the search so we know from what server to import the keys

    thread = new Thread(delegate()
    {
      try
      {
        PGP.FindPublicKeysOnServer(keyServer, GotResults,
                                   terms.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        Invoke((ThreadStart)delegate { SearchSucceeded(); });
      }
      catch(Exception ex)
      {
        Invoke((ThreadStart)delegate { SearchFailed(ex); });
      }
    });

    UpdateButtons();

    thread.Start();
  }

  /// <summary>Cancels the search or import that is currently in progress.</summary>
  public void CancelTask()
  {
    Thread thread = this.thread; // grab a local copy so it doesn't disappear out from under us
    if(thread != null)
    {
      thread.Abort();
      SearchFinished();
    }
  }

  /// <summary>Selects the given key server in the user interface.</summary>
  public void SelectKeyServer(Uri keyServer)
  {
    if(keyServer == null) throw new ArgumentNullException();
    keyservers.Text = keyServer.AbsoluteUri;
  }

  /// <include file="documentation.xml" path="/UI/Common/OnShown/*"/>
  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    terms.Focus();
  }

  /// <summary>Called when a search is to be started by the user.</summary>
  void BeginSearchFromUI()
  {
    Uri keyServer;
    try { keyServer = new Uri(keyservers.Text, UriKind.Absolute); }
    catch
    {
      MessageBox.Show(keyservers.Text + " is not a valid key server.", "Invalid key server",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
      return;
    }

    searchStartedFromUI = true;
    BeginSearch(terms.Text, keyServer);
  }

  /// <summary>The callback that is called when a chunk of search results have been returned from the server.</summary>
  bool GotResults(PrimaryKey[] keys)
  {
    results.Invoke((ThreadStart)delegate { results.AddResults(keys); });
    return true;
  }

  /// <summary>Imports the keys with the given IDs from the given key server.</summary>
  void ImportKeys(string[] ids, Uri keyServer)
  {
    if(TaskInProgress) throw new InvalidOperationException("A search or import is in progress.");
    
    thread = new Thread((ThreadStart)delegate
    {
      ImportedKey[] results;
      try { results = PGP.ImportKeysFromServer(new KeyDownloadOptions(keyServer), keyring, ids); }
      catch(Exception ex)
      {
        Invoke((ThreadStart)delegate { ImportFailed(ex); });
        return;
      }

      Invoke((ThreadStart)delegate { ImportFinished(results); });
    });

    UpdateButtons();

    progressBar.Style = ProgressBarStyle.Marquee;
    thread.Start();
  }

  /// <summary>Called if the import fails with an exception.</summary>
  void ImportFailed(Exception ex)
  {
    ImportFinished(null);
    if(!(ex is ThreadAbortException))
    {
      MessageBox.Show("The key import failed.", "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  /// <summary>Called when an import has finished, whether successfully or not.</summary>
  void ImportFinished(ImportedKey[] results)
  {
    TaskFinished();

    if(results != null)
    {
      if(ImportCompleted != null) ImportCompleted(this, EventArgs.Empty);

      int failCount = 0;
      foreach(ImportedKey result in results)
      {
        if(!result.Successful) failCount++;
      }

      MessageBox.Show((results.Length - failCount).ToString() + " key(s) imported successfully." +
                      (failCount == 0 ? null : "\n" + failCount.ToString() + " key(s) failed."), "Import results.",
                      MessageBoxButtons.OK, failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
  }

  /// <summary>Called if a search fails with an exception.</summary>
  void SearchFailed(Exception ex)
  {
    bool throwError = !searchStartedFromUI; // save this because it'll be clobbered by SearchFinished

    SearchFinished();

    if(!(ex is ThreadAbortException))
    {
      if(throwError) throw ex;
      else
      {
        MessageBox.Show("An error occurred during the search.", "Search failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }

  /// <summary>Called when a search finishes, whether successfully or not.</summary>
  void SearchFinished()
  {
    searchStartedFromUI = false;
    TaskFinished();
  }

  /// <summary>Called when a search completes without error.</summary>
  void SearchSucceeded()
  {
    SearchFinished();
    if(SearchCompleted != null) SearchCompleted(this, EventArgs.Empty);
  }

  /// <summary>Called when a task (search or import) finishes, whether successfully or not.</summary>
  void TaskFinished()
  {
    thread = null;
    UpdateButtons();
    progressBar.Style = ProgressBarStyle.Blocks;
  }

  /// <summary>Called to update both the import and search buttons.</summary>
  void UpdateButtons()
  {
    UpdateImportButton();
    UpdateSearchButton();
  }

  /// <summary>Updates the Enabled state of the import button.</summary>
  void UpdateImportButton()
  {
    btnImport.Enabled = PGP != null && !TaskInProgress && results.CheckedIndices.Count != 0;
  }

  /// <summary>Updates the Enabled state of the search button.</summary>
  void UpdateSearchButton()
  {
    if(TaskInProgress)
    {
      btnSearch.Text    = "&Cancel";
      btnSearch.Enabled = true;
    }
    else
    {
      btnSearch.Text = "&Search";
      btnSearch.Enabled = terms.Text.Trim().Length != 0;
    }
  }

  void btnImport_Click(object sender, EventArgs e)
  {
    ImportKeys(results.GetSelectedIds(), searchServer);
  }

  void btnSearch_Click(object sender, EventArgs e)
  {
    if(TaskInProgress) CancelTask(); // the Search button becomes a Cancel button when a task is in progress
    else BeginSearchFromUI();
  }

  void results_ItemChecked(object sender, ItemCheckedEventArgs e)
  {
    UpdateImportButton(); // the import button is enabled when at least one key is checked
  }

  void terms_KeyDown(object sender, KeyEventArgs e)
  {
    // start a search when the user presses enter
    if(!e.Handled && e.KeyCode == Keys.Enter && e.Modifiers == Keys.None && !TaskInProgress && btnSearch.Enabled)
    {
      BeginSearchFromUI();
      e.Handled = true;
    }
  }

  void terms_TextChanged(object sender, EventArgs e)
  {
    UpdateSearchButton(); // the search button is enabled only if a valid search term is entered
  }

  PGPSystem pgp;
  Thread thread;
  Keyring keyring;
  Uri searchServer;
  bool searchStartedFromUI;
}

} // namespace AdamMil.Security.UI