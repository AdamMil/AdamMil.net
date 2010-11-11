/*
AdamMil.UI is a library that provides useful user interface controls for the
.NET framework.

http://www.adammil.net/
Copyright (C) 2008-2010 Adam Milazzo

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
using System.Text;

namespace AdamMil.UI.RichDocument
{

#region ChangeEvent
/// <summary>Represents a change to the document. All changes to a document should happen through change events,
/// to allow changes to be undone.
/// </summary>
/// <remarks>Most change events are sensitive to the state of the document at the time when they are instantiated.
/// If the document is not in the expected state, the <see cref="Do"/> and <see cref="Undo"/> methods may raise an
/// <see cref="InvalidOperationException"/>. Therefore, it is usually best to create change events immediately before
/// they are used to modify the document.
/// </remarks>
public abstract class ChangeEvent : IDisposable
{
  /// <summary>Initializes a new <see cref="ChangeEvent"/> with a reference to the document to be modified, if any.</summary>
  /// <param name="document">The document to be modified, if any.</param>
  /// <remarks>The document reference is held and later checked to see if the document has changed.</remarks>
  protected ChangeEvent(Document document)
  {
    this.document        = document;
    this.expectedVersion = document == null ? 0 : document.Version;
  }

  /// <summary>Initializes a new <see cref="ChangeEvent"/> with a reference to a document node. The document to which
  /// the node belongs, if any, is passed to <see cref="ChangeEvent(Document)"/>.
  /// </summary>
  protected ChangeEvent(DocumentNode node) : this(node == null ? null : node.Document) { }

  /// <summary>Disposes of the current change event.</summary>
  ~ChangeEvent()
  {
    Dispose(true);
  }

  /// <summary>Error text to be used when <see cref="Do"/> is called after the change has already been applied.</summary>
  protected const string ChangeNotAppliedError = "This change has not been applied.";
  /// <summary>Error text to be used when <see cref="Undo"/> is called when the change is not applied (either because
  /// it was never applied or because Undo was already called to undo it).
  /// </summary>
  protected const string ChangeAlreadyAppliedError = "This change has already been applied.";
  /// <summary>Error text to be used when <see cref="Do"/> or <see cref="Undo"/> has been called, but an inconsistency
  /// in the document has been detected.
  /// </summary>
  protected const string StateChangedError = "The underlying document has been changed from its expected state.";

  /// <summary>Called when the change event is no longer needed by the undo/redo system.</summary>
  public void Dispose()
  {
    GC.SuppressFinalize(this);
    Dispose(false);
  }

  /// <summary>Returns a short, human-readable string describing the change that this event represents.</summary>
  public override abstract string ToString();

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal abstract void Do();

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal abstract void Undo();

  /// <summary>Gets the document with which this change is associated, if any.</summary>
  protected Document Document
  {
    get { return document; }
  }

  /// <summary>Called when the change event is no longer needed by the undo/redo system. This method should release
  /// any locks held on document nodes.
  /// </summary>
  protected virtual void Dispose(bool finalizing) { }

  /// <summary>Ensures that the document and document version match so that the <see cref="Do"/> operation can be
  /// performed.
  /// </summary>
  /// <param name="document">The current document of the node(s) to be affected.</param>
  protected void ValidateVersionForDo(Document document)
  {
    ValidateDocument(document);

    if(document != null && document.Version != expectedVersion) throw new InvalidOperationException(StateChangedError);
  }

  /// <summary>Ensures that the document and document version match so that the <see cref="Undo"/> operation can be
  /// performed.
  /// </summary>
  /// <param name="document">The current document of the node(s) to be affected.</param>
  protected void ValidateVersionForUndo(Document document)
  {
    ValidateDocument(document);

    if(document != null && document.Version != expectedVersion+1)
    {
      throw new InvalidOperationException(StateChangedError);
    }
  }

  /// <summary>Ensures that the document hasn't changed since the change event was created.</summary>
  /// <param name="document">The current document of the node(s) to be affected.</param>
  void ValidateDocument(Document document)
  {
    if(this.document != document)
    {
      throw new InvalidOperationException("The associated document node has changed its document since this change "+
                                          "event was created.");
    }
  }

  /// <summary>The document associated with the change event.</summary>
  readonly Document document;

  /// <summary>The expected version number of the document. This is the version number that the document should be at
  /// for the <see cref="Do"/> operation to succeed.
  /// </summary>
  readonly uint expectedVersion;
}
#endregion

#region CompositeChange
/// <summary>This class represents a single change event that groups multiple changes, allowing them to be done and
/// undone all together.
/// </summary>
public class CompositeChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="CompositeChange"/> object given a list of changes to include.</summary>
  public CompositeChange(params ChangeEvent[] changes) : this(null, changes) { }
  /// <summary>Initializes a new <see cref="CompositeChange"/> object given a list of changes to include, and the
  /// description of the composite change.
  /// </summary>
  public CompositeChange(string description, params ChangeEvent[] changes) : base((Document)null)
  {
    if(changes == null) throw new ArgumentNullException();
    if(changes.Length == 0) throw new ArgumentException("The change array is empty.");
    if(Array.IndexOf(changes, null) != -1) throw new ArgumentException("The change array contains a null value.");
    this.changes     = changes;
    this.description = description;
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    if(this.description != null) return this.description;

    string description = changes[0].ToString();
    if(changes.Length > 1) description += ", and "+(changes.Length-1).ToString()+" other changes";
    return description;
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose1/*"/>
  protected override void Dispose(bool finalizing)
  {
    foreach(ChangeEvent change in changes) change.Dispose();
    base.Dispose(finalizing);
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    // try to do all the changes in order
    for(int i=0; i<changes.Length; i++)
    {
      try { changes[i].Do(); }
      catch // if one change fails, try to roll back the changes that have already been done
      {
        for(i--; i >= 0; i--)
        {
          try { changes[i].Undo(); } // if rolling back fails, there's not much we can do.
          catch { }
        }
        throw; // rethrow the exception that caused the original failure
      }
    }
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    // try to do all the changes in reverse order
    for(int i=changes.Length; i >= 0; i--)
    {
      try { changes[i].Undo(); }
      catch // if one undo fails, try to roll back the undos that have already been done
      {
        for(i++; i<changes.Length; i++)
        {
          try { changes[i].Do(); } // if rolling back fails, there's not much we can do.
          catch { }
        }
        throw; // rethrow the exception that caused the original failure
      }
    }
  }

  readonly ChangeEvent[] changes;
  readonly string description;
}
#endregion

#region Child node changes
#region ClearNodeChange
/// <summary>Represents a change that clears all content in a <see cref="DocumentNode"/>.</summary>
/// <remarks>You shouldn't have to use this change event directly. Instead, you can call
/// <c>DocumentNode.Children.Clear</c>, which generates a <see cref="ClearNodeChange"/> implicitly.
/// </remarks>
public class ClearNodeChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="ClearNodeChange"/> with the node to clear.</summary>
  public ClearNodeChange(DocumentNode nodeToClear) : base(nodeToClear)
  {
    if(nodeToClear.Locked) throw new ArgumentException("The node to clear is locked.");
    this.nodeToClear = nodeToClear;
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    return "Clear "+nodeToClear.GetDescription();
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose1/*"/>
  protected override void Dispose(bool finalizing)
  {
    if(children != null)
    {
      foreach(DocumentNode node in children)
      {
        if(node.Locked) node.Unlock();
      }
    }

    base.Dispose(finalizing);
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(nodeToClear.Document);
    if(children != null) throw new InvalidOperationException(ChangeAlreadyAppliedError);

    // make a copy of the child nodes
    children = new DocumentNode[nodeToClear.Children.Count];
    nodeToClear.Children.CopyTo(children, 0);
    // lock the child nodes so that they cannot be modified while detached from the document
    foreach(DocumentNode node in children) node.Lock();
    // then remove them
    nodeToClear.Children.InternalClearItems();
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(nodeToClear.Document);
    if(children == null) throw new InvalidOperationException(ChangeNotAppliedError);
    if(nodeToClear.Children.Count != 0) throw new InvalidOperationException(StateChangedError); // we expect that it should be empty...

    // insert the children back into the node
    for(int i=0; i<children.Length; i++)
    {
      children[i].Unlock();
      nodeToClear.Children.InternalInsertItem(i, children[i]);
    }

    // then clear our copy
    children = null;
  }

  readonly DocumentNode nodeToClear;
  DocumentNode[] children;
}
#endregion

#region InsertNodeChange
/// <summary>Represents a change that inserts a new node into a document tree.</summary>
public class InsertNodeChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="InsertNodeChange"/>.</summary>
  /// <param name="parent">The node into which a new child will be inserted.</param>
  /// <param name="index">The index at which the new child will be inserted.</param>
  /// <param name="newItem">The new node to insert.</param>
  /// <remarks>You shouldn't have to use this change event directly. Instead, you can call
  /// <c>DocumentNode.Children.Add</c> or <c>DocumentNode.Children.Insert</c>, which generate
  /// an <see cref="InsertNodeChange"/> implicitly. Instantiating this change event will lock
  /// <paramref name="newItem"/> until <see cref="Do"/> is called, to help ensure that the new item is not changed in
  /// between.
  /// </remarks>
  public InsertNodeChange(DocumentNode parent, int index, DocumentNode newItem) : base(parent)
  {
    if(newItem == null) throw new ArgumentNullException("newItem");
    if(parent.Locked) throw new ArgumentException("The parent node is locked.");

    this.parent  = parent;
    this.index   = index;
    this.newItem = newItem;

    this.newItem.Lock(); // hold a lock on the node while it's not attached to the parent
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    return "Insert "+newItem.GetDescription();
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose1/*"/>
  protected override void Dispose(bool finalizing)
  {
    if(!inserted && newItem.Locked) newItem.Unlock();
    base.Dispose(finalizing);
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(parent.Document);
    if(inserted) throw new InvalidOperationException(ChangeAlreadyAppliedError);
    if(!newItem.Locked || parent.Children.Count < index) throw new InvalidOperationException(StateChangedError);

    newItem.Unlock();
    parent.Children.InternalInsertItem(index, newItem);
    inserted = true;
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(parent.Document);
    if(!inserted) throw new InvalidOperationException(ChangeNotAppliedError);
    if(newItem.Locked || parent.Children.Count <= index || parent.Children[index] != newItem)
    {
      throw new InvalidOperationException(StateChangedError);
    }

    parent.Children.RemoveAt(index);
    newItem.Lock();
    inserted = false;
  }

  readonly DocumentNode parent, newItem;
  readonly int index;
  bool inserted;
}
#endregion

#region ReplaceNodeChange
/// <summary>Represents a change that replaces one document node with another.</summary>
public class ReplaceNodeChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="ReplaceNodeChange"/>.</summary>
  /// <param name="parent">The node into which a new child will be placed.</param>
  /// <param name="index">The index at which the new child will be placed.</param>
  /// <param name="newItem">The new node to insert.</param>
  /// <remarks>You shouldn't have to use this change event directly. Instead, you can use
  /// <c>DocumentNode.Children[index] = value;</c>, which generates a <see cref="ReplaceNodeChange"/> implicitly.
  /// Instantiating this change event will lock <paramref name="newItem"/> until <see cref="Do"/> is called, and
  /// then lock the node that was replaced until <see cref="Undo"/> is called, to ensure that the nodes are not changed
  /// in between.
  /// </remarks>
  public ReplaceNodeChange(DocumentNode parent, int index, DocumentNode newItem) : base(parent)
  {
    if(newItem == null) throw new ArgumentNullException("newItem");
    this.parent  = parent;
    this.index   = index;
    this.newItem = newItem;
    this.oldItem = parent.Children[index];

    if(parent.Locked) throw new ArgumentException("The parent node is locked.");
    if(oldItem.Locked) throw new ArgumentException("The node to replace is locked.");

    this.newItem.Lock(); // hold a lock on the new node while it's not attached to the parent
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    return "Replace "+oldItem.GetDescription()+" with "+newItem.GetDescription();
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose1/*"/>
  protected override void Dispose(bool finalizing)
  {
    if(replaced)
    {
      if(oldItem.Locked) oldItem.Unlock();
    }
    else if(newItem.Locked) newItem.Unlock();

    base.Dispose(finalizing);
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(parent.Document);
    if(replaced) throw new InvalidOperationException(ChangeAlreadyAppliedError);
    if(!newItem.Locked || oldItem.Locked || parent.Children.Count <= index || parent.Children[index] != oldItem)
    {
      throw new InvalidOperationException(StateChangedError);
    }

    newItem.Unlock();
    parent.Children.InternalSetItem(index, newItem);
    oldItem.Lock();
    replaced = true;
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(parent.Document);
    if(!replaced) throw new InvalidOperationException(ChangeNotAppliedError);
    if(newItem.Locked || !oldItem.Locked || parent.Children.Count <= index || parent.Children[index] != newItem)
    {
      throw new InvalidOperationException(StateChangedError);
    }

    oldItem.Unlock();
    parent.Children.InternalSetItem(index, oldItem);
    newItem.Lock();
    replaced = false;
  }

  readonly DocumentNode parent, newItem, oldItem;
  readonly int index;
  bool replaced;
}
#endregion

#region RemoveNodeChange
/// <summary>Represents a change that removes a document node from the document node tree.</summary>
public class RemoveNodeChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="RemoveNodeChange"/>.</summary>
  /// <param name="parent">The node whose child will be removed.</param>
  /// <param name="index">The index of the child to remove.</param>
  /// <remarks>You shouldn't have to use this change event directly. Instead, you can call
  /// <c>DocumentNode.Children.Remove</c> or <c>DocumentNode.Children.RemoveAt</c>, which generates a
  /// <see cref="RemoveNodeChange"/> implicitly. Instantiating this change event will lock the removed node after
  /// <see cref="Do"/> is called, to help ensure that the node is not changed in case <see cref="Undo"/> is called.
  /// </remarks>
  public RemoveNodeChange(DocumentNode parent, int index) : base(parent)
  {
    if(parent.Locked) throw new ArgumentException("The parent node is locked.");

    this.parent = parent;
    this.index  = index;
    this.item   = parent.Children[index];
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    return "Delete "+item.GetDescription();
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose1/*"/>
  protected override void Dispose(bool finalizing)
  {
    if(removed && item.Locked) item.Unlock();
    base.Dispose(finalizing);
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(parent.Document);
    if(removed) throw new InvalidOperationException(ChangeAlreadyAppliedError);
    if(item.Locked || parent.Children.Count < index || parent.Children[index] != item)
    {
      throw new InvalidOperationException(StateChangedError);
    }

    parent.Children.InternalRemoveItem(index);
    item.Lock();
    removed = true;
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(parent.Document);
    if(!removed) throw new InvalidOperationException(ChangeNotAppliedError);
    if(!item.Locked || parent.Children.Count < index) throw new InvalidOperationException(StateChangedError);

    item.Unlock();
    parent.Children.InternalInsertItem(index, item);
    removed = false;
  }

  readonly DocumentNode parent, item;
  readonly int index;
  bool removed;
}
#endregion
#endregion

#region ClearDocumentChange
/// <summary>Represents a change that clears the entire document, restoring it to its original state.</summary>
public class ClearDocumentChange : ChangeEvent
{
  /// <summary>Initializes this <see cref="ClearDocumentChange"/> with the document to clear.</summary>
  public ClearDocumentChange(Document document) : base(document)
  {
    if(document == null) throw new ArgumentNullException();
    originalRoot = (RootNode)document.Root;
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    return "Clear the document";
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose1/*"/>
  protected override void Dispose(bool finalizing)
  {
    if(cleared && originalRoot.Locked) originalRoot.Unlock();
    base.Dispose(finalizing);
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(originalRoot.Document);
    if(cleared) throw new InvalidOperationException(ChangeAlreadyAppliedError);
    if(originalRoot.Locked || Document.Root != originalRoot) throw new InvalidOperationException(StateChangedError);

    Document.InternalClear();
    originalRoot.Lock();
    cleared = true;
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(originalRoot.Document);
    if(!cleared) throw new InvalidOperationException(ChangeNotAppliedError);
    if(!originalRoot.Locked || Document.Root.Children.Count != 0)
    {
      throw new InvalidOperationException(StateChangedError); // we expect that the document should be empty...
    }

    originalRoot.Unlock();
    Document.InternalSetRoot(originalRoot);
    cleared = false;
  }

  readonly RootNode originalRoot;
  bool cleared;
}
#endregion

#region Text changes
#region DeleteTextChange
/// <summary>Represents a change that deletes text from a <see cref="TextNode"/>.</summary>
/// <remarks>You shouldn't have to use this change event directly. Instead, you can use the members of
/// <see cref="TextNode"/>, which generate the necessary <see cref="ChangeEvent"/> objects implicitly.
/// </remarks>
public class DeleteTextChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="DeleteTextChange"/> with the text node and text span.</summary>
  public DeleteTextChange(TextNode textNode, int index, int count) : base(textNode)
  {
    if(textNode.Locked) throw new ArgumentException("The node to set is locked.");
    if(index < 0 || count < 0 || index+count > textNode.TextLength) throw new ArgumentOutOfRangeException();

    this.textNode = textNode;
    this.index    = index;
    this.count    = count;
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    string text = this.deletedText != null ? this.deletedText : textNode.GetText(index, count);
    return "Delete \"" + text + "\"";
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(textNode.Document);
    if(deletedText != null) throw new InvalidOperationException(ChangeAlreadyAppliedError);

    deletedText = textNode.GetText(index, count); // make a backup of the deleted text
    textNode.InternalDelete(index, count);        // then go ahead and delete it
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(textNode.Document);
    if(deletedText == null) throw new InvalidOperationException(ChangeNotAppliedError);

    textNode.InternalInsert(index, deletedText); // restore the deleted text
    deletedText = null;                          // then remove our copy
  }

  readonly TextNode textNode;
  readonly int index, count;
  string deletedText;
}
#endregion

#region InsertTextChange
/// <summary>Represents a change that inserts text into a <see cref="TextNode"/>.</summary>
/// <remarks>You shouldn't have to use this change event directly. Instead, you can use the members of
/// <see cref="TextNode"/>, which generate the necessary <see cref="ChangeEvent"/> objects implicitly.
/// </remarks>
public class InsertTextChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="InsertTextChange"/> with the text node, the position, and the new text.</summary>
  public InsertTextChange(TextNode textNode, int index, string text) : base(textNode)
  {
    if(textNode.Locked) throw new ArgumentException("The node to set is locked.");
    if(index < 0 || index > textNode.TextLength) throw new ArgumentOutOfRangeException();
    if(text == null) throw new ArgumentNullException();

    this.textNode = textNode;
    this.index    = index;
    this.text     = text;
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    return "Insert \"" + text + "\"";
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(textNode.Document);
    if(inserted) throw new InvalidOperationException(ChangeAlreadyAppliedError);

    textNode.InternalInsert(index, text);
    inserted = true;
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(textNode.Document);
    if(!inserted) throw new InvalidOperationException(ChangeNotAppliedError);

    textNode.InternalDelete(index, text.Length);
    inserted = false;
  }

  readonly TextNode textNode;
  readonly string text;
  readonly int index;
  bool inserted;
}
#endregion

#region ReplaceTextChange
/// <summary>Represents a change that replaces text within a <see cref="TextNode"/>.</summary>
/// <remarks>You shouldn't have to use this change event directly. Instead, you can use the members of
/// <see cref="TextNode"/>, which generate the necessary <see cref="ChangeEvent"/> objects implicitly.
/// </remarks>
public class ReplaceTextChange : ChangeEvent
{
  /// <summary>Initializes a new <see cref="ReplaceTextChange"/> with the text node, the text span, and the new text.</summary>
  public ReplaceTextChange(TextNode textNode, int index, int count, string text) : base(textNode)
  {
    if(textNode.Locked) throw new ArgumentException("The node to set is locked.");
    if(index < 0 || count < 0 || index+count > textNode.TextLength) throw new ArgumentOutOfRangeException();
    if(text == null) throw new ArgumentNullException();

    this.textNode = textNode;
    this.index    = index;
    this.count    = count;
    this.newText  = text;
  }

  /// <include file="documentation.xml" path="/UI/Common/ToString/*"/>
  public override string ToString()
  {
    StringBuilder sb = new StringBuilder();
    string oldText = this.oldText != null ? this.oldText : textNode.GetText(index, count);
    sb.Append("Replace \"").Append(oldText).Append("\" with \"").Append(newText).Append('"');
    return sb.ToString();
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Do/*"/>
  protected internal override void Do()
  {
    ValidateVersionForDo(textNode.Document);
    if(oldText != null) throw new InvalidOperationException(ChangeAlreadyAppliedError);

    oldText = textNode.GetText(index, count); // make a copy of the old text
    textNode.InternalSetText(index, count, newText); // and set the new text
  }

  /// <include file="documentation.xml" path="/UI/ChangeEvent/Undo/*"/>
  protected internal override void Undo()
  {
    ValidateVersionForUndo(textNode.Document);
    if(oldText == null) throw new InvalidOperationException(ChangeNotAppliedError);

    textNode.InternalSetText(index, newText.Length, oldText); // put the old text back
    oldText = null;                                           // and erase our copy
  }

  readonly TextNode textNode;
  readonly string newText;
  readonly int index, count;
  string oldText;
}
#endregion
#endregion

} // namespace AdamMil.UI.RichDocument