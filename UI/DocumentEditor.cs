/*
AdamMil.UI is a library that provides useful user interface controls for the
.NET framework.

http://www.adammil.net/
Copyright (C) 2008 Adam Milazzo

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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AdamMil.UI.RichDocument
{

/// <summary>Implements a renderer and editor for rich documents, represented by <see cref="Document"/> objects.</summary>
/// <remarks>The document editor maintains the concept of an index within the document, which is a position at which
/// the text cursor can be placed. Each editable portion of each document node has an index assigned to it. For
/// instance, a text node will have one index for each character that it contains, but an image node simply has one
/// index. The <see cref="CursorIndex"/>, <see cref="IndexLength"/>, <see cref="Selection"/>, etc members deal with
/// index units. The document editor also maintains a layout of how the document is rendered onto the canvas, allowing
/// conversion between pixel coordinates, indices, and document nodes.
/// </remarks>
public class DocumentEditor : Control
{
  /// <summary>Initializes this <see cref="DocumentEditor"/> with a new, empty document.</summary>
  public DocumentEditor() : this(new Document()) { }

  /// <summary>Initializes this <see cref="DocumentEditor"/> with the given document.</summary>
  public DocumentEditor(Document document)
  {
    if(document == null) throw new ArgumentNullException();

    this.document = document;
    document.NodeChanged += OnNodeChanged;

    SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.ResizeRedraw |
             ControlStyles.Selectable | ControlStyles.StandardClick | ControlStyles.StandardDoubleClick |
             ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

    SetStyle(ControlStyles.ContainerControl | ControlStyles.SupportsTransparentBackColor, false);

    BackColor = SystemColors.Window;
    ForeColor = SystemColors.WindowText;

    controlWidth = Width;

    ScrollBars = ScrollBars.Vertical; // create the vertical scrollbar immediately so we don't
  }                                   // have to redo the initial layout if the document is long

  /// <summary>Gets or sets the border style of the control. The default is
  /// <see cref="System.Windows.Forms.BorderStyle.Fixed3D"/>.
  /// </summary>
  [Category("Appearance")]
  [DefaultValue(BorderStyle.Fixed3D)]
  [Description("Determines how the border of the control will be drawn.")]
  public BorderStyle BorderStyle
  {
    get { return borderStyle; }
    set
    {
      if(value != BorderStyle)
      {
        int oldWidth = BorderWidth;
        borderStyle = value;

        // if the border thickness changes, the canvas size changes, which can affect the layout.
        if(BorderWidth != oldWidth) InvalidateLayout();
        Invalidate(); // in any case, we need to repaint the control
      }
    }
  }

  /// <summary>Gets the rectangle, in client coordinates, into which the document will be rendered.</summary>
  [Browsable(false)]
  public Rectangle CanvasRectangle
  {
    get
    {
      Rectangle rect = this.ClientRectangle; // start with the full client area
      int offset = -BorderWidth; // then remove the space used by the border
      rect.Inflate(offset, offset);
      if(hScrollBar != null) rect.Height -= hScrollBar.Height; // account for the horizontal scroll bar
      if(vScrollBar != null) rect.Width -= vScrollBar.Width; // account for the vertical scroll bar
      return rect;
    }
  }

  /// <summary>Gets or sets the index at which the text cursor is displayed, from 0 to <see cref="IndexLength"/>
  /// inclusive.
  /// </summary>
  [Browsable(false)]
  public int CursorIndex
  {
    get { return cursorIndex; }
    set
    {
      if(value < 0 || value > IndexLength) throw new ArgumentOutOfRangeException();
      cursorIndex = value;
    }
  }

  /// <summary>Gets the <see cref="Document"/> being rendered by this control.</summary>
  [Browsable(false)]
  public Document Document
  {
    get { return document; }
  }

  /// <summary>Gets the size of the document, in pixels.</summary>
  [Browsable(false)]
  public Size DocumentArea
  {
    get
    {
      Layout();
      return rootBlock.Size;
    }
  }

  /// <summary>Gets the length of the document, in index units.</summary>
  [Browsable(false)]
  public int IndexLength
  {
    get
    {
      Layout();
      return rootBlock.Length;
    }
  }

  /// <summary>Gets or sets whether editing capabilities are disabled.</summary>
  /// <remarks>This does not prevent programmatic editing of the document -- only editing using the user interface.</remarks>
  [Category("Behavior")]
  [DefaultValue(false)]
  [Description("If true, the document cannot be edited by the user.")]
  public bool ReadOnly
  {
    get { return readOnly; }
    set { readOnly = value; }
  }

  /// <summary>Gets the width and height of the area that can be scrolled into.</summary>
  public Size ScrollArea
  {
    get { return new Size(hScrollBar == null ? 0 : hScrollBar.Maximum, vScrollBar == null ? 0 : vScrollBar.Maximum); }
  }

  /// <summary>Gets or sets the position within the document that is being rendered at the top-left pixel of the
  /// control's render area.
  /// </summary>
  [Browsable(false)]
  public Point ScrollPosition
  {
    get
    {
      return new Point(hScrollPos, vScrollPos);
    }
    set
    {
      Layout();
      
      if(value.X < 0 || value.Y < 0 ||
         value.X > (hScrollBar == null ? 0 : hScrollBar.Maximum) ||
         value.Y > (vScrollBar == null ? 0 : vScrollBar.Maximum))
      {
        throw new ArgumentOutOfRangeException();
      }

      if(hScrollBar != null) hScrollBar.Value = value.X;
      if(vScrollBar != null) vScrollBar.Value = value.Y;
    }
  }

  /// <summary>Gets or sets the scrollbars which are always visible. In addition to these, scrollbars will be added
  /// as needed. The default is <see cref="System.Windows.Forms.ScrollBars.Vertical"/>.
  /// </summary>
  [Category("Appearance")]
  [DefaultValue(ScrollBars.Vertical)]
  [Description("Determines which scrollbars are always visible. In addition to these, scrollbars will be added as "+
    "needed.")]
  public ScrollBars ScrollBars
  {
    get { return scrollBars; }
    set
    {
      if(value != ScrollBars)
      {
        scrollBars = value; // since we changed the set of forced scrollbars, some scrollbars may be unnecessary

        // immediately remove any unnecessary scrollbars
        HScrollBar oldHbar = hScrollBar;
        if(CreateOrDestroyScrollbars()) // if that would affect the layout, invalidate it
        {
          InvalidateLayout();
        }
        else if(HasLayout)
        {
          using(Graphics gdi = Graphics.FromHwnd(Handle)) ResizeScrollbars(gdi);
        }
      }
    }
  }

  /// <summary>Gets or sets the span of the selection within the document.</summary>
  [Browsable(false)]
  public Span Selection
  {
    get { return selection; }
    set
    {
      if(value != selection)
      {
        if(value.End > IndexLength) throw new ArgumentOutOfRangeException();

        // get the bounding boxes of the previous and new selection
        Rectangle oldArea = GetVisibleArea(selection), newArea = GetVisibleArea(value);

        // set the new value
        selection = value;
        
        // and invalidate the affected areas of the control, if any
        if(oldArea.Width != 0) Invalidate(oldArea);
        if(newArea.Width != 0) Invalidate(newArea);
      }
    }
  }

  /// <summary>Gets or sets the text content of the document.</summary>
  /// <remarks>This property is included to follow Windows.Forms conventions. It is a very inefficient way to modify
  /// the content of the document, and will destroy all formatting, layout, and non-text nodes if set.
  /// </remarks>
  [Category("Appearance")]
  [Description("Sets the initial text of the document.")]
  public override string Text
  {
    get { return document.Root.InnerText; }
    set
    {
      if(!string.Equals(Text, value, StringComparison.Ordinal))
      {
        document.AddChangeEvent(new CompositeChange("Replace document text",
          new ClearNodeChange(document.Root),
          new InsertNodeChange(document.Root, 0, new TextNode(value))));
        OnTextChanged(EventArgs.Empty); // TODO: make this fire when a TextNode's text is changed, too
      }
    }
  }

  /// <summary>Clears the content of the document.</summary>
  public void Clear()
  {
    document.AddChangeEvent(new ClearNodeChange(document.Root));
  }

  /// <summary>Copies the content of the selection to the clipboard, if there is a selection.</summary>
  public void Copy()
  {
    Copy(Selection);
  }

  /// <summary>Copies the content of the given span to the clipboard, if the span is not zero-length.</summary>
  public void Copy(Span span)
  {
    Clipboard.SetDataObject(CreateClipboardDataObject(span), true, 3, 5);
  }

  /// <summary>Cuts the content of the selection from the document and places it on the clipboard, if there is a
  /// selection.
  /// </summary>
  public void Cut()
  {
    Cut(Selection);
  }

  /// <summary>Cuts the content of the given span from the document and places it on the clipboard, if the span is not
  /// zero-length.
  /// </summary>
  public void Cut(Span span)
  {
    Clipboard.SetDataObject(CreateClipboardDataObject(span), true, 3, 5);
    Delete(span);
  }

  /// <summary>Deletes the content of the current selection, if there is one.</summary>
  public void Delete()
  {
    Delete(Selection);
  }

  /// <summary>Deletes the content of the given span from the document.</summary>
  public void Delete(Span span)
  {
    Document.AddChangeEvent(CreateDeleteChange(span));
  }

  /// <summary>Clears the selection.</summary>
  public void DeselectAll()
  {
    Selection = new Span();
  }

  /// <summary>Returns an array containing the formats that the data on the clipboard can be converted to. The array
  /// will be empty if there is no data on the clipboard that can be inserted.
  /// </summary>
  /// <remarks>The returned format strings cannot be used directly with the <see cref="Clipboard"/> class. Rather, they
  /// come from the <see cref="ClipboardDataFormats"/> class, and any custom types implemented by a derived class.
  /// </remarks>
  public string[] GetPasteFormats()
  {
    List<string> formats = new List<string>();
    AddPasteFormats(formats);
    return formats.ToArray();
  }

  /// <summary>Inserts the given document nodes into the document at the given index.</summary>
  public void Insert(int index, params DocumentNode[] nodes)
  {
    if(nodes == null) throw new ArgumentNullException();
    if(nodes.Length != 0) Document.AddChangeEvent(CreateInsertChange(index, nodes));
  }

  /// <summary>Inserts the given text into the document at the given index.</summary>
  public void Insert(int index, string text)
  {
    Insert(index, new TextNode(text));
  }

  /// <summary>Forces the layout of the document to happen immediately, if it is not already complete. This will
  /// lay out the document even if layout has been suspended with <see cref="SuspendLayout"/>.
  /// </summary>
  public new void Layout()
  {
    if(!HasLayout)
    {
      using(Graphics gdi = Graphics.FromHwnd(Handle))
      {
        EnsureLayout(gdi);
      }
    }
  }

  /// <summary>Pastes the content of the clipboard over the current selection, or at the text cursor position if
  /// there's no selection, or at the end of the document if there's no text cursor.
  /// </summary>
  public void Paste()
  {
    Span span = Selection;
    if(span.Length == 0) span.Start = CursorIndex == -1 ? IndexLength : CursorIndex;
    Paste(span);
  }

  /// <summary>Pastes the content of the clipboard over the content at the given span within the document.</summary>
  public void Paste(Span span)
  {
    Paste(span, null);
  }

  /// <summary>Converts the content of the clipboard to the given format, and then pastes it over the content at the 
  /// given span within the document.
  /// </summary>
  /// <param name="span">The span within the document that will be overwritten with the clipboard data.</param>
  /// <param name="clipboardFormat">The type of clipboard data to use. This should be one of the values returned from
  /// <see cref="GetPasteFormats"/>, or null to automatically select the best format.
  /// </param>
  public void Paste(Span span, string clipboardFormat)
  {
    ValidateSpan(span);

    if(clipboardFormat == null)
    {
      clipboardFormat = SelectBestClipboardFormat(GetPasteFormats());
      if(clipboardFormat == null) return;
    }

    DocumentNode[] newNodes = ConvertClipboardDataToNodes(clipboardFormat);
    if(newNodes == null || newNodes.Length == 0) return;

    List<ChangeEvent> events = new List<ChangeEvent>();
    if(span.Length != 0) events.Add(CreateDeleteChange(span));
    events.Add(CreateInsertChange(span.Start, newNodes));
    Document.AddChangeEvent(new CompositeChange("Paste", events.ToArray()));
  }

  /// <summary>Redoes the next change to the document.</summary>
  public void Redo()
  {
    Document.Redo();
  }

  /// <summary>Resumes layout and painting after <see cref="SuspendLayout"/> has been called.</summary>
  public new void ResumeLayout()
  {
    layoutSuspended = false;

    if(repaintNeeded)
    {
      repaintNeeded = false;
      Invalidate();
    }
  }

  /// <summary>Scrolls until the given index within the document is visible.</summary>
  public void ScrollTo(int index)
  {
    ScrollTo(index, false);
  }

  /// <summary>Scrolls until the given offset within the document is visible.</summary>
  /// <param name="index">The index of the location to scroll to.</param>
  /// <param name="placeAtTop">If true, the document will be scrolled so that the location is rendered in the
  /// top of the control (if possible). If false, the document will be only scrolled if the location is not already
  /// onscreen, and it may not be scrolled to the top.
  /// </param>
  public void ScrollTo(int index, bool placeAtTop)
  {
    Layout();

    if(vScrollBar != null || hScrollBar != null)
    {
      LayoutRegion region = GetRegion(index);
      Rectangle canvasRect = CanvasRectangle;

      if(placeAtTop)
      {
        // place the region at the top of the canvas
        if(vScrollBar != null && region.AbsoluteTop  != vScrollBar.Value) vScrollBar.Value = region.AbsoluteTop;
        // and, if it doesn't fit horizontally, scroll it to the left of the canvas
        if(hScrollBar != null && region.AbsoluteRight > canvasRect.Right) hScrollBar.Value = region.AbsoluteLeft;
      }
      else
      {
        if(vScrollBar != null)
        {
          if(region.AbsoluteTop < vScrollBar.Value) // if the region is off the top, scroll it downward
          {
            vScrollBar.Value = region.AbsoluteTop;
          }
          else if(region.AbsoluteBottom > vScrollBar.Value+canvasRect.Height) // if the region is off the bottom
          {
            // if the region is too large to fit, then scroll to the top of it. otherwise scroll until the bottom is
            vScrollBar.Value = region.Height > canvasRect.Height ?  // flush with the bottom of the canvas
              region.AbsoluteTop : region.AbsoluteBottom - canvasRect.Height;
          }
        }

        if(hScrollBar != null)
        {
          if(region.AbsoluteLeft < hScrollBar.Value) // if the region is off the left, scroll it to the right
          {
            hScrollBar.Value = region.AbsoluteLeft;
          }
          else if(region.AbsoluteRight > hScrollBar.Value+canvasRect.Width) // if the region is off the right
          {
            // if the region is too large to fit, then scroll to the left of it. otherwise scroll until the right is
            hScrollBar.Value = region.Width > canvasRect.Width ?  // flush with the right of the canvas
              region.AbsoluteLeft : region.AbsoluteRight - canvasRect.Width;
          }
        }
      }
    }
  }

  /// <summary>Scrolls until the given point within the document is visible.</summary>
  /// <param name="node">The document node to scroll to.</param>
  /// <param name="placeAtTop">If true, the document will be scrolled so that the location is rendered in the
  /// top of the control (if possible). If false, the document will be only scrolled if the location is not already
  /// onscreen, and it may not be scrolled to the top.
  /// </param>
  public void ScrollTo(DocumentNode node, bool placeAtTop)
  {
    ScrollTo(GetIndexSpan(node).Start, placeAtTop);
  }

  /// <summary>Selects the entire document.</summary>
  public void SelectAll()
  {
    Selection = new Span(0, IndexLength);
  }

  /// <summary>Suspends layout and rendering of the document until <see cref="ResumeLayout"/> is called.</summary>
  public new void SuspendLayout()
  {
    layoutSuspended = true;
  }

  /// <summary>Undoes the last change to the document.</summary>
  public void Undo()
  {
    Document.Undo();
  }

  #region Layout classes and methods
  #region Region
  /// <summary>Represents a rectangular area within the document layout into which a span of the document indices are
  /// rendered.
  /// </summary>
  protected abstract class LayoutRegion
  {
    /// <summary>Gets the absolute horizontal position of Gets the leftmost pixel of Gets the region.</summary>
    public int AbsoluteLeft
    {
      get { return AbsolutePosition.X; }
    }

    /// <summary>Gets the absolute vertical position of Gets the topmost pixel of Gets the region.</summary>
    public int AbsoluteTop
    {
      get { return AbsolutePosition.Y; }
    }

    /// <summary>Gets the absolute horizontal position of Gets the pixel just to Gets the right of Gets the region.</summary>
    public int AbsoluteRight
    {
      get { return AbsolutePosition.X+Width; }
    }

    /// <summary>Gets the absolute vertical position of Gets the pixel just below Gets the region.</summary>
    public int AbsoluteBottom
    {
      get { return AbsolutePosition.Y+Height; }
    }

    /// <summary>Gets the absolute area of the region within the layout.</summary>
    public Rectangle AbsoluteBounds
    {
      get { return new Rectangle(AbsolutePosition, Size); }
    }

    /// <summary>Gets the position of the region relative to the parent region.</summary>
    public Point Position
    {
      get { return Bounds.Location; }
      set { Bounds.Location = value; }
    }

    /// <summary>Gets the size of the region, in pixels.</summary>
    public Size Size
    {
      get { return Bounds.Size; }
      set { Bounds.Size = value; }
    }

    /// <summary>Gets or sets the horizontal position of the leftmost pixel of the region, relative to the parent region.</summary>
    public int Left
    {
      get { return Bounds.X; }
      set { Bounds.X = value; }
    }

    /// <summary>Gets or sets the vertical position of the topmost pixel of the region, relative to the parent region.</summary>
    public int Top
    {
      get { return Bounds.Y; }
      set { Bounds.Y = value; }
    }

    /// <summary>Gets or sets the width of the region, in pixels.</summary>
    public int Width
    {
      get { return Bounds.Width; }
      set { Bounds.Width = value; }
    }

    /// <summary>Gets or sets the height of the region, in pixels.</summary>
    public int Height
    {
      get { return Bounds.Height; }
      set { Bounds.Height = value; }
    }

    /// <summary>Gets or sets the start of the span within the document node that is rendered in this region.</summary>
    public int Start
    {
      get { return Span.Start; }
      set { Span.Start = value; }
    }

    /// <summary>Gets or sets the end of the span within the document node that is rendered in this region.</summary>
    public int End
    {
      get { return Span.End; }
    }

    /// <summary>Gets or sets the length of the span within the document node that is rendered in this region.</summary>
    public int Length
    {
      get { return Span.Length; }
      set { Span.Length = value; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public abstract LayoutRegion[] Children
    {
      get;
    }

    /// <summary>The region's bounds, relative to the parent region.</summary>
    public Rectangle Bounds;
    /// <summary>The region's absolute position in the document.</summary>
    public Point AbsolutePosition;
    /// <summary>The span of indices that this region contains.</summary>
    public Span Span;
  }
  #endregion

  #region BlockBase
  /// <summary>Provides a base class for block regions, whose children are stacked vertically.</summary>
  protected abstract class BlockBase : LayoutRegion
  {
    internal abstract void Render(Graphics gdi, ref Rectangle area, ref RenderData data);
  }
  #endregion

  #region Block
  /// <summary>Represents a block that contains only other blocks.</summary>
  protected sealed class Block : BlockBase
  {
    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public override LayoutRegion[] Children
    {
      get { return Blocks; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public BlockBase[] Blocks;

    internal override void Render(Graphics gdi, ref Rectangle area, ref RenderData data)
    {
      foreach(BlockBase block in Blocks)
      {
        Rectangle childArea = new Rectangle(area.Left+block.Left, area.Top+block.Top, block.Width, block.Height);
        if(childArea.IntersectsWith(data.ClipRectangle))
        {
          block.Render(gdi, ref childArea, ref data);
        }                                                   
      }
    }
  }
  #endregion

  #region LineBlock
  /// <summary>Represents a block that contains only lines.</summary>
  protected sealed class LineBlock : BlockBase
  {
    /// <summary>Initializes this <see cref="LineBlock"/> with the given list of <see cref="Line"/> objects.</summary>
    public LineBlock(Line[] lines) { Lines = lines; }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public override LayoutRegion[] Children
    {
      get { return Lines; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public Line[] Lines;

    internal override void Render(Graphics gdi, ref Rectangle area, ref RenderData data)
    {
      foreach(Line line in Lines)
      {
        Rectangle childArea = new Rectangle(area.Left+line.Left, area.Top+line.Top, line.Width, line.Height);
        if(childArea.IntersectsWith(data.ClipRectangle))
        {
          line.Render(gdi, ref childArea, ref data);
        }
      }
    }
  }
  #endregion

  #region Line
  /// <summary>Represents a line region, whose children are stacked horizontally.</summary>
  protected sealed class Line : LayoutRegion
  {
    /// <summary>Initializes this <see cref="Line"/> with the given list of <see cref="LayoutSpan"/> objects.</summary>
    public Line(LayoutSpan[] spans) { Spans = spans; }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public override LayoutRegion[] Children
    {
      get { return Spans; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public LayoutSpan[] Spans;

    internal void Render(Graphics gdi, ref Rectangle area, ref RenderData data)
    {
      foreach(LayoutSpan span in Spans)
      {
        Rectangle childArea = new Rectangle(area.Left+span.Left, area.Top+span.Top, span.Width, span.Height);
        if(childArea.IntersectsWith(data.ClipRectangle)) span.Render(gdi, childArea.Location, data.Selection);
      }
    }
  }
  #endregion

  #region LayoutSpan
  /// <summary>Represents a portion of a <see cref="DocumentNode"/> that can be rendered without line wrapping. There
  /// may be multiple <see cref="LayoutSpan"/> regions referencing the same <see cref="DocumentNode"/> if the document
  /// node was wrapped onto multiple lines.
  /// </summary>
  protected abstract class LayoutSpan : LayoutRegion
  {
    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Children/*"/>
    public sealed override LayoutRegion[] Children
    {
      get { return NoChildren; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/*"/>
    public virtual int LineCount
    {
      get { return 1; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/*"/>
    public abstract LayoutSpan CreateNew();

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/GetNextSplitPiece/*"/>
    public abstract SplitPiece GetNextSplitPiece(Graphics gdi, int line, SplitPiece piece, int spaceLeft,
                                                 bool lineIsEmpty);

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/Render/*"/>
    public abstract void Render(Graphics gdi, Point clientPoint, Span selection);

    /// <summary>Gets or sets the number of pixels from the baseline to the bottom of the region.</summary>
    public int Descent;

    static readonly LayoutRegion[] NoChildren = new LayoutRegion[0];
  }
  #endregion

  #region LayoutSpan<T>
  /// <summary>Represents a <see cref="LayoutSpan"/> with a strongly-typed <see cref="DocumentNode"/>.</summary>
  protected abstract class LayoutSpan<NodeType> : LayoutSpan where NodeType : DocumentNode
  {
    /// <summary>Initializes the <see cref="LayoutSpan"/> with the given document node.</summary>
    protected LayoutSpan(NodeType node)
    {
      if(node == null) throw new ArgumentNullException();
      Node = node;
    }

    /// <summary>The <see cref="DocumentNode"/> associated with this <see cref="LayoutSpan"/>.</summary>
    protected readonly NodeType Node;
  }
  #endregion

  #region SplitPiece
  /// <summary>Represents a piece of content within a document node, and its size as it would be rendered.</summary>
  protected sealed class SplitPiece
  {
    /// <summary>Initializes this <see cref="SplitPiece"/> with the given index span and pixel size, and an indication
    /// of whether there is more content to be added, but a new line must be started before it will fit.
    /// </summary>
    public SplitPiece(Span span, Size size, bool newLine) : this(span, size, 0, newLine) { }

    /// <summary>Initializes this <see cref="SplitPiece"/> with the given index span and pixel size, a number of
    /// indices to skip after this content chunk, and an indication of whether there is more content to be added, but
    /// a new line must be started before it will fit.
    /// </summary>
    public SplitPiece(Span span, Size size, int skip, bool newLine)
    {
      if(size.Width < 0 || size.Height < 0 || skip < 0) throw new ArgumentOutOfRangeException();
      Span    = span;
      Size    = size;
      Skip    = skip;
      NewLine = newLine;
    }

    /// <summary>The span of the content within the document node, in index units.</summary>
    public Span Span;
    /// <summary>The size of the content in pixels, as it would be rendered.</summary>
    public Size Size;
    /// <summary>How many indices after the end of the span should be skipped before starting the next span.</summary>
    public int Skip;
    /// <summary>Whether a new line must be started to receive more of the content of this node.</summary>
    public bool NewLine;
  }
  #endregion

  #region TextNodeSpan
  /// <summary>Represents a <see cref="LayoutSpan"/> to render a <see cref="TextNode"/>.</summary>
  protected class TextNodeSpan : LayoutSpan<TextNode>
  {
    /// <summary>Initializes this <see cref="TextNodeSpan"/> given the owning <see cref="DocumentEditor"/>, a
    /// <see cref="TextNode"/> to render, and the starting index of the node within the document.
    /// </summary>
    public TextNodeSpan(TextNode node, DocumentEditor editor, int startIndex)
      : this(node, editor, startIndex, editor.GetEffectiveFont(node)) { }

    TextNodeSpan(TextNode node, DocumentEditor editor, int startIndex, Font font) : base(node)
    {
      Editor     = editor;
      StartIndex = startIndex;
      Font       = font;

      // calculate the font descent, so that adjacent spans of text with different fonts render on the same baseline
      Descent = Font.Height - (int)Math.Round(Font.Height * Font.FontFamily.GetCellAscent(Font.Style) /
                                              (float)Font.FontFamily.GetLineSpacing(Font.Style));
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/*"/>
    public override int LineCount
    {
      get { return Node.LineCount; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/*"/>
    public override LayoutSpan CreateNew()
    {
      return new TextNodeSpan(Node, Editor, StartIndex, Font);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/GetNextSplitPiece/*"/>
    public override SplitPiece GetNextSplitPiece(Graphics gdi, int line, SplitPiece piece, int spaceLeft,
                                                 bool lineIsEmpty)
    {
      const TextFormatFlags MeasureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping |
                                           TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

      int start = piece != null ? piece.Span.End + piece.Skip : 0, lineLength = Node.GetLineLength(line);
      int charactersLeft = lineLength - start, skipCharacters = 0;

      // store the text of the line in 'cachedText'
      if(piece == null || cachedText == null)
      {
        int offset;
        Node.GetLineInfo(line, out offset, out lineLength);
        cachedText = Node.GetText(offset, lineLength);
      }

      // ignore the newline character
      bool skippedNewLine = charactersLeft != 0 && cachedText[start+charactersLeft-1] == '\n';
      if(skippedNewLine) charactersLeft--;

      if(charactersLeft == 0) // if we're at the end of the line...
      {
        if(piece != null) // if we've already returned a piece, then we're done
        {
          cachedText = null; // clear the cached text
          return null;
        }
        else // otherwise, the line must be empty, so we need to return a piece that has some height just to ensure
        {    // that the containing line acquires some height
          return new SplitPiece(new Span(start, 0), new Size(0, Font.Height), (skippedNewLine ? 1 : 0), false);
        }
      }

      // measure and accumulate words until we've run out of either text or space
      Size size = new Size(), wordSize = new Size(), maxTextArea = new Size(int.MaxValue, int.MaxValue);
      Match match;
      int charactersFit = 0;
      for(match = wordRE.Match(cachedText, start, charactersLeft); match.Success; match = match.NextMatch())
      {
        wordSize = TextRenderer.MeasureText(gdi, match.Value, Font, maxTextArea, MeasureFlags);
        if(size.Width + wordSize.Width > spaceLeft) break; // if the word doesn't fit, then break out

        size.Width    += wordSize.Width;
        size.Height    = Math.Max(size.Height, wordSize.Height);
        charactersFit += match.Length;
      }

      bool lineWrapped = match.Success; // if the match is still valid, that means there was a word that didn't fit
      if(lineWrapped)
      {
        // if there was one big word and it didn't fit...
        if(charactersFit == 0 && !char.IsWhiteSpace(cachedText[start]))
        {
          if(!lineIsEmpty) // if the line is not empty, then we'll return an empty piece with NewLine set to true, so
          {                // we can try again with an empty line, which may have enough space
            return new SplitPiece(new Span(start, 0), new Size(), true);
          }
          else // otherwise, the line is empty, so starting a new line won't help. we need to add the word
          {    // even though it doesn't fit
            size.Width    += wordSize.Width;
            size.Height    = Math.Max(size.Height, wordSize.Height);
            charactersFit += match.Length;
          }
        }
        else // otherwise, we have some words that fit, but one that doesn't
        {
          // there may be some whitespace at the start of the word that does fit, however. so we'll go through it
          // character by character.
          for(int index=start+charactersFit, accumulatedWidth=0; index < lineLength; index++)
          {
            char c = cachedText[index];
            if(!char.IsWhiteSpace(c)) break;
            Size charSize = TextRenderer.MeasureText(gdi, new string(c, 1), Font, maxTextArea, MeasureFlags);
            accumulatedWidth += charSize.Width;
            if(accumulatedWidth > spaceLeft) break; // if the character didn't fit, we're done
            skipCharacters++; // we won't render the trailing whitespace though. instead, we'll skip it.
          }
        }
      }

      // if this is the last piece, and the line ends in a newline character, then add 1 to skip over it
      return new SplitPiece(new Span(start, charactersFit), size,
                            skipCharacters + (!lineWrapped && skippedNewLine ? 1 : 0), lineWrapped);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/Render/*"/>
    public override void Render(Graphics gdi, Point clientPoint, Span selection)
    {
      const TextFormatFlags DrawFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
                                        TextFormatFlags.SingleLine;
      // TODO: render the selection too
      Color fore = Node.Style.EffectiveForeColor ?? Editor.ForeColor;
      Color? back = Node.Style.BackColor; // assume the background is filled

      if(back.HasValue && back.Value.A != 0)
      {
        TextRenderer.DrawText(gdi, Node.GetText(Start-StartIndex, Length), Font, clientPoint, fore, back.Value,
                              DrawFlags);
      }
      else
      {
        TextRenderer.DrawText(gdi, Node.GetText(Start-StartIndex, Length), Font, clientPoint, fore, DrawFlags);
      }
    }

    readonly Font Font;
    readonly DocumentEditor Editor;
    readonly int StartIndex;
    /// <summary>A string that holds the current line during word-wrapping.</summary>
    string cachedText;

    static readonly Regex wordRE = new Regex(@"\s*\S+|\s+", RegexOptions.Singleline | RegexOptions.Compiled);
  }
  #endregion

  /// <summary>Given a block <see cref="DocumentNode"/>, creates and returns an appropriate <see cref="Block"/> to
  /// render the node. This method must not return null, but can use a generic <see cref="Block"/> for unknown document
  /// nodes.
  /// </summary>
  protected virtual Block CreateLayoutBlock(DocumentNode node)
  {
    return new Block();
  }

  /// <summary>Given an inline <see cref="DocumentNode"/>, and its starting index within the document, creates and
  /// returns an appropriate <see cref="LayoutSpan"/> to render the node, or null if the node cannot be rendered.
  /// </summary>
  protected virtual LayoutSpan CreateLayoutSpan(DocumentNode node, int startIndex)
  {
    TextNode textNode = node as TextNode;
    if(textNode != null) return new TextNodeSpan(textNode, this, startIndex);

    return null;
  }

  /// <summary>Creates a layout block given the rendering context, a document node, and the width available in pixels.
  /// </summary>
  BlockBase CreateBlock(Graphics gdi, DocumentNode node, int availableWidth, ref int startIndex)
  {
    Block newBlock = CreateLayoutBlock(node);

    // first, determine whether any of the children are block nodes
    bool allInline = true;
    foreach(DocumentNode child in node.Children)
    {
      if((child.Layout & RichDocument.Layout.Block) != 0) // if the child is a block node
      {
        allInline = false;
        break;
      }
    }

    if(allInline) // if there are no block children, lay them all out horizontally into a line
    {
      LineBlock lines = LayoutLines(gdi, node.Children, availableWidth, ref startIndex);
      if(lines == null) return null;
      newBlock.Blocks = new BlockBase[] { lines };
      newBlock.Span   = lines.Span;
      newBlock.Size   = lines.Size;
    }
    else // otherwise, one or more nodes is a block
    {
      // TODO: implement float and clear

      // create a list of blocks by using block nodes as-is and gathering runs of inline nodes into a line block
      List<BlockBase> blocks = new List<BlockBase>(node.Children.Count);
      List<DocumentNode> inline = null; // a list of inline nodes waiting to be grouped into a line block

      foreach(DocumentNode child in node.Children)
      {
        if((child.Layout & RichDocument.Layout.Block) == 0) // if the child is an inline node, add it to the list
        {
          if(inline == null) inline = new List<DocumentNode>();
          inline.Add(child);
        }
        else // otherwise, the child is a block node
        {
          if(inline != null && inline.Count != 0) // if we have collected some inline nodes already, group and add them
          {
            blocks.Add(LayoutLines(gdi, inline, availableWidth, ref startIndex));
            inline.Clear();
          }

          // TODO: implement margin and padding
          BlockBase block = CreateBlock(gdi, child, availableWidth, ref startIndex);
          if(block != null) blocks.Add(block);
        }
      }

      // if we have some inline nodes left, create a final line block to hold them
      if(inline != null && inline.Count != 0) blocks.Add(LayoutLines(gdi, inline, availableWidth, ref startIndex));

      newBlock.Blocks = blocks.ToArray();

      // go through the blocks and stack them up, while simultaneously calculating the size of the container
      foreach(BlockBase child in newBlock.Blocks)
      {
        int effectiveChildHeight = child.Top + child.Height; // the height of the child, including the top margin
        child.Top += newBlock.Height; // stick the block at the bottom of the stack

        // if the block is wider than the container, enlarge the container horizontally
        if(child.Bounds.Right > newBlock.Bounds.Right) newBlock.Width = child.Bounds.Right - newBlock.Left;

        newBlock.Height += effectiveChildHeight; // enlarge the container vertically
      }

      // calculate the span of the container
      newBlock.Start  = newBlock.Blocks[0].Start;
      newBlock.Length = newBlock.Blocks[newBlock.Blocks.Length-1].End - newBlock.Start;
    }

    return newBlock;
  }

  BlockBase CreateRootBlock(Graphics gdi)
  {
    int startIndex = 0;
    BlockBase rootBlock = CreateBlock(gdi, Document.Root, CanvasRectangle.Width, ref startIndex);
    if(rootBlock == null)
    {
      Block block = new Block();
      block.Blocks = new BlockBase[0];
      rootBlock = block;
    }
    return rootBlock;
  }

  /// <summary>Converts a <see cref="Measurement"/> into pixels.</summary>
  /// <param name="gdi">The graphics device.</param>
  /// <param name="node">The <see cref="DocumentNode"/> to which the measurement is related.</param>
  /// <param name="measurement">The measurement.</param>
  /// <param name="parentPixels">The size of the parent measurement, used in relative calculations.</param>
  /// <param name="orientation">The orientation of the measurement.</param>
  float GetPixels(Graphics gdi, DocumentNode node, Measurement measurement, int parentPixels,
                  Orientation orientation)
  {
    switch(measurement.Unit)
    {
      case Unit.FontRelative:
        Font font = GetEffectiveFont(node);
        return measurement.Size * GetPixels(gdi, font.Size, font.Unit, orientation);

      case Unit.Inches:
        return GetPixels(gdi, measurement.Size, GraphicsUnit.Inch, orientation);

      case Unit.Millimeters:
        return GetPixels(gdi, measurement.Size, GraphicsUnit.Millimeter, orientation);

      case Unit.Percent:
        return measurement.Size * 0.01f * parentPixels;

      case Unit.Pixels:
        return measurement.Size;

      case Unit.Points:
        return GetPixels(gdi, measurement.Size, GraphicsUnit.Point, orientation);

      default: throw new NotImplementedException("Measurement unit "+measurement.Unit.ToString()+" not implemented.");
    }
  }

  /// <summary>Creates a line block given the rendering context, the inline nodes to place into the block, and the
  /// width available, in pixels.
  /// </summary>
  LineBlock LayoutLines(Graphics gdi, ICollection<DocumentNode> inlineNodes, int availableWidth, ref int startIndex)
  {
    // the method creates a list of lines, each holding spans, from a list of nodes.

    List<Line> lines = new List<Line>(); // holds the lines for all of these nodes
    List<LayoutSpan> spans = new List<LayoutSpan>(); // holds the spans in the current line
    int lineWidth = 0, lineHeight = 0; // the size of the current line so far

    LayoutSpan span = null;
    foreach(DocumentNode node in inlineNodes)
    {
      // TODO: implement padding, margin, etc
      span = CreateLayoutSpan(node, startIndex);
      if(span == null) continue; // if this node cannot be rendered, skip to the next one

      span.Start = startIndex;
      for(int lineIndex=0; lineIndex < span.LineCount; lineIndex++) // for each line in the document node
      {
        if(lineIndex != 0) // if there was a line break in the document node (and hence a line other than the first),
        {                  // start a new output line too
          FinishLine(lines, spans, ref span, ref lineWidth, lineHeight, startIndex);
        }

        SplitPiece piece = null;
        while(true)
        {
          int spaceLeft = availableWidth - lineWidth;
          piece = span.GetNextSplitPiece(gdi, lineIndex, piece, spaceLeft, spaceLeft == availableWidth);
          if(piece == null) break;

          // so extend the size of the current line and span to encompass the new piece
          lineWidth += piece.Size.Width;
          lineHeight = Math.Max(lineHeight, piece.Size.Height);

          span.Size = new Size(span.Width + piece.Size.Width, Math.Max(span.Height, piece.Size.Height));
          span.Length += piece.Span.Length; // increase the span length of the size of the split piece
          startIndex  += piece.Span.Length + piece.Skip; // and update the next document index

          // if we need to start a new line before we can receive more of the content, do so
          if(piece.NewLine) FinishLine(lines, spans, ref span, ref lineWidth, lineHeight, startIndex);
        }
      }

      // now we've finished the current document node, so output the span (if it's not empty)
      if(span.Length != 0) spans.Add(span);
    }

    // if the last line is not empty, add it
    if(spans.Count != 0)
    {
      span = null; // the span has already been added, so don't add it again
      FinishLine(lines, spans, ref span, ref lineWidth, lineHeight, startIndex);
    }

    // now, loop through each of the lines that we've added and stack them vertically into a LineBlock
    LineBlock block = new LineBlock(lines.ToArray());
    foreach(Line line in block.Lines)
    {
      // calculate the line span
      if(line.Spans.Length != 0)
      {
        line.Start  = line.Spans[0].Start;
        line.Length = line.Spans[line.Spans.Length-1].End - line.Start;
      }

      // stack the spans horizontally within the line and calculate the maximum descent
      int maxDescent = 0;
      foreach(LayoutSpan s in line.Spans)
      {
        s.Position  = new Point(line.Width, 0);
        line.Width += s.Width;
        if(s.Descent > maxDescent) maxDescent = s.Descent;
      }

      // now go back through and position the spans vertically
      // TODO: implement vertical alignment
      foreach(LayoutSpan s in line.Spans)
      {
        s.Top += line.Height - s.Height + s.Descent - maxDescent; // just do bottom alignment for now...
      }

      // TODO: implement horizontal alignment
      line.Position = new Point(0, block.Height);
      block.Width   = Math.Max(line.Width, block.Width);
      block.Height += line.Height;
    }

    if(block.Lines.Length == 0) return null;

    block.Start  = block.Lines[0].Start;
    block.Length = block.Lines[block.Lines.Length-1].End - block.Start;
    return block;
  }

  /// <summary>Incrementally updates the layout around the given node and repaints the canvas.</summary>
  void RelayoutNode(DocumentNode node)
  {
    throw new NotImplementedException();
  }

  /// <summary>A helper for <see cref="LayoutLines"/> which ends the current line.</summary>
  static void FinishLine(List<Line> lines, List<LayoutSpan> spans, ref LayoutSpan span, ref int lineWidth,
                         int lineHeight, int startIndex)
  {
    if(span != null)
    {
      if(span.Length != 0) // if the current span contains something, add that to the line first
      {
        spans.Add(span);
        span = span.CreateNew();
      }
      span.Start = startIndex;
    }

    Line line = new Line(spans.ToArray());
    line.Height = lineHeight;
    line.Start  = startIndex; // this will be overwritten later if the line is not empty...
    lines.Add(line);

    spans.Clear();
    lineWidth = 0;
  }
  #endregion

  /// <summary>Gets whether a valid layout exists.</summary>
  protected bool HasLayout
  {
    get { return rootBlock != null; }
  }

  /// <summary>Adds all of the data formats available on the clipboard, like those found in
  /// <see cref="ClipboardDataFormats"/>, to the given list.
  /// </summary>
  protected virtual void AddPasteFormats(List<string> formats)
  {
    if(Clipboard.ContainsAudio()) formats.Add(ClipboardDataFormats.Audio);
    if(Clipboard.ContainsFileDropList()) formats.Add(ClipboardDataFormats.Files);
    if(Clipboard.ContainsImage()) formats.Add(ClipboardDataFormats.Image);
    if(Clipboard.ContainsText()) formats.Add(ClipboardDataFormats.Text);
    if(Clipboard.ContainsText(TextDataFormat.CommaSeparatedValue)) formats.Add(ClipboardDataFormats.CSV);
    if(Clipboard.ContainsText(TextDataFormat.Html)) formats.Add(ClipboardDataFormats.Html);
    if(Clipboard.ContainsText(TextDataFormat.Rtf)) formats.Add(ClipboardDataFormats.RTF);
    // TODO: verify that SymbolicLink is what I think it is. maybe it would be more fitting under "Files"
    if(Clipboard.ContainsData(DataFormats.SymbolicLink)) formats.Add(ClipboardDataFormats.Link);
    // TODO: see about DataFormats.StringFormat and DataFormats.OemText
  }

  /// <summary>Given a point relative to the control, converts it to a point relative to the canvas.</summary>
  protected Point ClientToCanvas(Point clientPoint)
  {
    Rectangle canvas = CanvasRectangle;
    return new Point(clientPoint.X - canvas.X, clientPoint.Y - canvas.Y);
  }

  /// <summary>Given a point relative to the control, converts it to a point relative to the document layout.</summary>
  protected Point ClientToDocument(Point clientPoint)
  {
    Point scrollPosition = ScrollPosition, canvasPoint = ClientToCanvas(clientPoint);
    return new Point(canvasPoint.X + scrollPosition.X, canvasPoint.Y + scrollPosition.Y);
  }

  /// <summary>Converts the data on the clipboard, in the given format, into nodes that can be inserted into the
  /// document.
  /// </summary>
  /// <param name="dataFormat">The type of clipboard data to use. This should be one of the values returned from
  /// <see cref="GetPasteFormats"/>.
  /// </param>
  protected virtual DocumentNode[] ConvertClipboardDataToNodes(string dataFormat)
  {
    throw new NotImplementedException();
  }

  /// <summary>Creates and returns an <see cref="IDataObject"/> representing the given section of the document.</summary>
  protected virtual IDataObject CreateClipboardDataObject(Span span)
  {
    ValidateSpan(span);
    throw new NotImplementedException();
  }

  /// <summary>Creates and returns a <see cref="ChangeEvent"/> that will delete the given span within the document.</summary>
  protected ChangeEvent CreateDeleteChange(Span span)
  {
    ValidateSpan(span);
    throw new NotImplementedException();
  }

  /// <summary>Creates and returns a <see cref="ChangeEvent"/> that will insert the given nodes into the document at
  /// the given position.
  /// </summary>
  protected ChangeEvent CreateInsertChange(int index, DocumentNode[] newNodes)
  {
    ValidateIndex(index);
    if(newNodes == null || newNodes.Length == 0) throw new ArgumentException("There are no nodes to insert.");
    throw new NotImplementedException();
  }

  /// <include file="documentation.xml" path="/UI/Common/Dispose/*"/>
  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    document.NodeChanged -= OnNodeChanged;
  }

  /// <summary>Updates the layout using the given graphics device if the layout is currently invalid.</summary>
  protected void EnsureLayout(Graphics gdi)
  {
    if(!HasLayout)
    {
      rootBlock = CreateRootBlock(gdi);

      // if a vertical scrollbar was created or destroyed, altering the canvas width, we need to redo the layout
      if(CreateOrDestroyScrollbars())
      {
        rootBlock = CreateRootBlock(gdi);
        CreateOrDestroyScrollbars(); // now a horizontal scrollbar may have been created or destroyed
      }

      SetAbsolutePositions(rootBlock, rootBlock.Position);
      ResizeScrollbars(gdi);
    }
  }

  /// <summary>Invalidates the entire layout.</summary>
  protected void InvalidateLayout()
  {
    DeselectAll();
    rootBlock = null;
    Invalidate(CanvasRectangle);
  }

  /// <summary>Invalidates the layout of the given node, and triggers a repaint.</summary>
  protected void InvalidateNode(DocumentNode node)
  {
    if(HasLayout) RelayoutNode(node); // if we have a layout, update it and repaint incrementally
    else Invalidate(CanvasRectangle); // otherwise, we have no layout, so just trigger a repaint
  }

  /// <include file="documentation.xml" path="/UI/Document/OnNodeChanged/*"/>
  protected virtual void OnNodeChanged(Document document, DocumentNode node)
  {
    InvalidateNode(node);
  }

  /// <summary>Paints the control.</summary>
  protected override void OnPaint(PaintEventArgs e)
  {
    base.OnPaint(e);

    if(layoutSuspended) // if the layout is suspended, we'll repaint the control after layout resumes
    {
      repaintNeeded = true;
      return;
    }

    EnsureLayout(e.Graphics);
    Rectangle canvasRect = CanvasRectangle;

    // erase the invalid area with the background color for the control
    if(BackColor.A != 0) // if the background is not transparent
    {
      using(Brush bgBrush = new SolidBrush(BackColor)) e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
    }

    // render the document
    Point scrollPosition = ScrollPosition;
    RenderData data = new RenderData(Rectangle.Intersect(e.ClipRectangle, canvasRect), Selection);
    Rectangle renderArea = new Rectangle(canvasRect.X - scrollPosition.X, canvasRect.Y - scrollPosition.Y,
                                         canvasRect.Width, canvasRect.Height);
    rootBlock.Render(e.Graphics, ref renderArea, ref data);

    // since the text rendering is done with GDI rather than GDI+, it doesn't obey the GDI+ clip region. so the text
    // may have overlapped the border or the corner between the scrollbars. so we'll redraw them unconditionally now.

    // if we have both scrollbars, fill in the corner between them
    if(hScrollBar != null && vScrollBar != null)
    {
      using(Brush brush = new SolidBrush(SystemColors.Control))
      {
        e.Graphics.FillRectangle(brush,
          new Rectangle(canvasRect.Right, canvasRect.Bottom, vScrollBar.Width, hScrollBar.Height));
      }
    }

    // render the border
    if(BorderStyle != BorderStyle.None)
    {
      ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, 
                                BorderStyle == BorderStyle.Fixed3D ? Border3DStyle.Sunken : Border3DStyle.Flat);
    }
  }

  /// <summary>Called when the background color changes.</summary>
  protected override void OnBackColorChanged(EventArgs e)
  {
    base.OnBackColorChanged(e);
    Invalidate(CanvasRectangle);
  }

  /// <summary>Called when the font changes.</summary>
  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);

    // if no font is set at the root, make the assumption that the new control font will be used somewhere and that
    // it's a different size. (optimizing to detect fonts of the same size seems like overkill...)
    if(GetEffectiveFont(Document.Root) == Font)
    {
      InvalidateLayout(); // and if the font is used and is a different size, we need to recaluclate the layout
    }
  }

  /// <summary>Called when the foreground color changes.</summary>
  protected override void OnForeColorChanged(EventArgs e)
  {
    base.OnForeColorChanged(e);

    // if no foreground color is defined at the root, assume that the control color will be used somewhere
    Color? rootColor = Document.Root.Style.ForeColor;
    if(!rootColor.HasValue) Invalidate(CanvasRectangle);
  }

  /// <summary>Called when the size of the control changes.</summary>
  protected override void OnSizeChanged(EventArgs e)
  {
    base.OnSizeChanged(e);

    if(HasLayout)
    {
      // if the width changed or the height changed such that the vertical scrollbar was added or removed, then we need
      // to redo the layout
      if(controlWidth != Width || CreateOrDestroyScrollbars())
      {
        InvalidateLayout();
      }
      else if(hScrollBar != null || vScrollBar != null) // otherwise, the layout is still valid, so we just need to
      {                                                 // resize the scrollbars to fit the control
        using(Graphics gdi = Graphics.FromHwnd(Handle)) ResizeScrollbars(gdi);
      }
    }

    controlWidth = Width; // keep track of the width so we can tell when it changes
  }

  /// <summary>Given data on the clipboard in the given formats, selects the best format to use for pasting the data
  /// into the document. If no format can or should be used, the method returns null.
  /// </summary>
  protected virtual string SelectBestClipboardFormat(string[] availableFormats)
  {
    throw new NotImplementedException();
  }

  /// <summary>Throws an exception if the given index is out of bounds for the document.</summary>
  protected void ValidateIndex(int index)
  {
    if(index < 0 || index > IndexLength) throw new ArgumentOutOfRangeException();
  }

  /// <summary>Throws an exception if the given span is out of bounds for the document.</summary>
  protected void ValidateSpan(Span span)
  {
    if(span.End > IndexLength) throw new ArgumentOutOfRangeException();
  }

  #region Win32 Interop
  [StructLayout(LayoutKind.Sequential)]
  struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public RECT(Rectangle rect)
    {
      this.Bottom = rect.Bottom;
      this.Left   = rect.Left;
      this.Right  = rect.Right;
      this.Top    = rect.Top;
    }

    public RECT(int left, int top, int right, int bottom)
    {
      this.Bottom = bottom;
      this.Left   = left;
      this.Right  = right;
      this.Top    = top;
    }
  }

  [DllImport("user32.dll", EntryPoint="ScrollWindow")]
  static extern bool W32ScrollWindow(IntPtr hWnd, int xOffset, int yOffset, ref RECT scrollRegion, ref RECT clipRect);
  #endregion

  internal struct RenderData
  {
    public RenderData(Rectangle clipRect, Span selection)
    {
      ClipRectangle = clipRect;
      Selection     = selection;
    }

    public Rectangle ClipRectangle;
    public Span Selection;
  }

  /// <summary>Gets the thickness of the border, in pixels.</summary>
  int BorderWidth
  { // TODO: we probably shouldn't be making assumptions about how the border will be drawn...
    get { return BorderStyle == BorderStyle.Fixed3D ? 2 : BorderStyle == BorderStyle.FixedSingle ? 1 : 0; }
  }

  /// <summary>Handles the horizontal scrollbar value changing.</summary>
  void hScrollBar_ValueChanged(object sender, EventArgs e)
  {
    ScrollCanvas(hScrollPos - hScrollBar.Value, 0);
    hScrollPos = hScrollBar.Value;
  }

  /// <summary>Handles the vertical scrollbar value changing.</summary>
  void vScrollBar_ValueChanged(object sender, EventArgs e)
  {
    ScrollCanvas(0, vScrollPos - vScrollBar.Value);
    vScrollPos = vScrollBar.Value;
  }

  /// <summary>Adds scrollbars to the document or removes them, depending on the size of the document and the size of
  /// the canvas. Returns true if the new scrollbars have altered the canvas such that the layout needs to be redone.
  /// </summary>
  bool CreateOrDestroyScrollbars()
  {
    Rectangle canvasRect = CanvasRectangle;

    bool isRequired = (ScrollBars & ScrollBars.Vertical) != 0;
    if(vScrollBar != null && !isRequired && HasLayout && rootBlock.Bounds.Bottom <= canvasRect.Height)
    {
      // if the vertical scrollbar is being removed, invalidate the right side of the canvas
      Invalidate(new Rectangle(vScrollBar.Left, vScrollBar.Top, vScrollBar.Width, Height));

      vScrollBar.ValueChanged -= vScrollBar_ValueChanged;
      Controls.Remove(vScrollBar);
      vScrollBar = null;
      vScrollPos = 0;
      return true;
    }
    else if(vScrollBar == null && (isRequired || HasLayout && rootBlock.Bounds.Bottom > canvasRect.Height))
    {
      vScrollBar = new VScrollBar();
      vScrollBar.ValueChanged += vScrollBar_ValueChanged;
      Controls.Add(vScrollBar);
      return true;
    }

    isRequired = (ScrollBars & ScrollBars.Horizontal) != 0;
    if(hScrollBar != null && !isRequired && HasLayout && rootBlock.Bounds.Right <= canvasRect.Width)
    {
      // if the horizontal scrollbar is being removed, invalidate the bottom side of the canvas
      Invalidate(new Rectangle(hScrollBar.Left, hScrollBar.Top, Width, hScrollBar.Height));

      hScrollBar.ValueChanged -= hScrollBar_ValueChanged;
      Controls.Remove(hScrollBar);
      hScrollBar = null;
      hScrollPos = 0;
    }
    else if(hScrollBar == null && (isRequired || HasLayout && rootBlock.Bounds.Right > canvasRect.Width))
    {
      hScrollBar = new HScrollBar();
      hScrollBar.ValueChanged += hScrollBar_ValueChanged;
      Controls.Add(hScrollBar);

      if(vScrollBar != null) // if we now have both scroll bars, then we need to invalidate the corner between them
      {
        canvasRect = CanvasRectangle; // get the updated canvas rectangle, which changed when the scrollbar was added
        Invalidate(new Rectangle(canvasRect.Right, canvasRect.Bottom, vScrollBar.Width, hScrollBar.Height));
      }
    }

    return false;
  }

  /// <summary>Returns the font to be used to render text in a document node.</summary>
  Font GetEffectiveFont(DocumentNode node)
  {
    // if the node is null, simply return the control font
    if(node == null) return Font;

    // get the font names and style
    string[] fontNames = node.Style.EffectiveFontNames;
    FontStyle fontStyle = node.Style.EffectiveFontStyle ?? Font.Style;
    Measurement? size = node.Style.EffectiveFontSize;
    float fontSize;
    GraphicsUnit fontUnit;

    // now calculate the font size
    if(!size.HasValue) // if there is no font size defined by the document, use the control's font
    {
      fontSize = Font.Size;
      fontUnit = Font.Unit;
    }
    else // otherwise, there is a font size defined, so calculate the GDI size and unit
    {
      fontSize = size.Value.Size;
      fontUnit = GraphicsUnit.Point;

      Font parentFont;
      switch(size.Value.Unit)
      {
        case Unit.FontRelative: // the font relative size is relative the parent font in this case
          parentFont = GetEffectiveFont(node.Parent);
          fontSize *= parentFont.Size;
          fontUnit  = parentFont.Unit;
          break;

        case Unit.Inches:
          fontUnit = GraphicsUnit.Inch;
          break;

        case Unit.Millimeters:
          fontUnit = GraphicsUnit.Millimeter;
          break;

        case Unit.Percent:
          parentFont = GetEffectiveFont(node.Parent);
          fontSize = fontSize * 0.01f * parentFont.Size;
          fontUnit = parentFont.Unit;
          break;

        case Unit.Pixels:
          fontUnit = GraphicsUnit.Pixel;
          break;

        case Unit.Points:
          fontUnit = GraphicsUnit.Point;
          break;
      }
    }

    // finally, figure out the font family
    FontFamily family = null;
    if(fontNames != null) // if a list of font names was passed, try them each in turn
    {
      foreach(string fontName in fontNames)
      {
        // define built-in names "serif", "sans-serif", and "mono" as generic fonts of that type
        if(string.Equals(fontName, "serif", StringComparison.OrdinalIgnoreCase))
        {
          family = FontFamily.GenericSerif;
        }
        else if(string.Equals(fontName, "sans-serif", StringComparison.OrdinalIgnoreCase))
        {
          family = FontFamily.GenericSansSerif;
        }
        else if(string.Equals(fontName, "mono", StringComparison.OrdinalIgnoreCase))
        {
          family = FontFamily.GenericMonospace;
        }
        else // if it's not a built-in name, try to get the font from the system
        {
          family = FontHelpers.GetFontFamily(fontName);
        }

        if(family != null) break; // if we got a font family, we're done
      }
    }

    // if none of the font names were valid, use the control's font family
    if(family == null) family = Font.FontFamily;

    return new Font(family, fontSize, fontStyle, fontUnit);
  }

  /// <summary>Gets the span of indices within the document that the given node contains.</summary>
  Span GetIndexSpan(DocumentNode node)
  {
    throw new NotImplementedException();
  }

  /// <summary>Gets the innermost <see cref="LayoutRegion"/> that contains the given index.</summary>
  LayoutRegion GetRegion(int index)
  {
    throw new NotImplementedException();
  }

  /// <summary>Given a span within the document, returns the portion of the span that is visible within the control,
  /// in client units.
  /// </summary>
  Rectangle GetVisibleArea(Span span)
  {
    throw new NotImplementedException();
  }

  /// <summary>Places and sizes the scrollbars based on the size of the control and the size of the document.</summary>
  void ResizeScrollbars(Graphics gdi)
  {
    if(HasLayout)
    {
      Rectangle canvasRect = CanvasRectangle;
      Font font = GetEffectiveFont(document.Root);

      if(vScrollBar != null)
      {
        vScrollBar.Top    = canvasRect.Top;
        vScrollBar.Left   = canvasRect.Right;
        vScrollBar.Height = canvasRect.Height;

        int smallChange = (int)Math.Ceiling(font.GetHeight(gdi));
        int largeChange = Math.Max(0, canvasRect.Height - smallChange);
        // the range (ie, .Maximum) has to be set before SmallChange and LargeChange, otherwise they may may be clipped
        vScrollBar.Maximum     = Math.Max(0, rootBlock.Bounds.Bottom - canvasRect.Height + largeChange - 1);
        vScrollBar.SmallChange = smallChange;
        vScrollBar.LargeChange = largeChange;
        vScrollBar.Enabled     = rootBlock.Bounds.Bottom > canvasRect.Height; // disable the scrollbar if the document
                                                                              // already fits vertically
        vScrollPos = Math.Min(vScrollBar.Value, vScrollBar.Maximum - vScrollBar.LargeChange + 1);
      }

      if(hScrollBar != null)
      {
        hScrollBar.Top   = canvasRect.Bottom;
        hScrollBar.Left  = canvasRect.Left;
        hScrollBar.Width = canvasRect.Width;

        int smallChange = (int)Math.Ceiling(gdi.MeasureString("W", font).Width);
        int largeChange = Math.Max(0, canvasRect.Width - smallChange);
        // the range (ie, .Maximum) has to be set before SmallChange and LargeChange, otherwise they may may be clipped
        hScrollBar.Maximum     = Math.Max(0, rootBlock.Bounds.Right - canvasRect.Width + largeChange - 1);
        hScrollBar.SmallChange = smallChange;
        hScrollBar.LargeChange = largeChange;
        hScrollBar.Enabled     = rootBlock.Bounds.Right > canvasRect.Width; // disable the scrollbar if the document
                                                                            // already fits horizontally
        hScrollPos = Math.Min(hScrollBar.Value, hScrollBar.Maximum - hScrollBar.LargeChange + 1);
      }
    }
  }

  /// <summary>Scrolls the canvas image by the given amount and invalidates the uncovered portion.</summary>
  /// <param name="xOffset">The amount to scroll the canvas horizontally. A negative value moves the canvas image to
  /// the left.
  /// </param>
  /// <param name="yOffset">The amount to scroll the canvas vertically. A negative value moves the canvas image
  /// upwards.
  /// </param>
  /// <remarks>This method uses an optimized system calls if they are available.</remarks>
  void ScrollCanvas(int xOffset, int yOffset)
  {
    PlatformID platform = Environment.OSVersion.Platform;
    if(platform == PlatformID.Win32NT || platform == PlatformID.Win32Windows || platform == PlatformID.WinCE)
    {
      RECT canvasRect = new RECT(CanvasRectangle);
      W32ScrollWindow(Handle, xOffset, yOffset, ref canvasRect, ref canvasRect);
    }
    else
    {
      // TODO: attempt a manual scroll using the GDI and see how fast it is compared to a full repaint
      Invalidate(CanvasRectangle);
    }
  }

  readonly Document document;
  BlockBase rootBlock;
  HScrollBar hScrollBar;
  VScrollBar vScrollBar;
  int cursorIndex, hScrollPos, vScrollPos, controlWidth;
  Span selection;
  ScrollBars scrollBars;
  BorderStyle borderStyle = BorderStyle.Fixed3D;
  bool readOnly, layoutSuspended, repaintNeeded;

  /// <summary>Given a graphics context, a measurement, the measurement unit, and the measurement orientation, returns
  /// the size of the measurement in pixels.
  /// </summary>
  static float GetPixels(Graphics gdi, float size, GraphicsUnit unit, Orientation orientation)
  {
    float inches = size * (orientation == Orientation.Horizontal ? gdi.DpiX : gdi.DpiY);
    switch(unit)
    {
      case GraphicsUnit.Document:   return inches * (1/300f);
      case GraphicsUnit.Inch:       return inches;
      case GraphicsUnit.Millimeter: return inches * (1/25.4f);
      case GraphicsUnit.Pixel:      return size;
      case GraphicsUnit.Point:      return inches * (1/72f);
      default: throw new NotSupportedException();
    }
  }

  /// <summary>Sets the absolute position of the region to the given value, and recursively updates its descendants.</summary>
  static void SetAbsolutePositions(LayoutRegion region, Point absPosition)
  {
    region.AbsolutePosition = absPosition;

    foreach(LayoutRegion child in region.Children)
    {
      SetAbsolutePositions(child, new Point(absPosition.X+child.Left, absPosition.Y+child.Top));
    }
  }
}

} // namespace AdamMil.UI.RichDocument