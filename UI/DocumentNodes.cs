/*
AdamMil.UI is a library that provides useful user interface controls for the
.NET framework.

http://www.adammil.net/
Copyright (C) 2008-2013 Adam Milazzo

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
using System.Text;
using AdamMil.UI.TextEditing;

namespace AdamMil.UI.RichDocument
{

#region Layout
/// <summary>This enumeration represents non-inherited layout attributes associated with a <see cref="DocumentNode"/>.</summary>
[Flags]
public enum Layout
{
  /// <summary>The node is positioned horizontally adjacent to its inline siblings, and is capable of wrapping from
  /// one line onto the next, similar to the CSS attribute display:inline. An inline block cannot float and cannot
  /// clear other floating blocks. Only inline nodes can directly render content.
  /// </summary>
  Inline=0,
  /// <summary>The node normally is a rectangle that cannot wrap and occupies its own line unless it is floating.
  /// This is similar to the CSS attribute display:block.
  /// </summary>
  Block=0x1,
  /// <summary>The node is a left-aligned block node, but allows subsequent non-floating nodes to flow around it.
  /// This is similar to the CSS attribute float:left.
  /// </summary>
  FloatLeft=Block | 0x2,
  /// <summary>The node is a right-aligned block node, but allows subsequent non-floating nodes to flow around it.
  /// This is similar to the CSS attribute float:right.
  /// </summary>
  FloatRight=Block | 0x4,
  /// <summary>The node is a block node that will not flow around left-floating blocks. This is similar to the CSS
  /// attribute clear:left.
  /// </summary>
  ClearLeft=Block | 0x8,
  /// <summary>The node is a block node that will not flow around right-floating blocks. This is similar to the CSS
  /// attribute clear:right.
  /// </summary>
  ClearRight=Block | 0x10,
}
#endregion

#region DocumentNode
/// <summary>Represents a rectangular section of content within a document.</summary>
public class DocumentNode
{
  /// <summary>Initializes a new <see cref="DocumentNode"/>.</summary>
  public DocumentNode() : this(true) { }

  /// <summary>Initializes a new <see cref="DocumentNode"/>.</summary>
  protected DocumentNode(bool allowChildren)
  {
    this.nodes = allowChildren ? new NodeCollection(this) : ReadOnlyNodeCollection;
    this.style = new Style(this);
  }

  #region NodeCollection
  /// <summary>A collection of <see cref="DocumentNode"/> associated with a parent node.</summary>
  public sealed class NodeCollection : System.Collections.ObjectModel.Collection<DocumentNode>
  {
    internal NodeCollection(DocumentNode owner)
    {
      this.owner = owner;
    }

    /// <summary>Creates and executes a <see cref="ClearNodeChange"/> event.</summary>
    protected override void ClearItems()
    {
      AssertNotReadOnly();
      owner.DoChange(new ClearNodeChange(owner));
    }

    /// <summary>Creates and executes an <see cref="InsertNodeChange"/> event.</summary>
    protected override void InsertItem(int index, DocumentNode item)
    {
      AssertNotReadOnly();
      ValidateNewItem(item);
      owner.DoChange(new InsertNodeChange(owner, index, item));
    }

    /// <summary>Creates and executes a <see cref="ReplaceNodeChange"/> event.</summary>
    protected override void SetItem(int index, DocumentNode item)
    {
      AssertNotReadOnly();
      ValidateNewItem(item);
      owner.DoChange(new ReplaceNodeChange(owner, index, item));
    }

    /// <summary>Creates and executes a <see cref="RemoveNodeChange"/> event.</summary>
    protected override void RemoveItem(int index)
    {
      AssertNotReadOnly();
      owner.DoChange(new RemoveNodeChange(owner, index));
    }

    /// <summary>Actually performs the work of clearing items from the collection.</summary>
    internal void InternalClearItems()
    {
      foreach(DocumentNode node in this) node.OnRemoved();
      base.ClearItems();
      owner.OnNodeChanged();
    }

    /// <summary>Actually performs the work of inserting an item into the collection.</summary>
    internal void InternalInsertItem(int index, DocumentNode item)
    {
      // fix up the indices of the items after the position where the new one is being inserted
      for(int i=index; i<Count; i++) this[i].index++;

      base.InsertItem(index, item);
      item.OnAdded(owner, index);
      owner.OnNodeChanged();
    }

    /// <summary>Actually performs the work of replacing an item in the collection.</summary>
    internal void InternalSetItem(int index, DocumentNode item)
    {
      this[index].OnRemoved();
      base.SetItem(index, item);
      item.OnAdded(owner, index);
      owner.OnNodeChanged();
    }

    /// <summary>Actually performs the work of removing an item from the collection.</summary>
    internal void InternalRemoveItem(int index)
    {
      // fix up the indices of the items after the one being removed
      for(int i=index+1; i<Count; i++) this[i].index--;

      this[index].OnRemoved();
      base.RemoveItem(index);
      owner.OnNodeChanged();
    }

    /// <summary>Throws an exception if the collection is read only.</summary>
    void AssertNotReadOnly()
    {
      if(owner == null)
      {
        throw new InvalidOperationException("This node does not allow children, so its Children collection "+
                                            "cannot be modified.");
      }
    }

    /// <summary>Ensures that the given node is not already part of another node or document.</summary>
    /// <param name="node">The node to check.</param>
    void ValidateNewItem(DocumentNode node)
    {
      if(node == null) throw new ArgumentNullException("node item");
      if(node.Parent != null) throw new ArgumentException("This node already has a parent.");

      if(node.Document != null && node.Document != owner.Document)
      {
        throw new ArgumentException("This node belongs to another document.");
      }
    }

    /// <summary>The document node that owns this collection, or null if this collection is unowned and read only.</summary>
    readonly DocumentNode owner;
  }
  #endregion

  /// <summary>Gets the collection containing this node's child nodes.</summary>
  public NodeCollection Children
  {
    get { return nodes; }
  }

  /// <summary>Gets the document to which this node belongs, or null if it does not belong to a document.</summary>
  public Document Document
  {
    get { return document; }
  }

  /// <summary>Gets the node's index within its parent node.</summary>
  public int Index
  {
    get
    {
      if(Parent == null) throw new InvalidOperationException("This node does not have a parent.");
      return index;
    }
  }

  /// <summary>Gets or sets the way the node should be laid out.</summary>
  public Layout Layout
  {
    get { return layout; }
    set
    {
      if(value != Layout)
      {
        layout = value;
        OnNodeChanged();
      }
    }
  }

  /// <summary>Gets this node's parent, or null if it has no parent.</summary>
  public DocumentNode Parent
  {
    get { return parent; }
  }

  /// <summary>Gets or sets the style of this node and its descendants.</summary>
  public Style Style
  {
    get { return style; }
    set
    {
      if(value != style)
      {
        if(value == null) throw new ArgumentNullException();
        if(value.owner != this) throw new ArgumentException("This style belongs to another document node.");
        style = value;
        OnNodeChanged();
      }
    }
  }

  /// <summary>Concatenates and returns the text of all text nodes in the node's descendants.</summary>
  public string InnerText
  {
    get
    {
      StringBuilder sb = new StringBuilder();
      foreach(DocumentNode node in EnumerateDescendants())
      {
        TextNode textNode = node as TextNode;
        if(textNode != null) sb.Append(textNode.GetText());
      }
      return sb.ToString();
    }
  }

  /// <summary>Enumerates all descendant nodes.</summary>
  public IEnumerable<DocumentNode> EnumerateDescendants()
  {
    foreach(DocumentNode child in Children)
    {
      yield return child;
      foreach(DocumentNode grandchild in child.EnumerateDescendants()) yield return grandchild;
    }
  }

  /// <include file="documentation.xml" path="/UI/DocumentNode/GetDescription/*"/>
  public virtual string GetDescription()
  {
    return "a node";
  }

  /// <summary>Adds the given change to this node's document if it has one, or simply executes the change if it
  /// doesn't.
  /// </summary>
  protected void DoChange(ChangeEvent change)
  {
    if(change == null) throw new ArgumentNullException();

    if(locked)
    {
      change.Dispose();
      throw new InvalidOperationException("This node is currently locked by the undo/redo system and "+
                                          "cannot be modified.");
    }
    else if(Document != null) // if this document node belongs to a document, add it to the document
    {
      Document.AddChangeEvent(change);
    }
    else // otherwise, just execute the change and dispose of it
    {
      change.Do();
      change.Dispose();
    }
  }

  /// <include file="documentation.xml" path="/UI/DocumentNode/OnNodeChanged/*"/>
  protected internal virtual void OnNodeChanged()
  {
    if(Document != null) Document.OnNodeChanged(this);
  }

  /// <summary>Called when a this node has been added to or removed from a document.</summary>
  /// <remarks>This method does not get called when a new node is being initialized with a new document (by passing a
  /// document to the constructor). Subclasses wanting to do work at that time should do it in their constructors.
  /// </remarks>
  protected virtual void OnDocumentAssigned() { }

  /// <summary>Called when this node is added to a parent node at the given index.</summary>
  protected virtual void OnAdded(DocumentNode parent, int index)
  {
    this.parent = parent;
    this.index  = index;
    RecursivelySetDocument(parent.Document);
  }

  /// <summary>Called when this node is removed from its parent.</summary>
  protected virtual void OnRemoved()
  {
    this.parent = null;
    this.index  = -1;
    RecursivelySetDocument(null);
  }

  /// <summary>Gets whether the document node is currently locked.</summary>
  internal bool Locked
  {
    get { return locked; }
  }

  /// <summary>Locks the document node so that it cannot be modified. This is called by the undo/redo system to
  /// prevent changes to the document node while it's been removed from the document but is still referenced by the
  /// undo system.
  /// </summary>
  internal void Lock()
  {
    if(locked) throw new InvalidOperationException("The document node is already locked.");
    RecursivelySetLock(true);
  }

  /// <summary>Unlocks the document node so that it can be modified again.
  /// <seealso cref="Lock"/>
  /// </summary>
  internal void Unlock()
  {
    if(!locked) throw new InvalidOperationException("The document node is not locked.");
    RecursivelySetLock(false);
  }

  /// <summary>Sets the <see cref="Document"/> property of this node and all descendant nodes to the given value.</summary>
  internal void RecursivelySetDocument(Document document)
  {
    if(this.document != document)
    {
      this.document = document;
      foreach(DocumentNode child in nodes) child.RecursivelySetDocument(document);
      OnDocumentAssigned();
    }
  }

  /// <summary>Sets the <see cref="locked"/> field of this node and all descendant nodes to the given value.</summary>
  void RecursivelySetLock(bool locked)
  {
    this.locked = locked;
    foreach(DocumentNode child in nodes) child.RecursivelySetLock(locked);
  }

  /// <summary>The children of this node.</summary>
  NodeCollection nodes;
  /// <summary>This node's parent node, or null if this is the root.</summary>
  DocumentNode parent;
  /// <summary>The document to which this node belongs.</summary>
  Document document;
  /// <summary>The style of this node and its descendants.</summary>
  Style style;
  /// <summary>The node's index within its parent node.</summary>
  int index = -1;
  /// <summary>The node's layout style.</summary>
  Layout layout;
  /// <summary>Whether the document node is locked and cannot be modified.</summary>
  bool locked;

  static readonly NodeCollection ReadOnlyNodeCollection = new NodeCollection(null);
}
#endregion

#region RootNode
/// <summary>Represents the root node of a document.</summary>
internal class RootNode : DocumentNode
{
  /// <summary>Initializes the root node with its containing document.</summary>
  /// <param name="document">The document containing the root node.</param>
  public RootNode(Document document) : base(true)
  {
    if(document == null) throw new ArgumentNullException("document");
    Layout = Layout.Block;
    RecursivelySetDocument(document);
  }

  public override string GetDescription()
  {
    return "document"; // this node represents the entire document
  }
}
#endregion

#region LinkNode
/// <summary>Represents a link to another resource. The child nodes, if any, are the representation of the link.
/// For instance, a <see cref="TextNode"/> with a friendly name. If there are no children, a default representation
/// should be chosen by the renderer implementation.
/// </summary>
public class LinkNode : DocumentNode
{
  /// <summary>Initializes the <see cref="LinkNode"/> with the given link and the default representation.</summary>
  public LinkNode(Uri linkUri) : this(linkUri, null) { }

  /// <summary>Initializes the <see cref="LinkNode"/> with the given link and a <see cref="TextNode"/> containing the
  /// given text as the representation.
  /// </summary>
  public LinkNode(Uri linkUri, string linkText) : base(true)
  {
    if(linkUri == null) throw new ArgumentNullException();
    if(!string.IsNullOrEmpty(linkText)) Children.Add(new TextNode(linkText));
  }

  /// <summary>This event is raised when the link is activated.</summary>
  public DocumentNodeEvent Activated;

  /// <summary>Gets or sets the link destination.</summary>
  public Uri Link
  {
    get { return link; }
    set
    {
      if(value == null) throw new ArgumentNullException();
      if(!value.Equals(link))
      {
        link = value;
        OnNodeChanged();
      }
    }
  }

  /// <summary>Activates the link.</summary>
  public void Activate()
  {
    OnActivated();
  }

  /// <include file="documentation.xml" path="/UI/DocumentNode/GetDescription/*"/>
  public override string GetDescription()
  {
    return Link.AbsoluteUri;
  }

  /// <summary>Called when the link is activated. The base implementation raises the <see cref="Activated"/> event.</summary>
  protected virtual void OnActivated()
  {
    if(Activated != null) Activated(Document, this);
  }

  Uri link;
}
#endregion

#region TextNode
/// <summary>Represents an editable span of text within the document. A <see cref="TextNode"/> cannot have children.</summary>
public class TextNode : DocumentNode
{
  /// <summary>Initializes the <see cref="TextNode"/> with an empty string of text.</summary>
  public TextNode() : this((TextDocument)null) { }
  /// <summary>Initializes the <see cref="TextNode"/> with the given string of text.</summary>
  public TextNode(string initialText) : this(new System.IO.StringReader(initialText)) { }
  /// <summary>Initializes the <see cref="TextNode"/> with text from the given
  /// <see cref="System.IO.TextReader"/>.
  /// </summary>
  public TextNode(System.IO.TextReader reader) : this(new TextDocument(reader)) { }
  /// <summary>Initializes the <see cref="TextNode"/> with the given initial document.</summary>
  public TextNode(TextDocument initialDocument) : base(false)
  {
    textDocument = initialDocument != null ? initialDocument : new TextDocument();
  }

  /// <include file="documentation.xml" path="/UI/TextDocument/LineCount/*"/>
  public int LineCount
  {
    get { return textDocument.LineCount; }
  }

  /// <summary>Gets the length of the text within the node.</summary>
  public int TextLength
  {
    get { return textDocument.Length; }
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Copy3/*"/>
  public void CopyText(System.IO.TextWriter writer, int srcIndex, int count)
  {
    textDocument.CopyTo(writer, srcIndex, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Copy4/*"/>
  public void CopyText(int srcIndex, char[] destArray, int destIndex, int count)
  {
    textDocument.CopyTo(srcIndex, destArray, destIndex, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Delete2/*"/>
  public void Delete(int index, int count)
  {
    DoChange(new DeleteTextChange(this, index, count));
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/Indexer/*"/>
  public char GetCharacter(int index)
  {
    return textDocument[index];
  }

  /// <include file="documentation.xml" path="/UI/DocumentNode/GetDescription/*"/>
  public override string GetDescription()
  {
    return "\"" + textDocument.GetText() + "\"";
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLength/*"/>
  public int GetLineLength(int line)
  {
    return textDocument.GetLineLength(line);
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineLengths/*"/>
  public int[] GetLineLengths()
  {
    return textDocument.GetLineLengths();
  }

  /// <include file="documentation.xml" path="/UI/LineStorage/GetLineInfo/*"/>
  public void GetLineInfo(int line, out int offset, out int length)
  {
    textDocument.GetLineInfo(line, out offset, out length);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/GetText/*"/>
  public string GetText()
  {
    return textDocument.GetText();
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/GetText2/*"/>
  public string GetText(int index, int count)
  {
    return textDocument.GetText(index, count);
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertChar/*"/>
  public void Insert(int index, char c)
  {
    Insert(index, new string(c, 1));
  }

  /// <include file="documentation.xml" path="/UI/EditableTextBuffer/InsertString/*"/>
  public void Insert(int index, string text)
  {
    DoChange(new InsertTextChange(this, index, text));
  }

  /// <summary>Deletes the given text and notifies the document that the text has been changed.</summary>
  internal void InternalDelete(int index, int count)
  {
    textDocument.Delete(index, count);
    OnNodeChanged();
  }

  /// <summary>Inserts the given text and notifies the document that the text has been changed.</summary>
  internal void InternalInsert(int index, string text)
  {
    textDocument.Insert(index, text);
    OnNodeChanged();
  }

  /// <summary>Sets the node text and notifies the document that the text has been changed.</summary>
  internal void InternalSetText(int index, int count, string text)
  {
    if(text == null) throw new ArgumentNullException();
    textDocument.Delete(index, count);
    textDocument.Insert(index, text);
    OnNodeChanged();
  }

  readonly TextDocument textDocument;
}
#endregion

} // namespace AdamMil.UI.RichDocument