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

public partial class KeyServerSearchForm : Form
{
  public KeyServerSearchForm() : this(null, null) { }

  public KeyServerSearchForm(PGPSystem pgp) : this(pgp, null) { }

  public KeyServerSearchForm(PGPSystem pgp, Keyring importKeyring)
  {
    InitializeComponent();

    keyservers.Items.AddRange(PGPUI.GetDefaultKeyServers());
    keyservers.SelectedIndex = 0;

    this.pgp     = pgp;
    this.keyring = importKeyring;
  }

  public ComboBox.ObjectCollection KeyServers
  {
    get { return keyservers.Items; }
  }

  public Keyring Keyring
  {
    get { return keyring; }
    set { keyring = value; }
  }

  public PGPSystem PGPSystem
  {
    get { return pgp; }
    set
    {
      pgp = value;
      UpdateImportButton();
    }
  }

  public bool TaskInProgress
  {
    get { return thread != null; }
  }

  public void CancelTask()
  {
    Thread thread = this.thread; // grab a local copy so it doesn't disappear out from under us
    if(thread != null)
    {
      thread.Abort();
      SearchFinished();
    }
  }

  public void Search(string terms, Uri keyServer)
  {
    if(terms == null || keyServer == null) throw new ArgumentNullException();
    if(PGPSystem == null) throw new InvalidOperationException("PGPSystem is not set.");
    if(TaskInProgress) throw new InvalidOperationException("A search or import is in progress.");

    terms = terms.Trim();
    if(string.IsNullOrEmpty(terms)) throw new ArgumentException("No terms were provided.");

    progressBar.Style = ProgressBarStyle.Marquee;
    
    results.Items.Clear();
    searchServer = keyServer;

    thread = new Thread(delegate()
    {
      try
      {
        PGPSystem.FindPublicKeysOnServer(keyServer, GotResults,
                                         terms.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        Invoke((ThreadStart)delegate { SearchFinished(); });
      }
      catch(Exception ex)
      {
        Invoke((ThreadStart)delegate { SearchFailed(ex); });
      }
    });
    
    thread.Start();
  }

  public void SelectKeyServer(Uri keyServer)
  {
    if(keyServer == null) throw new ArgumentNullException();
    keyservers.Text = keyServer.AbsoluteUri;
  }

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    terms.Focus();
  }

  bool GotResults(PrimaryKey[] keys)
  {
    results.Invoke((ThreadStart)delegate { results.AddResults(keys); });
    return true;
  }

  void ImportKeys(string[] ids, Uri keyServer)
  {
    if(thread != null) throw new InvalidOperationException("A search or import is in progress.");
    
    thread = new Thread((ThreadStart)delegate
    {
      ImportedKey[] results;
      try { results = PGPSystem.ImportKeysFromServer(new KeyDownloadOptions(keyServer), keyring, ids); }
      catch(Exception ex)
      {
        Invoke((ThreadStart)delegate { ImportFailed(ex); });
        return;
      }

      Invoke((ThreadStart)delegate { ImportFinished(results); });
    });

    progressBar.Style = ProgressBarStyle.Marquee;

    thread.Start();
    UpdateButtons();
  }

  void ImportFailed(Exception ex)
  {
    ImportFinished(null);
    if(!(ex is ThreadAbortException))
    {
      MessageBox.Show("The key import failed.", "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
  }

  void ImportFinished(ImportedKey[] results)
  {
    TaskFinished();

    if(results != null)
    {
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

  void SearchFailed(Exception ex)
  {
    bool throwError = !searchStartedFromUI; // save this because it'll be clobbered by SearchFinished

    SearchFinished();

    if(!(ex is ThreadAbortException))
    {
      if(throwError) throw ex;
      else
      {
        MessageBox.Show("An error occurred during the search.", "Search failed", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
      }
    }
  }

  void SearchFinished()
  {
    searchStartedFromUI = false;
    TaskFinished();
  }

  void StartSearch()
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
    Search(terms.Text, keyServer);
    UpdateButtons();
  }

  void TaskFinished()
  {
    thread = null;
    UpdateButtons();
    progressBar.Style = ProgressBarStyle.Blocks;
  }

  void UpdateButtons()
  {
    UpdateImportButton();
    UpdateSearchButton();
  }

  void UpdateImportButton()
  {
    btnImport.Enabled = PGPSystem != null && !TaskInProgress && results.CheckedIndices.Count != 0;
  }

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
    string[] ids = results.GetSelectedIds();
    ImportKeys(results.GetSelectedIds(), searchServer);
  }

  void btnSearch_Click(object sender, EventArgs e)
  {
    if(TaskInProgress) CancelTask();
    else StartSearch();
  }

  void results_ItemChecked(object sender, ItemCheckedEventArgs e)
  {
    UpdateImportButton();
  }

  void terms_KeyDown(object sender, KeyEventArgs e)
  {
    // start a search when the user presses enter
    if(!e.Handled && e.Modifiers == Keys.None && e.KeyCode == Keys.Enter && !TaskInProgress && btnSearch.Enabled)
    {
      StartSearch();
      e.Handled = true;
    }
  }

  void terms_TextChanged(object sender, EventArgs e)
  {
    UpdateSearchButton();
  }

  PGPSystem pgp;
  Thread thread;
  Keyring keyring;
  Uri searchServer;
  bool searchStartedFromUI;
}

} // namespace AdamMil.Security.UI