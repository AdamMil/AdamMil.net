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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AC = AdamMil.Collections;

namespace AdamMil.UI.RichDocument
{
/*
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

    // give the control a default size and create the vertical scrollbar immediately to prevent the initial layout
    // from having to be done multiple times
    Size       = controlSize = new Size(100, 100);
    ScrollBars = ScrollBars.Vertical;
  }

  /// <summary>Gets or sets the border style of the control. The default is
  /// <see cref="System.Windows.Forms.BorderStyle.Fixed3D"/>.
  /// </summary>
  [Category("Appearance")]
  [DefaultValue(System.Windows.Forms.BorderStyle.Fixed3D)]
  [Description("Determines how the border of the control will be drawn.")]
  public System.Windows.Forms.BorderStyle BorderStyle
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
      if(vScrollBar != null) rect.Width  -= vScrollBar.Width;  // account for the vertical scroll bar
      return rect;
    }
  }

  /// <summary>Gets or sets the index at which the text cursor (caret) is displayed, from 0 to
  /// <see cref="IndexLength"/> inclusive.
  /// </summary>
  [Browsable(false)]
  public int CursorIndex
  {
    get { return cursorIndex; }
    set
    {
      if(value != CursorIndex)
      {
        ValidateIndex(value);
        CursorIndexSetter(value);
        idealCursorX = GetDocumentPoint(cursorRect.Location).X;
      }
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

  /// <summary>Gets or sets whether the text cursor is enabled, which affects the way keyboard and mouse input behave.</summary>
  [Category("Behavior")]
  [Description("Determines whether the text cursor is visible, which affects how keyboard and mouse input behave.")]
  public bool EnableCursor
  {
    get { return enableCursor; }
    set
    {
      if(value != EnableCursor)
      {
        enableCursor = value;
        UpdateCursor();
      }
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

  /// <summary>Gets the width and height of the area that can be scrolled into.</summary>
  public Size ScrollArea
  {
    get { return new Size(HScrollMaximum, VScrollMaximum); }
  }

  /// <summary>Gets or sets the position within the document that is being rendered at the top-left pixel of the
  /// control's canvas area.
  /// </summary>
  [Browsable(false)]
  public Point ScrollPosition
  {
    get { return new Point(hScrollPos, vScrollPos); }
    set
    {
      Layout();

      if((uint)value.X > (uint)(hScrollBar == null ? 0 : hScrollBar.Maximum) ||
         (uint)value.Y > (uint)(vScrollBar == null ? 0 : vScrollBar.Maximum))
      {
        throw new ArgumentOutOfRangeException();
      }

      if(hScrollBar != null) hScrollBar.Value = Math.Min(HScrollMaximum, value.X);
      if(vScrollBar != null) vScrollBar.Value = Math.Min(VScrollMaximum, value.Y);
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
        ValidateSpan(value);

        if(selection.Length != 0 || value.Length != 0)
        {
          // get the bounding boxes of the previous and new selection
          Rectangle oldArea = GetVisibleArea(selection), newArea = GetVisibleArea(value);

          // set the new value
          selection = value;

          // and invalidate the affected areas of the control, if any
          if(ClientRectangle.IntersectsWith(oldArea)) Invalidate(oldArea);
          if(ClientRectangle.IntersectsWith(newArea)) Invalidate(newArea);
        }
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
    document.Clear();
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
  /// there's no selection, or at the end of the document if there is no cursor.
  /// </summary>
  public void Paste()
  {
    Span span = Selection;
    if(span.Length == 0) span.Start = EnableCursor ? CursorIndex : IndexLength;
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

  #region Layout classes and methods, including rendering code
  #region LayoutRegion
  /// <summary>Represents a rectangular area within the document layout into which a span of the document indices are
  /// rendered.
  /// </summary>
  protected abstract class LayoutRegion : IDisposable
  {
    /// <summary>Gets the absolute horizontal position of the leftmost pixel of the region.</summary>
    public int AbsoluteLeft
    {
      get { return AbsolutePosition.X; }
    }

    /// <summary>Gets the absolute vertical position of the topmost pixel of the region.</summary>
    public int AbsoluteTop
    {
      get { return AbsolutePosition.Y; }
    }

    /// <summary>Gets the absolute horizontal position of the pixel just to the right of the region.</summary>
    public int AbsoluteRight
    {
      get { return AbsolutePosition.X+Width; }
    }

    /// <summary>Gets the absolute vertical position of the pixel just below the region.</summary>
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

    /// <summary>Returns the rectangle that encloses the border of the control, relative to its top-left corner.</summary>
    public Rectangle BorderBox
    {
      get
      {
        Rectangle borderBox = new Rectangle(new Point(), Size);
        switch(NodePart)
        {
          case NodePart.Full:
            borderBox.Offset(Margin.Left, Margin.Top);
            borderBox.Width  -= Margin.TotalHorizontal;
            borderBox.Height -= Margin.TotalVertical;
            break;
          case NodePart.Start:
            borderBox.Offset(Margin.Left, Margin.Top);
            borderBox.Width  -= Margin.Left;
            borderBox.Height -= Margin.Top;
            break;
          case NodePart.End:
            borderBox.Width  -= Margin.Right;
            borderBox.Height -= Margin.Bottom;
            break;
        }
        return borderBox;
      }
    }

    /// <summary>Returns the area of the control, relative to its top-left corner, that is within the padding, border,
    /// and margin.
    /// </summary>
    public Rectangle ContentArea
    {
      get
      {
        Rectangle contentArea = new Rectangle(0, Border.Height + Padding.Top,
                                              Width, Height - Border.Height*2 - Padding.TotalVertical);
        switch(NodePart)
        {
          case NodePart.Full:
            contentArea.Offset(Border.Width + Margin.Left + Padding.Left, Padding.Top + Margin.Top);
            contentArea.Width  -= Border.Width*2 + Margin.TotalHorizontal + Padding.TotalHorizontal;
            contentArea.Height -= Margin.TotalVertical;
            break;
          case NodePart.Start:
            contentArea.Offset(Border.Width + Margin.Left + Padding.Left, Padding.Top + Margin.Top);
            contentArea.Width  -= Border.Width + Margin.Left + Padding.Left;
            contentArea.Height -= Margin.Top;
            break;
          case NodePart.End:
            contentArea.Width  -= Border.Width + Margin.Right + Padding.Right;
            contentArea.Height -= Margin.Bottom;
            break;
        }
        return contentArea;
      }
    }

    /// <summary>Returns the area of the control, relative to its top-left corner, that is within the border and
    /// margin.
    /// </summary>
    public Rectangle PaddingArea
    {
      get
      {
        Rectangle paddingArea = new Rectangle(0, Border.Height, Width, Height - Border.Height*2);
        switch(NodePart)
        {
          case NodePart.Full:
            paddingArea.Offset(Border.Width + Margin.Left, Margin.Top);
            paddingArea.Width  -= Border.Width*2 + Margin.TotalHorizontal;
            paddingArea.Height -= Margin.TotalVertical;
            break;
          case NodePart.Start:
            paddingArea.Offset(Border.Width + Margin.Left, Margin.Top);
            paddingArea.Width  -= Border.Width + Margin.Left;
            paddingArea.Height -= Margin.Top;
            break;
          case NodePart.End:
            paddingArea.Width  -= Border.Width + Margin.Right;
            paddingArea.Height -= Margin.Bottom;
            break;
        }
        return paddingArea;
      }
    }

    /// <summary>Gets the width of the left border, margin, and padding.</summary>
    public int LeftPBM
    {
      get
      {
        return NodePart == NodePart.Full || NodePart == NodePart.Start ?
          Border.Width + Margin.Left + Padding.Left : 0;
      }
    }

    /// <summary>Gets the width of the right border, margin, and padding.</summary>
    public int RightPBM
    {
      get
      {
        return NodePart == NodePart.Full || NodePart == NodePart.End ? Border.Width + Margin.Right + Padding.Right : 0;
      }
    }

    /// <summary>Gets the width of the top border, margin, and padding.</summary>
    public int TopPBM
    {
      get
      {
        int thickness = Border.Height + Padding.Top;
        if(NodePart == NodePart.Full || NodePart == NodePart.Start) thickness += Margin.Top;
        return thickness;
      }
    }

    /// <summary>Gets the width of the bottom border, margin, and padding.</summary>
    public int BottomPBM
    {
      get
      {
        int thickness = Border.Height + Padding.Bottom;
        if(NodePart == NodePart.Full || NodePart == NodePart.End) thickness += Margin.Bottom;
        return thickness;
      }
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

    /// <summary>Gets or sets the <see cref="AdamMil.UI.Span.Length"/> member of the <see cref="ContentSpan"/>
    /// property.
    /// </summary>
    public int ContentLength
    {
      get { return ContentSpan.Length; }
      set { ContentSpan = new Span(ContentStart, value); }
    }

    /// <summary>Gets or sets the <see cref="AdamMil.UI.Span.Start"/> member of the <see cref="ContentSpan"/>
    /// property.
    /// </summary>
    public int ContentStart
    {
      get { return ContentSpan.Start; }
      set { ContentSpan = new Span(value, ContentLength); }
    }

    /// <summary>Gets a span containing the document indices that render actual content.</summary>
    public Span DocumentContentSpan
    {
      get { return new Span(Start, ContentLength); }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/BeginLayout/node()"/>
    public virtual void BeginLayout(Graphics gdi) { }

    /// <include file="documentation.xml" path="/UI/Common/Dispose/node()"/>
    public virtual void Dispose()
    {
      foreach(LayoutRegion child in GetChildren()) child.Dispose();
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/EndLayout/node()"/>
    public virtual void EndLayout(Graphics gdi)
    {
      int index = 0;
      foreach(LayoutRegion child in GetChildren())
      {
        child.Parent = this;
        child.Index  = index++;
        child.EndLayout(gdi);
      }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/node()"/>
    public abstract LayoutRegion[] GetChildren();

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetCursorHeight/node()"/>
    public virtual int GetCursorHeight(int index)
    {
      return Math.Max(index == 0 || index >= ContentLength ? BorderBox.Height : ContentArea.Height, 1);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNearestIndex/node()"/>
    public virtual int GetNearestIndex(Graphics gdi, Point regionPt)
    {
      int end = DocumentContentSpan.End;

      // if the point is not contained within the region, return the start or end depending on where it is
      Rectangle paddingArea = PaddingArea;
      if(regionPt.Y < paddingArea.Top) return Start;
      else if(regionPt.Y >= paddingArea.Bottom) return end;
      else if(regionPt.X < paddingArea.Left) return Start;
      else if(regionPt.X >= paddingArea.Right) return end;

      // the point is contained within this region, so find the child node closest to the point
      LayoutRegion closestChild = null;
      uint minDistance = uint.MaxValue;
      foreach(LayoutRegion child in GetChildren())
      {
        uint distance = CalculateDistance(regionPt, child.Bounds);
        if(distance < minDistance)
        {
          minDistance  = distance;
          closestChild = child;
          if(minDistance == 0) break; // zero signifies containment of the point, so we need look no further
        }
      }

      if(closestChild != null) // if there is a child node, delegate to it.
      {
        regionPt.Offset(-closestChild.Left, -closestChild.Top); // translate the point into the child's space
        return closestChild.GetNearestIndex(gdi, regionPt);
      }
      else return Start; // otherwise, there are no children node, so just return the starting index
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetPixelOffset/node()"/>
    public virtual Point GetPixelOffset(Graphics gdi, int indexOffset)
    {
      if((uint)indexOffset > (uint)Length) throw new ArgumentOutOfRangeException();

      if(indexOffset > 0 && indexOffset < Length)
      {
        // the base implementation only supports indivisible nodes, so try delegating to a descendant
        LayoutRegion descendant = GetRegion(indexOffset + Start);
        if(descendant != this)
        {
          indexOffset += Start; // make the index absolute
          Point offset = descendant.GetPixelOffset(gdi, indexOffset - descendant.Start);
          do
          {
            offset.Offset(descendant.Position);
            descendant = descendant.Parent;
          } while(descendant != this);

          return offset;
        }
        else if(indexOffset == 1)
        {
          return ContentArea.Location;
        }
        else if(indexOffset < ContentLength) throw new NotImplementedException();
      }

      // if the border is one pixel wide, the cursor will cause the border to flash on and off, so we'll move the
      // cursor one pixel to the outside of the border
      // TODO: FIXME: if the side of the region is flush with the canvas, the cursor will be off-canvas and disappear :-(
      return new Point(indexOffset == 0 ? Margin.Left-(Border.Width == 1 ? 1 : 0) : Width-Margin.Right, BorderBox.Top);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetRegion/node()"/>
    public virtual LayoutRegion GetRegion(int index)
    {
      if(!Span.Contains(index)) return null;

      foreach(LayoutRegion child in GetChildren())
      {
        LayoutRegion descendant = child.GetRegion(index);
        if(descendant != null) return descendant;
      }
      return this;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNode/node()"/>
    public virtual DocumentNode GetNode() { return null; }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/OnClick/node()"/>
    public virtual void OnClick(DocumentEditor editor, MouseEventArgs e)
    {
      if(Parent != null) Parent.OnClick(editor, e);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/OnDoubleClick/node()"/>
    public virtual void OnDoubleClick(DocumentEditor editor, MouseEventArgs e)
    {
      if(Parent != null) Parent.OnDoubleClick(editor, e);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/OnMouseEnter/node()"/>
    public virtual void OnMouseEnter(DocumentEditor editor, MouseEventArgs e) { }
    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/OnMouseLeave/node()"/>
    public virtual void OnMouseLeave(DocumentEditor editor) { }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/OnMouseHover/node()"/>
    public virtual void OnMouseHover(DocumentEditor editor, Point docPt)
    {
      if(Parent != null) Parent.OnMouseHover(editor, docPt);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Render/node()"/>
    public virtual void Render(ref RenderData data, Point clientPoint)
    {
      RenderBackgroundAndBorder(ref data, clientPoint);

      foreach(LayoutRegion child in GetChildren())
      {
        Rectangle childArea = new Rectangle(clientPoint.X + child.Left, clientPoint.Y + child.Top,
                                            child.Width, child.Height);
        if(childArea.IntersectsWith(data.ClipRectangle)) child.Render(ref data, childArea.Location);
      }
    }

    /// <summary>The layout region that contains this region, or null if this is the root region.</summary>
    public LayoutRegion Parent;
    /// <summary>The region's index within the parent's collection of children.</summary>
    public int Index;
    /// <summary>The region's bounds, relative to the parent region.</summary>
    public Rectangle Bounds;
    /// <summary>The region's absolute position in the document.</summary>
    public Point AbsolutePosition;
    /// <summary>The span of indices that this region contains.</summary>
    public Span Span;
    /// <summary>Gets or sets the span of the associated document node's content that is rendered within this region.</summary>
    public Span ContentSpan;
    /// <summary>The thickness of the margin, in pixels.</summary>
    public FourSideInt Margin;
    /// <summary>The thickness of the padding, in pixels.</summary>
    public FourSideInt Padding;
    /// <summary>The thickness of the border, in pixels.</summary>
    public Size Border;
    /// <summary>The portion of the <see cref="DocumentNode"/> represented by this region.</summary>
    public NodePart NodePart;

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/CalculateDistance/node()"/>
    /// <remarks>Distances are biased so that points contained vertically are always nearer than points not contained
    /// vertically. The distances should not be taken as actual measurements of space, but only used for comparisons
    /// of relative closeness.
    /// </remarks>
    protected virtual uint CalculateDistance(Point pt, Rectangle rect)
    {
      int xDistance = pt.X < rect.Left ? rect.Left-pt.X : pt.X >= rect.Right  ? pt.X-rect.Right+1  : 0;
      int yDistance = pt.Y < rect.Top  ? rect.Top-pt.Y  : pt.Y >= rect.Bottom ? pt.Y-rect.Bottom+1 : 0;

      if(xDistance == 0 && yDistance == 0) return 0;
      else if(xDistance >= 32768 || yDistance >= 32768) return uint.MaxValue; // prevent overflow
      else
      {
        uint distance = (uint)(xDistance*xDistance + yDistance*yDistance);
        return yDistance == 0 ? distance : distance | 0x80000000;
      }
    }

    /// <summary>Renders the background of the region if it is associated with a <see cref="DocumentNode"/>.</summary>
    protected void RenderBackground(ref RenderData data, Point clientPoint)
    {
      DocumentNode node = GetNode();

      bool isSelected = data.Selection.Contains(DocumentContentSpan);
      if(node != null || isSelected)
      {
        Color? color = isSelected ? SystemColors.Highlight : node.Style.BackColor;
        if(color.HasValue && color.Value.A != 0)
        {
          using(Brush brush = new SolidBrush(color.Value))
          {
            Rectangle paddingBox = PaddingArea;
            paddingBox.Offset(clientPoint);
            data.Graphics.FillRectangle(brush, paddingBox);
          }
        }
      }
    }

    /// <summary>Renders the background and border of the region if it is associated with a <see cref="DocumentNode"/>.</summary>
    /// <remarks>Calling this method is equivalent to calling <see cref="RenderBackground"/> and
    /// <see cref="RenderBorder"/>.
    /// </remarks>
    protected void RenderBackgroundAndBorder(ref RenderData data, Point clientPoint)
    {
      RenderBackground(ref data, clientPoint);
      RenderBorder(ref data, clientPoint);
    }

    /// <summary>Renders the border of the region if it is associated with a <see cref="DocumentNode"/>.</summary>
    protected void RenderBorder(ref RenderData data, Point clientPoint)
    {
      DocumentNode node = GetNode();
      if(node != null && !Border.IsEmpty)
      {
        bool isSelected = data.Selection.Contains(DocumentContentSpan);
        BorderStyle borderStyle = node.Style.BorderStyle;
        Color color = isSelected ? SystemColors.HighlightText
          : node.Style.BorderColor ?? node.Style.EffectiveForeColor ?? data.Editor.ForeColor;
        if(borderStyle != RichDocument.BorderStyle.None && color.A != 0)
        {
          using(Pen pen = new Pen(color))
          {
            switch(borderStyle)
            {
              case RichDocument.BorderStyle.Dashed:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                break;
              case RichDocument.BorderStyle.Dotted:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                break;
              case RichDocument.BorderStyle.DashDotted:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
                break;
              default:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                break;
            }

            // pen.Alignment doesn't work reliably on Windows so we need to handle the inset calculation ourselves.
            RectangleF borderRect = BorderBox;
            borderRect.Offset(clientPoint.X + (Border.Width-1)*0.5f, clientPoint.Y + (Border.Height-1)*0.5f);
            borderRect.Width  -= Border.Width;
            borderRect.Height -= Border.Height;

            // if we can draw it with a single DrawRectangle call, then do so
            // TODO: GDI+ sucks ass when drawing thick borders with dash styles. we need a replacement for this code
            if(Border.Width == Border.Height && NodePart == NodePart.Full)
            {
              pen.Width = Border.Width;
              data.Graphics.DrawRectangle(pen, borderRect.X, borderRect.Y, borderRect.Width, borderRect.Height);
            }
            else // otherwise, we're either not drawing a rectangle, or the horizontal lines are of different widths
            {    // than the vertical lines (due to the output device having different horizontal and vertical DPIs)

              // square the end caps so that the lines join together
              pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Square;

              // TODO: i tried rewriting the whole layout engine to use inches rather than pixels, to enable using
              // DrawRectangle with a device-independent PageUnit, but that just ended up being very ugly because of
              // GDI+ bugs that screw up the dash style even more than usual when using non-integer dimensions

              // draw the left border
              if(NodePart == NodePart.Full || NodePart == NodePart.Start)
              {
                pen.Width = Border.Width;
                data.Graphics.DrawLine(pen, borderRect.Left, borderRect.Bottom, borderRect.Left, borderRect.Top);
              }
              pen.DashOffset += borderRect.Height;

              // draw the top border
              pen.Width = Border.Height;
              data.Graphics.DrawLine(pen, borderRect.Left, borderRect.Top, borderRect.Right, borderRect.Top);
              pen.DashOffset += borderRect.Width;

              // draw the right border
              if(NodePart == NodePart.Full || NodePart == NodePart.End)
              {
                pen.Width = Border.Width;
                data.Graphics.DrawLine(pen, borderRect.Right, borderRect.Top, borderRect.Right, borderRect.Bottom);
              }
              pen.DashOffset += borderRect.Height;

              // draw the bottom border
              pen.Width = Border.Height;
              data.Graphics.DrawLine(pen, borderRect.Right, borderRect.Bottom, borderRect.Left, borderRect.Bottom);
            }
          }
        }
      }
    }
  }
  #endregion

  #region LayoutRegion<ChildType>
  /// <summary>Represents a <see cref="LayoutRegion"/> with a strongly-typed array of child regions.</summary>
  /// <typeparam name="ChildType">The type of child region this region contains.</typeparam>
  protected abstract class LayoutRegion<ChildType> : LayoutRegion where ChildType : LayoutRegion
  {
    /// <summary>Initializes this <see cref="LayoutRegion{T}"/> with a null child array.</summary>
    public LayoutRegion() { }
    /// <summary>Initializes this <see cref="LayoutRegion{T}"/> with the given child array.</summary>
    public LayoutRegion(ChildType[] children) { Children = children; }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/node()"/>
    public sealed override LayoutRegion[] GetChildren()
    {
      return Children;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/node()"/>
    public ChildType[] Children;
  }
  #endregion

  #region Block
  /// <summary>Represents a block region, whose children are stacked vertically.</summary>
  protected class Block : LayoutRegion<LayoutRegion>
  {
    /// <summary>Initializes a new <see cref="Block"/> with a null child array.</summary>
    public Block() { }
    /// <summary>Initializes a new <see cref="Block"/> with the given child array.</summary>
    public Block(LayoutRegion[] children) : base(children) { }
  }
  #endregion

  #region Block<NodeType>
  /// <summary>Represents a block region with a strongly-typed document node.</summary>
  protected class Block<NodeType> : Block where NodeType : DocumentNode
  {
    /// <summary>Initializes a new <see cref="Block"/> with a null child array.</summary>
    public Block(NodeType node) { Node = node; }
    /// <summary>Initializes a new <see cref="Block"/> with the given child array.</summary>
    public Block(NodeType node, LayoutRegion[] children) : base(children) { Node = node; }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNode/node()"/>
    public sealed override DocumentNode GetNode()
    {
      return Node;
    }

    /// <summary>The <see cref="DocumentNode"/> associated with this <see cref="Block{T}"/>.</summary>
    protected readonly NodeType Node;
  }
  #endregion

  #region Line
  /// <summary>Represents a line region, whose children are <see cref="LayoutSpan"/> regions that are stacked
  /// horizontally, and which render a portion of a span of inline document nodes.
  /// </summary>
  protected sealed class Line : LayoutRegion<LayoutSpan>
  {
    /// <summary>Initializes a new <see cref="Line"/> with a null child array.</summary>
    public Line() { }
    /// <summary>Initializes a new <see cref="Line"/> with the given child array.</summary>
    public Line(LayoutSpan[] children) : base(children) { }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/CalculateDistance/node()"/>
    /// <remarks>Calculates the distance, considering horizontal positioning only.</remarks>
    protected override uint CalculateDistance(Point pt, Rectangle rect)
    {
      return (uint)(pt.X < rect.Left ? rect.Left-pt.X : pt.X >= rect.Right ? pt.X-rect.Right+1 : 0);
    }
  }
  #endregion

  #region LineBlock
  /// <summary>Represents a line block region, whose children are <see cref="Line"/> regions that are stacked
  /// vertically, and which render a span of inline document nodes.
  /// </summary>
  protected sealed class LineBlock : LayoutRegion<Line>
  {
    /// <summary>Initializes this <see cref="LineBlock"/> with the given array of <see cref="Line"/> children.</summary>
    public LineBlock(Line[] children) : base(children) { }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNearestIndex/node()"/>
    public override int GetNearestIndex(Graphics gdi, Point regionPt)
    {
      // clip the point to be within the region horizontally so that the child lines will be considered by the base
      // implementation even if the cursor is to the left or right of a line
      if(regionPt.X < 0) regionPt.X = 0;
      else if(regionPt.X >= Width) regionPt.X = Width-1;

      return base.GetNearestIndex(gdi, regionPt);
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
    /// <summary>Gets or sets the number of pixels from the baseline to the bottom of the region.</summary>
    public int Descent;

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/node()"/>
    public virtual int LineCount
    {
      get { return 1; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/node()"/>
    public abstract LayoutSpan CreateNew();

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/node()"/>
    public sealed override LayoutRegion[] GetChildren()
    {
      return Children;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNearestIndex/node()"/>
    public override int GetNearestIndex(Graphics gdi, Point regionPt)
    {
      Rectangle paddingRect = PaddingArea;
      if(regionPt.Y < paddingRect.Top) regionPt.Y = paddingRect.Top;
      else if(regionPt.Y >= paddingRect.Bottom) regionPt.Y = paddingRect.Bottom-1;
      return base.GetNearestIndex(gdi, regionPt);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/Split/node()"/>
    public abstract IEnumerable<SplitPiece> Split(Graphics gdi, int line);

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/node()"/>
    public LayoutSpan[] Children = NoChildren;

    static readonly LayoutSpan[] NoChildren = new LayoutSpan[0];
  }
  #endregion

  #region ContainerSpan
  /// <summary>A generic span used to render an inline container node.</summary>
  protected class ContainerSpan<NodeType> : LayoutSpan<NodeType> where NodeType : DocumentNode
  {
    /// <summary>Initializes a new <see cref="ContainerSpan{T}"/> with the given node.</summary>
    public ContainerSpan(NodeType node) : base(node) { }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/node()"/>
    public override int LineCount
    {
      get { return 0; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/node()"/>
    public override LayoutSpan CreateNew()
    {
      return new ContainerSpan<NodeType>(Node);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/Split/node()"/>
    public override IEnumerable<SplitPiece> Split(Graphics gdi, int line)
    {
      throw new NotSupportedException();
    }
  }
  #endregion

  #region LayoutSpan<NodeType>
  /// <summary>Represents a <see cref="LayoutSpan"/> with a strongly-typed <see cref="DocumentNode"/>.</summary>
  protected abstract class LayoutSpan<NodeType> : LayoutSpan where NodeType : DocumentNode
  {
    /// <summary>Initializes the <see cref="LayoutSpan"/> with the given document node.</summary>
    protected LayoutSpan(NodeType node)
    {
      if(node == null) throw new ArgumentNullException();
      Node = node;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNode/node()"/>
    public sealed override DocumentNode GetNode()
    {
      return Node;
    }

    /// <summary>The <see cref="DocumentNode"/> associated with this <see cref="LayoutSpan{T}"/>.</summary>
    protected readonly NodeType Node;
  }
  #endregion

  #region RootRegion
  /// <summary>A region that wraps the region created for the document root node, and provides the ability to reference
  /// the end of the document.
  /// </summary>
  sealed class RootRegion : Block
  {
    public RootRegion(LayoutRegion child) : base(new LayoutRegion[] { child })
    {
      // as a special case, if the last child of the given region is a LineBlock, decrease the length of the lineblock
      // and its parents by one, because otherwise the last cursor position would have two indices
      LayoutRegion[] children = child.GetChildren();
      if(children.Length != 0)
      {
        LineBlock block = children[children.Length-1] as LineBlock;
        if(block != null)
        {
          Line lastLine = block.Children[block.Children.Length-1];
          if(lastLine.Children.Length != 0) lastLine.Children[lastLine.Children.Length-1].Length--;
          lastLine.Length--;
          block.Length--;
          child.Length--;
        }
      }

      Span = child.Span;
      Size = child.Size;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetRegion/node()"/>
    public override LayoutRegion GetRegion(int index)
    {
      if(index == Length) // if the index is at the end of the document, normally no region would contain it. but we
      {                   // want the end of the document to be a valid index, so we'll return something
        LayoutRegion[] children = Children[0].GetChildren();
        LayoutRegion lastChild = children.Length == 0 ? null : children[children.Length-1];

        // if there no children under the root, just return the root. otherwise, return the last child, but if it's
        // a LineBlock, drill down into the last span of the last line
        if(lastChild == null) lastChild = Children[0];
        else
        {
          LineBlock block = lastChild as LineBlock;
          if(block != null)
          {
            Line lastLine = block.Children[block.Children.Length-1];
            lastChild = lastLine.Children.Length == 0 ?
              (LayoutRegion)lastLine : lastLine.Children[lastLine.Children.Length-1];
          }
        }
        return lastChild;
      }
      else return Children[0].GetRegion(index); // otherwise, just defer to the wrapped region
    }
  }
  #endregion

  #region FourSideInt
  /// <summary>Represents the four measures of a four-sided object, like a margin, in pixels.</summary>
  protected struct FourSideInt
  {
    /// <summary>Initializes this <see cref="FourSideInt"/> with the given pixel dimensions.</summary>
    public FourSideInt(int left, int top, int right, int bottom)
    {
      Left   = left;
      Top    = top;
      Right  = right;
      Bottom = bottom;
    }

    /// <summary>Returns the total horizontal measurement of the object, equal to <see cref="Left"/> +
    /// <see cref="Right"/>.
    /// </summary>
    public int TotalHorizontal
    {
      get { return Left + Right; }
    }

    /// <summary>Returns the total vertical measurement of the object, equal to <see cref="Top"/> +
    /// <see cref="Bottom"/>.
    /// </summary>
    public int TotalVertical
    {
      get { return Top + Bottom; }
    }

    /// <summary>Gets or sets the measurment of the left side of the object, in pixels.</summary>
    public int Left;
    /// <summary>Gets or sets the measurment of the top side of the object, in pixels.</summary>
    public int Top;
    /// <summary>Gets or sets the measurment of the right side of the object, in pixels.</summary>
    public int Right;
    /// <summary>Gets or sets the measurment of the bottom side of the object, in pixels.</summary>
    public int Bottom;
  }
  #endregion

  #region NodePart
  /// <summary>Describes the portion of the <see cref="DocumentNode"/> covered by this <see cref="LayoutRegion"/>.</summary>
  protected enum NodePart : byte
  {
    /// <summary>The layout region covers all four sides of the document node.</summary>
    Full,
    /// <summary>The layout region covers only the top, left, and bottom sides of the document node, because
    /// the node was wrapped onto at least two lines (and this is the first region of the node).
    /// </summary>
    Start,
    /// <summary>The layout region covers only the top and bottom sides of the document node, because the node
    /// was wrapped onto at least three lines (and this is one of the middle regions).
    /// </summary>
    Middle,
    /// <summary>The layout region covers only the top, right, and bottom sides of the document node, because
    /// the node was wrapped onto at least two lines (and this is the last region of the node).
    /// </summary>
    End
  }
  #endregion

  #region RenderData
  /// <summary>Contains render-related data that does not change during a rendering.</summary>
  protected struct RenderData
  {
    /// <summary>Initializes a new <see cref="RenderData"/> structure with the given graphics context, clipping
    /// rectangle, and span of selected document indices.
    /// </summary>
    public RenderData(Graphics graphics, DocumentEditor editor, Rectangle clipRect, Span selection)
    {
      Graphics      = graphics;
      Editor        = editor;
      ClipRectangle = clipRect;
      Selection     = selection;
    }

    /// <summary>The graphics context used to render the document.</summary>
    public readonly Graphics Graphics;
    /// <summary>The editor that is being rendered.</summary>
    public readonly DocumentEditor Editor;
    /// <summary>The rectangle in which all rendering output should be contained, in client coordinates.</summary>
    public readonly Rectangle ClipRectangle;
    /// <summary>The span of document indices that are selected in the control.</summary>
    public readonly Span Selection;
  }
  #endregion

  #region PieceType
  /// <summary>Represents the type of a <see cref="SplitPiece"/>.</summary>
  protected enum PieceType
  {
    /// <summary>The split piece is a word. The line wrapping algorithm will wrap at word boundaries.</summary>
    Word,
    /// <summary>The split piece is a piece of whitespace that trails after a word and must not be separated from the
    /// word unless horizontal justification is being performed. These spaces are the ones that will be stretched by
    /// justification.
    /// </summary>
    TrailingSpace,
    /// <summary>The split piece is a piece of whitespace between words. The whitespace may be split from the preceding
    /// word and placed on the next line, but will not be stretched by justification.
    /// </summary>
    Space,
    /// <summary>The split piece represents a span of the node content that will be skipped, although its height will
    /// still affect the height of the line that would have contained it.
    /// </summary>
    Skip
  }
  #endregion

  #region SplitPiece
  /// <summary>Represents a piece of content from a document node, that is used by the line wrapping to decide where
  /// to break lines.
  /// </summary>
  protected sealed class SplitPiece
  {
    /// <summary>Initializes a new <see cref="SplitPiece"/> with the given size, content length, and type.</summary>
    public SplitPiece(Size pixelSize, int contentLength, PieceType type)
    {
      PixelSize     = pixelSize;
      ContentLength = contentLength;
      Type          = type;
    }

    /// <summary>The size of the piece when rendered, in pixels.</summary>
    public Size PixelSize;
    /// <summary>The length of the content from the document that this piece represents.</summary>
    public int ContentLength;
    /// <summary>The type of the piece, which determines how it will be treated by the line wrapping algorithm.</summary>
    public PieceType Type;
    /// <summary>The span that generated the piece.</summary>
    internal LayoutSpan Container;
  }
  #endregion

  #region TextNodeSpan
  /// <summary>Represents a <see cref="LayoutSpan"/> to render a <see cref="TextNode"/>.</summary>
  protected class TextNodeSpan : LayoutSpan<TextNode>
  {
    /// <summary>Initializes this <see cref="TextNodeSpan"/> given the owning <see cref="DocumentEditor"/>, and a
    /// <see cref="TextNode"/> to render.
    /// </summary>
    public TextNodeSpan(TextNode node, DocumentEditor editor) : this(node, editor, editor.GetEffectiveFont(node)) { }

    TextNodeSpan(TextNode node, DocumentEditor editor, Font font) : base(node)
    {
      Editor = editor;
      Font   = font;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/node()"/>
    public override int LineCount
    {
      get { return Node.LineCount; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/BeginLayout/node()"/>
    public override void BeginLayout(Graphics gdi)
    {
      base.BeginLayout(gdi);

      // calculate the font descent, so that adjacent spans of text with different fonts render on the same baseline
      float height = Font.GetHeight(gdi);
      Descent = (int)Math.Round(height - (height * Font.FontFamily.GetCellAscent(Font.Style) /
                                          (float)Font.FontFamily.GetLineSpacing(Font.Style)));
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/node()"/>
    public override LayoutSpan CreateNew()
    {
      return new TextNodeSpan(Node, Editor, Font);
    }

    /// <include file="documentation.xml" path="/UI/Common/Dispose/node()"/>
    public override void Dispose()
    {
      base.Dispose();
      Font.Dispose();
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetCursorHeight/node()"/>
    public override int GetCursorHeight(int index)
    {
      return Math.Max(ContentArea.Height, 1);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetPixelOffset/node()"/>
    public override int GetNearestIndex(Graphics gdi, Point regionPt)
    {
      int end = DocumentContentSpan.End;

      // if the point is not contained within the region horizontally, return the start or end depending on where it is
      Rectangle paddingArea = PaddingArea;
      if(regionPt.X < paddingArea.Left) return Start;
      else if(regionPt.X >= paddingArea.Right) return end;

      // this clever code works by taking advantage of the ability of the TextRenderer to modify a string by replacing
      // the part of a string that wouldn't fit into a rectangle with an ellipsis. by giving a rectangle with a width
      // equal to the space up to the region point and checking for the presence of the ellipsis, we can figure out how
      // many characters fit. (it'd be nicer if the text renderer exposed this feature directly.)

      const TextFormatFlags MeasureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
                                           TextFormatFlags.SingleLine;
      if(cachedEllipsisWidth == -1) // figure out and cache the width of the ellipsis
      {
        cachedEllipsisWidth = TextRenderer.MeasureText(gdi, "...", Font, new Size(int.MaxValue, int.MaxValue),
                                                       MeasureFlags).Width;
      }

      // make sure we construct a new string object so the MeasureText call doesn't modify an existing one.
      // also, add four spaces at the end so the MeasureText function will have enough room to add the ellipsis.
      char[] chars = new char[ContentLength+4];
      Node.CopyText(ContentStart, chars, 0, ContentLength);
      for(int i=ContentLength; i<chars.Length; i++) chars[i] = ' '; // fill the rest of the text with spaces
      string text = new string(chars);

      regionPt.X -= LeftPBM; // account for the border, margin, and padding

      int fitWidth = TextRenderer.MeasureText(gdi, text, Font,
                       new Size(regionPt.X + cachedEllipsisWidth, int.MaxValue),
                       MeasureFlags | TextFormatFlags.ModifyString | TextFormatFlags.EndEllipsis).Width;

      // check for the presence of the ellipsis and calculate the number of characters that fit. unfortunately, this
      // code doesn't work if the text contains embedded nul characters, but it's likely that neither does the text
      // renderer...

      int nulPos = text.IndexOf('\0'); // if the string was modified, it will have "...\0" inserted
      if(nulPos == -1) return end; // if there is no ellipsis, then all characters fit.

      int charactersFit = nulPos-3;

      // unfortunately, on Windows at least, the TextRenderer puts in at least one character even if there's no room
      // for it, so we need to treat that as suspect and confirm that it actually fits
      if(charactersFit == 1)
      {
        int charWidth = TextRenderer.MeasureText(gdi, new string(chars[0], 1), Font,
                                                 new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width;
        if(regionPt.X < charWidth/2) return Start; // if the point is before the center of the character, it didn't fit
      }

      if(charactersFit == ContentLength)
      {
        return end;
      }
      else
      {
        // at this point, some number of characters fit, but we want to be a bit more lenient, and allow the user to
        // click anywhere after the midpoint of a character to accept that character as having fit. so we'll measure
        // the next character and decide if we should include it as well.

        // remove the width of the ellipsis from the calculation, so that 'fitWidth' is equal to the length of the
        // string containing the first 'charactersFit' characters
        fitWidth -= cachedEllipsisWidth;

        // now we'll measure the next character and see if the point was at or beyond its midpoint
        int charWidth = TextRenderer.MeasureText(gdi, new string(chars[charactersFit], 1), Font,
                                                 new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width;
        if(regionPt.X >= fitWidth+charWidth/2) charactersFit++;
      }

      return Start + charactersFit;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNearestIndex/node()"/>
    public override Point GetPixelOffset(Graphics gdi, int indexOffset)
    {
      if((uint)indexOffset > (uint)Length) throw new ArgumentOutOfRangeException();

      int xPos;
      if(indexOffset >= ContentLength)
      {
        // TODO: FIXME: if the side of the region is flush with the canvas, the cursor will be off-canvas and disappear :-(
        xPos = Width - RightPBM;
      }
      else
      {
        xPos = (indexOffset == 0 ? 0 : MeasureWidth(gdi, Node.GetText(ContentStart, indexOffset))) + LeftPBM;
      }

      return new Point(xPos, TopPBM);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/Split/node()"/>
    public override IEnumerable<SplitPiece> Split(Graphics gdi, int line)
    {
      int lineOffset, lineLength, fontHeight = (int)Math.Ceiling(Font.GetHeight(gdi));
      Node.GetLineInfo(line, out lineOffset, out lineLength);
      string text = Node.GetText(lineOffset, lineLength);

      int skipChars = text.Length != 0 && text[text.Length-1] == '\n' ? 1 : 0;
      bool pieceReturned = false;
      for(Match match = wordRE.Match(text, 0, text.Length - skipChars);
          match.Success; match = match.NextMatch())
      {
        string word = match.Value;

        if(!char.IsWhiteSpace(word[word.Length-1])) // the last character is not whitespace, so it's a single word
        {
          yield return new SplitPiece(new Size(MeasureWidth(gdi, word), fontHeight), word.Length, PieceType.Word);
        }
        else if(word.Length > 1 && !char.IsWhiteSpace(word[word.Length-2])) // it's a word followed by a space
        {
          yield return new SplitPiece(new Size(MeasureWidth(gdi, word.Substring(0, word.Length-1)), fontHeight),
                                      word.Length-1, PieceType.Word);
          yield return new SplitPiece(new Size(MeasureWidth(gdi, new string(word[word.Length-1], 1)), fontHeight),
                                      1, PieceType.TrailingSpace);
        }
        else // it's all space, so allow the text to be split at each character
        {
          for(int lastWidth=0, i=0; i<word.Length; i++)
          {
            // the space is likely to be composed of a run of the repeated spaces, so cache the last measurement
            int charWidth = i != 0 || word[i] == word[i-1] ? lastWidth : MeasureWidth(gdi, new string(word[i], 1));
            yield return new SplitPiece(new Size(charWidth, fontHeight), 1, PieceType.Space);
            lastWidth = charWidth;
          }
        }

        pieceReturned = true;
      }

      // if there are characters to skip, or if we haven't returned anything, return a piece with just a height
      if(skipChars != 0 || !pieceReturned)
      {
        yield return new SplitPiece(new Size(0, fontHeight), skipChars, PieceType.Skip);
      }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Render/node()"/>
    public override void Render(ref RenderData data, Point clientPoint)
    {
      RenderBackgroundAndBorder(ref data, clientPoint);

      const TextFormatFlags DrawFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
                                        TextFormatFlags.SingleLine;

      clientPoint.Offset(LeftPBM, TopPBM); // account for the border, margin, and padding

      string text = Node.GetText(ContentStart, ContentLength);
      Span contentSpan = DocumentContentSpan;

      if(!data.Selection.Intersects(contentSpan)) // if the text is completely outside the selection...
      {
        Color fore = Node.Style.EffectiveForeColor ?? Editor.ForeColor;
        TextRenderer.DrawText(data.Graphics, text, Font, clientPoint, fore, DrawFlags);
      }
      else if(data.Selection.Contains(contentSpan)) // if the text is completely inside the selection...
      {
        TextRenderer.DrawText(data.Graphics, text, Font, clientPoint,
                              SystemColors.HighlightText, SystemColors.Highlight, DrawFlags);
      }
      else // if the text is partly inside and partly outside
      {
        Color fore = Node.Style.EffectiveForeColor ?? Editor.ForeColor;
        // draw the portion of the text before the selection
        if(Start < data.Selection.Start)
        {
          string subText = text.Substring(0, data.Selection.Start - Start);
          TextRenderer.DrawText(data.Graphics, subText, Font, clientPoint, fore, DrawFlags);
          clientPoint.X += MeasureWidth(data.Graphics, subText);
        }
        // draw the portion within the selection
        {
          string subText = text.Substring(Math.Max(0, data.Selection.Start - Start),
                             Math.Min(contentSpan.End, data.Selection.End) - Math.Max(data.Selection.Start, Start));
          TextRenderer.DrawText(data.Graphics, subText, Font, clientPoint,
                                SystemColors.HighlightText, SystemColors.Highlight, DrawFlags);
          clientPoint.X += MeasureWidth(data.Graphics, subText);
        }
        // draw the portion after the selection
        if(contentSpan.End > data.Selection.End)
        {
          string subText = text.Substring(data.Selection.End - Start, contentSpan.End - data.Selection.End);
          TextRenderer.DrawText(data.Graphics, subText, Font, clientPoint, fore, DrawFlags);
        }
      }
    }

    int MeasureWidth(Graphics gdi, string text)
    {
      return TextRenderer.MeasureText(gdi, text, Font, new Size(int.MaxValue, int.MaxValue),
        TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
    }

    readonly Font Font;
    readonly DocumentEditor Editor;
    /// <summary>Holds the width of an ellipsis drawn with the <see cref="Font"/>.</summary>
    int cachedEllipsisWidth = -1;

    // TODO: implement splitting at hyphens
    static readonly Regex wordRE = new Regex(@"\s*\S+\s?|\s+", RegexOptions.Singleline | RegexOptions.Compiled);
  }
  #endregion

  /// <summary>Given a block <see cref="DocumentNode"/>, creates and returns an appropriate <see cref="Block"/> to
  /// render the node. This method must not return null, but can use a generic <see cref="Block"/> for unknown document
  /// nodes.
  /// </summary>
  protected virtual Block CreateLayoutBlock(DocumentNode node)
  {
    return new Block<DocumentNode>(node);
  }

  /// <summary>Given an inline <see cref="DocumentNode"/>, creates and returns an appropriate <see cref="LayoutSpan"/>
  /// to render the node, or null if the node cannot be rendered. The layout span should render starting from the
  /// beginning of the document node.
  /// </summary>
  protected virtual LayoutSpan CreateLayoutSpan(DocumentNode node)
  {
    TextNode textNode = node as TextNode;
    if(textNode != null) return new TextNodeSpan(textNode, this);

    if(node.Children.Count != 0) return new ContainerSpan<DocumentNode>(node);

    return null;
  }

  #region Container
  /// <summary>A helper class used during layout to represent a span and whether its left and right PBM have been
  /// considered yet.
  /// </summary>
  new sealed class Container
  {
    public Container(LayoutRegion span)
    {
      Span = span;
    }

    public readonly LayoutRegion Span;
    /// <summary>A value indicating whether the left PBM of the span has been output yet.</summary>
    public bool OutputLeft;
    /// <summary>A value indicating whether the right PBM of the span has pushed onto the next line.</summary>
    public bool BrokeRight;
  }
  #endregion

  #region PieceEnumerator
  /// <summary>A class that takes a list of node trees and returns lists of <see cref="SplitPiece"/> objects
  /// corresponding to the natural line breaks within the content.
  /// </summary>
  sealed class PieceEnumerator
  {
    public PieceEnumerator(DocumentEditor owner, Graphics gdi, IEnumerable<DocumentNode> nodes, Size available)
    {
      this.owner     = owner;
      this.gdi       = gdi;
      this.states    = new AC.Stack<State>();
      this.available = available;

      states.Push(new State(nodes));
    }

    /// <summary>Gets whether all pieces have been enumerated.</summary>
    public bool IsDone
    {
      get { return states.Count == 0; }
    }

    /// <summary>Enumerates the pieces in the next line of document content.</summary>
    public IEnumerable<SplitPiece> GetNextLine()
    {
      while(!IsDone)
      {
        State state = states.Peek();

        if(state.Span == null || state.LineIndex == state.Span.LineCount)
        {
          if(!state.Nodes.MoveNext())
          {
            states.Pop();
            continue;
          }

          state.Span = owner.CreateLayoutSpan(state.Nodes.Current);
          if(state.Span == null) continue;

          owner.SetBorderMarginAndPadding(gdi, state.Nodes.Current, state.Span, available);
          state.LineIndex = 0;

          if(state.Nodes.Current.Children.Count != 0)
          {
            states.Push(new State(state.Nodes.Current.Children));
            continue;
          }
        }

        if(states.Count > 1) state.Span.Parent = states[states.Count-2].Span;

        foreach(SplitPiece piece in state.Span.Split(gdi, state.LineIndex++))
        {
          piece.Container = state.Span;
          yield return piece;
        }

        if(state.LineIndex != state.Span.LineCount) break;
      }
    }

    #region State
    sealed class State
    {
      public State(IEnumerable<DocumentNode> nodes)
      {
        Nodes = nodes.GetEnumerator();
      }

      public readonly IEnumerator<DocumentNode> Nodes;
      public LayoutSpan Span;
      public int LineIndex;
    }
    #endregion

    readonly DocumentEditor owner;
    readonly Graphics gdi;
    readonly AC.Stack<State> states;
    readonly Size available;
  }
  #endregion

  /// <summary>Applies the given horizontal alignment value to the region's children.</summary>
  void ApplyHorizontalAlignment(HorizontalAlignment alignment, LayoutRegion region)
  {
    // we only handle right and center because things are left-aligned by default
    if(alignment == HorizontalAlignment.Right)
    {
      int contentRight = region.Bounds.Right - region.Border.Width - region.Margin.Right - region.Padding.Right;
      foreach(LayoutRegion child in region.GetChildren()) child.Left += contentRight - child.Bounds.Right;
    }
    else if(alignment == HorizontalAlignment.Center)
    {
      int leftOffset = region.Border.Width + region.Margin.Left + region.Padding.Left;
      int contentWidth = region.Width - leftOffset - region.Border.Width - region.Margin.Right - region.Padding.Right;
      foreach(LayoutRegion child in region.GetChildren()) child.Left = leftOffset + (contentWidth - child.Width) / 2;
    }
  }

  /// <summary>Creates a layout block given the rendering context, a document node, and the width available in pixels.
  /// </summary>
  // TODO: we should virtualize CreateBlock and LayoutLines to allow the layout logic to be overridden
  Block CreateBlock(Graphics gdi, DocumentNode node, Size available, ref int startIndex)
  {
    Block newBlock = CreateLayoutBlock(node);
    newBlock.BeginLayout(gdi);

    // see if the node has a defined size. if it does, set the available size. later we'll actually set the node size.
    Measurement? nodeWidth = node.Style.Width, nodeHeight = node.Style.Height;
    if(nodeWidth.HasValue)
    {
      available.Width = GetPixels(gdi, node, nodeWidth.Value, available.Width, Orientation.Horizontal);
    }
    if(nodeHeight.HasValue)
    {
      available.Height = GetPixels(gdi, node, nodeHeight.Value, available.Height, Orientation.Vertical);
    }

    // calculate the horizontal margin and padding
    Size shrinkage = SetBorderMarginAndPadding(gdi, node, newBlock, available);

    // calculate the size available for child controls. the available height doesn't shrink the same way as the width
    // because we can always make the document taller, but we don't want to make it wider.
    Size shrunkSize = new Size(available.Width - shrinkage.Width, available.Height);

    // now, determine whether any of the children are block nodes
    bool noBlocks = true;
    foreach(DocumentNode child in node.Children)
    {
      if((child.Layout & RichDocument.Layout.Block) != 0) // if the child is a block node
      {
        noBlocks = false;
        break;
      }
    }

    // if the node is not the root of the document, create an index for the block so the cursor can be placed in front
    // of it. (it doesn't make sense for the cursor to be in front of the root of the document...)
    if(node != Document.Root) newBlock.Start = startIndex++;

    if(noBlocks) // if there are no block children, lay them all out horizontally into a line
    {
      LineBlock lines = LayoutLines(gdi, node, node.Children, shrunkSize, ref startIndex);
      if(lines == null)
      {
        newBlock.Children = new LayoutRegion[0];
        // if the block had no content, add an index inside it to allow content to be placed inside
        if(node != Document.Root) startIndex++;
      }
      else
      {
        newBlock.Children = new LayoutRegion[] { lines };
        newBlock.Size     = lines.Size;
      }
    }
    else // otherwise, one or more nodes is a block
    {
      // TODO: implement float and clear

      // create a list of blocks by using block nodes as-is and gathering runs of inline nodes into a line block
      List<LayoutRegion> blocks = new List<LayoutRegion>(node.Children.Count);
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
            blocks.Add(LayoutLines(gdi, node, inline, shrunkSize, ref startIndex));
            inline.Clear();
          }

          Block block = CreateBlock(gdi, child, shrunkSize, ref startIndex);
          block.Length++;    // add a virtual newline to the end of the block, to allow the cursor to be positioned at
          startIndex++;      // the left or right side of it. we actually don't want this for the last position, but
          blocks.Add(block); // we'll fix that later (below)
        }
      }

      // if we have some inline nodes left, create a final line block to hold them
      if(inline != null && inline.Count != 0)
      {
        LineBlock lines = LayoutLines(gdi, node, inline, shrunkSize, ref startIndex);
        if(lines != null) blocks.Add(lines);
      }
      else if(node == Document.Root) // if the final child was a non-line block and this is the root node, then we
      {                              // added too many document indices. we'll remove one.
        blocks[blocks.Count-1].Length--;
        startIndex--;
      }

      newBlock.Children = blocks.ToArray();

      // go through the blocks and stack them up, while simultaneously calculating the size of the container
      foreach(LayoutRegion child in newBlock.Children)
      {
        int effectiveChildHeight = child.Top + child.Height; // from the top of the block to the bottom of the child...
        child.Top += newBlock.Height; // stick the block at the bottom of the stack

        // if the block is wider than the container, widen the container
        if(child.Bounds.Right > newBlock.Bounds.Right) newBlock.Width = child.Bounds.Right - newBlock.Left;

        newBlock.Height += effectiveChildHeight; // enlarge the container vertically
      }
    }

    newBlock.Length = startIndex - newBlock.Start; // calculate the span of the container
    newBlock.ContentSpan = newBlock.Span; // and set the content span

    // now that the container is exactly the size of its content, we can go back and apply margin and padding
    if(!shrinkage.IsEmpty)
    {
      int leftAdd = newBlock.Margin.Left + newBlock.Padding.Left + newBlock.Border.Width;
      int  topAdd = newBlock.Margin.Top  + newBlock.Padding.Top  + newBlock.Border.Height;
      foreach(LayoutRegion child in newBlock.Children)
      {
        child.Left += leftAdd;
        child.Top  += topAdd;
      }
      newBlock.Width  += shrinkage.Width;
      newBlock.Height += shrinkage.Height;
    }

    // now we'll force the node size if it is defined, but won't go smaller than the content
    if(nodeWidth.HasValue) newBlock.Width = Math.Max(newBlock.Width, available.Width);
    if(nodeHeight.HasValue) newBlock.Height = Math.Max(newBlock.Height, available.Height);

    ApplyHorizontalAlignment(node.Style.HorizontalAlignment, newBlock);
    return newBlock;
  }

  /// <summary>Performs the layout of the document and returns the layout, represented by a <see cref="RootRegion"/>
  /// object.
  /// </summary>
  RootRegion CreateRootBlock(Graphics gdi)
  {
    int startIndex = 0;
    RootRegion root = new RootRegion(CreateBlock(gdi, Document.Root, CanvasRectangle.Size, ref startIndex));
    root.EndLayout(gdi);
    return root;
  }

  /// <summary>Converts a <see cref="Measurement"/> into pixels.</summary>
  /// <param name="gdi">The graphics device.</param>
  /// <param name="node">The <see cref="DocumentNode"/> to which the measurement is related.</param>
  /// <param name="measurement">The measurement.</param>
  /// <param name="parentPixels">The size of the parent measurement, used in relative calculations.</param>
  /// <param name="orientation">The orientation of the measurement.</param>
  int GetPixels(Graphics gdi, DocumentNode node, Measurement measurement, int parentPixels, Orientation orientation)
  {
    if(measurement.Size == 0) return 0;

    float pixels;
    switch(measurement.Unit)
    {
      case Unit.FontRelative:
        Font font = GetEffectiveFont(node);
        pixels = measurement.Size * GetPixels(gdi, font.Size, font.Unit, orientation);
        break;

      case Unit.Inches:
        pixels = GetPixels(gdi, measurement.Size, GraphicsUnit.Inch, orientation);
        break;

      case Unit.Millimeters:
        pixels = GetPixels(gdi, measurement.Size, GraphicsUnit.Millimeter, orientation);
        break;

      case Unit.Percent:
        pixels = measurement.Size * 0.01f * parentPixels;
        break;

      case Unit.Pixels:
        pixels = measurement.Size;
        break;

      case Unit.Points:
        pixels = GetPixels(gdi, measurement.Size, GraphicsUnit.Point, orientation);
        break;

      default: throw new NotImplementedException("Measurement unit "+measurement.Unit.ToString()+" not implemented.");
    }

    return (int)Math.Round(pixels);
  }

  /// <summary>Creates a line block given the rendering context, the inline and content nodes to place into the block,
  /// and the width available, in pixels.
  /// </summary>
  LineBlock LayoutLines(Graphics gdi, DocumentNode parent, ICollection<DocumentNode> inlineNodes,
                        Size available, ref int startIndex)
  {
    // TODO: FIXME: make sure this code can handle a very narrow (insufficient) available width

    // a dictionary mapping document nodes to the spans that render the node
    Dictionary<DocumentNode, List<LayoutSpan>> nodeSpans = new Dictionary<DocumentNode, List<LayoutSpan>>();
    List<Line> lines = new List<Line>(); // the final output of lines

    List<LayoutSpan> spans = new List<LayoutSpan>(); // the leaf spans in the current output line
    List<SplitPiece> pieces = new List<SplitPiece>(); // the pieces in the current content line
    // lists containing the span of the current piece and its ancestors
    List<Container> ancestors = new List<Container>();
    List<LayoutRegion> newAncestors = new List<LayoutRegion>();
    LayoutSpan span = null; // the span of the current piece
    int nodeIndex = 0;      // the index within the content of the current document node
    HorizontalAlignment hAlignment = parent.Style.HorizontalAlignment;

    // an enumerator that turns a list of node trees into sets of spans for each line of content
    PieceEnumerator pieceEnum = new PieceEnumerator(this, gdi, inlineNodes, available);
    while(!pieceEnum.IsDone) // while there are lines of content remaining
    {
      pieces.AddRange(pieceEnum.GetNextLine()); // get the pieces in the line
      if(pieces.Count == 0) continue; // if there are no pieces at all (not even height placeholders), skip this line

      Size lineSize = new Size(); // the size of the output current line so far
      int availableWidth = available.Width; // the amount of space left in the current line
      span = ResetSpan(span, startIndex, nodeIndex); // reset the size and length of the span for the new line

      for(int pieceIndex=0,nextIndex=0; pieceIndex<pieces.Count; pieceIndex=nextIndex)
      {
        // get the size and content length of the current piece
        SplitPiece piece = pieces[nextIndex++];
        Size spaceNeeded = piece.PixelSize;
        int  docLength   = piece.ContentLength;

        // if the piece's span changed, update the set of ancestors for the current span and the available width
        if(piece.Container != span)
        {
          FinishSpan(spans, span); // output the old span

          // fill 'newAncestors' with the new span and its ancestors
          FillAncestorList(newAncestors, piece.Container);
          newAncestors.Reverse();

          // now we have two ancestor lists. find the point of divergence and remove spans that are no longer used
          int i=0;
          for(int count=Math.Min(ancestors.Count, newAncestors.Count); i<count; i++)
          {
            if(newAncestors[i] != ancestors[i].Span)
            {
              ancestors.RemoveRange(i, ancestors.Count-i);
              break;
            }
          }

          // add the new ancestors and remove their horizontal PBM from 'availableWidth', giving a conservative
          // estimate of the amount of space available on the current line
          for(; i<newAncestors.Count; i++)
          {
            ancestors.Add(new Container(newAncestors[i]));
            availableWidth -= newAncestors[i].LeftPBM + newAncestors[i].RightPBM;
          }
          newAncestors.Clear(); // clear the 'newAncestors' list for use next time

          // if the span changed, it's because the node changed, so reset the nodeIndex
          nodeIndex = 0;
          span = ResetSpan(piece.Container, startIndex, nodeIndex); // set 'span' to the new span
        }

        // if the piece is a word, it may have trailing space attached. these spaces should be grouped along with it
        // unless we're doing justification
        if(hAlignment != HorizontalAlignment.Justify && piece.Type == PieceType.Word)
        {
          while(nextIndex < pieces.Count && pieces[nextIndex].Type == PieceType.TrailingSpace &&
                pieces[nextIndex].Container == span)
          {
            spaceNeeded.Width += pieces[nextIndex].PixelSize.Width;
            spaceNeeded.Height = Math.Min(spaceNeeded.Height, pieces[nextIndex].PixelSize.Height);
            docLength += pieces[nextIndex].ContentLength;
            nextIndex++;
          }
        }

        if(spaceNeeded.Width > availableWidth) // if the pieces don't fit
        {
          bool startNewLine = true;

          // if the current output line is not empty, try to add some more space by breaking the parent containers
          // onto the next line, causing the space taken by their right PBM to be freed up on the current line.
          // (if the line is empty, we're going to add the piece regardless, so there's no point.)
          if(lineSize.Width != 0)
          {
            // add back the right PBM from the outermost container inwards
            int containerI;
            for(containerI=0; containerI<ancestors.Count; containerI++)
            {
              if(!ancestors[containerI].BrokeRight)
              {
                availableWidth += ancestors[containerI].Span.RightPBM;
                ancestors[containerI].BrokeRight = true;
                startNewLine = spaceNeeded.Width > availableWidth;
                if(!startNewLine) break; // if adding the PBM allowed the piece to fit, then stop adding PBM
              }
            }

            // now, adding the PBM has the effect of breaking the parent container onto the next line. but we don't
            // want to do that if it would cause a sliver of border to be on its own line. so we have to make sure
            // that there's another word before the end of the the innermost container that added its right PBM.
            if(!startNewLine)
            {
              startNewLine = true;

              for(int i=nextIndex; i<pieces.Count; i++)
              {
                if(pieces[i].Type == PieceType.Word) // find the next word piece
                {
                  // see if the piece is within the innermost container that added its right PBM
                  for(LayoutRegion region = pieces[i].Container; region != null; region = region.Parent)
                  {
                    if(region == ancestors[containerI].Span)
                    {
                      startNewLine = false; // it is, so that's good
                      break;
                    }
                  }

                  break; // if the word is not within the container, then no others will be either, so we can stop now
                }
              }
            }
          }

          if(startNewLine) // if we need to finish the current line before adding the new pieces
          {
            FinishLine(gdi, lines, spans, span, nodeSpans, ref startIndex, ref lineSize); // then do so

            span = ResetSpan(span, startIndex, nodeIndex); // and reset the current span so it doesn't include content
                                                           // from the previous line

            // recalculate the available width for the new line. we'll conservatively remove the right PBM from all
            // containers and the left PBM for all that have not yet been involved in any output
            availableWidth = available.Width;
            foreach(Container container in ancestors)
            {
              container.BrokeRight = false;
              availableWidth -= container.Span.RightPBM;
              if(!container.OutputLeft) availableWidth -= container.Span.LeftPBM;
            }
          }
        }

        // pieces of skipped content don't add to the rendering, but they do affect the line height
        if(piece.Type == PieceType.Skip)
        {
          lineSize.Height = Math.Max(lineSize.Height, spaceNeeded.Height);
          span.Height     = Math.Max(span.Height, spaceNeeded.Height);
        }
        else
        {
          // at this point, a word is ready to be added, which "starts" all of its containing spans. we'll signify
          // this by marking that each container has output its left PBM
          foreach(Container container in ancestors) container.OutputLeft = true;

          // enlarge the line and span, and reduce the available width, by the size of the pieces
          lineSize  = new Size(lineSize.Width + spaceNeeded.Width, Math.Max(lineSize.Height, spaceNeeded.Height));
          span.Size = new Size(span.Width + spaceNeeded.Width, Math.Max(span.Height, spaceNeeded.Height));
          availableWidth -= spaceNeeded.Width;

          // update the content and document spans, as well as the starting index
          span.ContentLength += docLength;
          span.Length        += docLength;
          startIndex = span.End;
        }

        nodeIndex += docLength; // finally, update the node index regardless of the piece type
      }

      // at this point, all pieces have been added, so finish the line
      FinishLine(gdi, lines, spans, span, nodeSpans, ref startIndex, ref lineSize);

      pieces.Clear(); // clear the pieces list for the next line
    }

    // if there was no content, we can return null. there's no point in creating an empty LineBlock
    if(lines.Count == 0) return null;

    // now that all spans are processed, we have the information needed to set their NodePart members
    UpdateSpansWithPBM(nodeSpans);

    // now, loop through each of the lines that we've added and stack them vertically into a LineBlock
    LineBlock block = new LineBlock(lines.ToArray());
    block.BeginLayout(gdi);

    foreach(Line line in block.Children)
    {
      // calculate the line span
      if(line.Children.Length != 0)
      {
        line.Start  = line.Children[0].Start;
        line.Length = line.Children[line.Children.Length-1].End - line.Start;
      }
      line.ContentSpan = new Span(line.Start, line.Length-1); // account for the virtual newline in the content length

      // lay out all the line's children horizontally
      StackSpans(line, line.Children);

      // and position the line vertically within the block
      line.Position = new Point(0, block.Height);
      block.Width   = Math.Max(line.Width, block.Width);
      block.Height += line.Height;
    }

    ApplyHorizontalAlignment(hAlignment, block);
    block.Start       = block.Children[0].Start;
    block.Length      = block.Children[block.Children.Length-1].End - block.Start;
    block.ContentSpan = block.Span;
    return block;
  }

  /// <summary>Sets the <see cref="LayoutRegion.Border"/>, <see cref="LayoutRegion.Margin"/>, and
  /// <see cref="LayoutRegion.Padding"/> members of the given region, based on the given node.
  /// </summary>
  Size SetBorderMarginAndPadding(Graphics gdi, DocumentNode node, LayoutRegion region, Size available)
  {
    FourSide fourSide = node.Style.Margin;
    Size shrinkage;
    if(fourSide.IsEmpty)
    {
      shrinkage = new Size();
    }
    else
    {
      region.Margin.Left   = GetPixels(gdi, node, fourSide.Left,   available.Width,  Orientation.Horizontal);
      region.Margin.Right  = GetPixels(gdi, node, fourSide.Right,  available.Width,  Orientation.Horizontal);
      region.Margin.Top    = GetPixels(gdi, node, fourSide.Top,    available.Height, Orientation.Vertical);
      region.Margin.Bottom = GetPixels(gdi, node, fourSide.Bottom, available.Height, Orientation.Vertical);
      shrinkage = new Size(region.Margin.TotalHorizontal, region.Margin.TotalVertical);
    }

    fourSide = node.Style.Padding;
    if(!fourSide.IsEmpty)
    {
      region.Padding.Left   = GetPixels(gdi, node, fourSide.Left,   available.Width,  Orientation.Horizontal);
      region.Padding.Right  = GetPixels(gdi, node, fourSide.Right,  available.Width,  Orientation.Horizontal);
      region.Padding.Top    = GetPixels(gdi, node, fourSide.Top,    available.Height, Orientation.Vertical);
      region.Padding.Bottom = GetPixels(gdi, node, fourSide.Bottom, available.Height, Orientation.Vertical);
      shrinkage.Width  += region.Padding.TotalHorizontal;
      shrinkage.Height += region.Padding.TotalVertical;
    }

    Measurement measurement = node.Style.BorderWidth;
    if(measurement.Size != 0 && node.Style.BorderStyle != RichDocument.BorderStyle.None)
    {
      region.Border = new Size(GetPixels(gdi, node, measurement, available.Width,  Orientation.Horizontal),
                               GetPixels(gdi, node, measurement, available.Height, Orientation.Vertical));
      shrinkage.Width  += region.Border.Width  * 2;
      shrinkage.Height += region.Border.Height * 2;
    }

    return shrinkage;
  }

  /// <summary>Finishes a line by adding the final span, grouping spans into a tree, cloning the spans,
  /// calculating their span and content length, adding extra document indices where desired, adding all new spans to
  /// the given node spans dictionary, and finally creating and adding a new <see cref="Line"/> to contain the new span
  /// tree.
  /// </summary>
  static void FinishLine(Graphics gdi, List<Line> lines, List<LayoutSpan> spans, LayoutSpan finalSpan,
                         Dictionary<DocumentNode,List<LayoutSpan>> nodeSpans, ref int startIndex, ref Size lineSize)
  {
    FinishSpan(spans, finalSpan); // add the final span if it's valid

    if(spans.Count != 0) // if there are any spans in the tree...
    {
      // the list of spans is currently stored as a list of leaf spans, each having a list of ancestors in the chain of
      // Parent members. the list needs to be reorganized into a list of trees, grouping spans together under common
      // ancestors

      // a list of spans in order from parent to child, containing the previous leaf span and its ancestors
      List<LayoutSpan> prevAncestors = new List<LayoutSpan>();
      // a list of spans in order from parent to child, containing the next leaf span and its ancestors
      List<LayoutSpan> ancestors = new List<LayoutSpan>();
      // a list of the children of each span in 'prevAncestors'
      List<List<LayoutSpan>> children = new List<List<LayoutSpan>>();

      // we'll loop through each leaf span, and compare the list of ancestors to the previous list of ancestors.
      // the similarities and differences between the two lists will allow us to calculate the tree that needs to be
      // created
      foreach(LayoutSpan span in spans)
      {
        // get the ancestors of the current span
        for(LayoutSpan region = span; region != null; region = (LayoutSpan)region.Parent) ancestors.Add(region);
        ancestors.Add(null); // add a 'null' ancestor representing the virtual root of all ancestors
        ancestors.Reverse(); // reverse it to put it in order from parent to child

        // make sure the prevAncestors and children lists are big enough so we don't have to do bounds checks later
        while(prevAncestors.Count < ancestors.Count) prevAncestors.Add(null);
        while(children.Count < ancestors.Count) children.Add(null);

        // scan from root to leaf and find the point where the lists diverge
        int i;
        for(i=0; i<ancestors.Count; i++)
        {
          if(prevAncestors[i] != ancestors[i]) break;
        }

        for(; i<ancestors.Count; i++) // for each span after the point of divergence...
        {
          // the span in prevAncestors was removed, so its children list is complete. save the list and reset it
          if(children[i] != null && children[i].Count != 0)
          {
            prevAncestors[i].Children = children[i].ToArray();
            children[i].Clear();
          }

          prevAncestors[i] = ancestors[i]; // put the new span in the list

          // and add it as a child of its parent
          if(children[i-1] == null) children[i-1] = new List<LayoutSpan>();
          children[i-1].Add(ancestors[i]);
        }

        ancestors.Clear(); // reset the ancestors list so we can use it for the next span
      }

      // finalize the child lists of the remaining spans. we start from 1 because the 'null' span is at index 0
      for(int i=1; i<children.Count; i++)
      {
        if(children[i] != null && children[i].Count != 0) prevAncestors[i].Children = children[i].ToArray();
      }

      spans.Clear();       // clear the 'spans' parameter for reuse by the caller
      spans = children[0]; // and replace it with a list of the roots of the span trees

      // now finalize and clone each span tree, because the spans passed in will be reused by the caller
      for(int i=0; i<spans.Count; i++) spans[i] = FinishSpanTree(gdi, nodeSpans, spans[i], ref startIndex, 0);

      // add an index after it to allow the cursor to be placed at the end of the line
      LayoutSpan lastSpan = spans[spans.Count-1];
      if(lastSpan.Children.Length == 0)
      {
        lastSpan.Length++;
        startIndex++;
      }
    }

    // now add a new line using the list of spans
    Line line = new Line(spans.ToArray());
    line.BeginLayout(gdi);
    line.Height = lineSize.Height;
    if(spans.Count == 0) line.Span = new Span(startIndex++, 1); // give blank lines an index too
    lines.Add(line);

    lineSize = new Size(); // and reset the lineSize in the caller, now that we've used it
  }

  /// <summary>Adds the given span to the given list if it's not null and not empty.</summary>
  static void FinishSpan(List<LayoutSpan> spans, LayoutSpan span)
  {
    if(span != null && span.Length != 0) spans.Add(span);
  }

  /// <summary>A helper for <see cref="FinishLine"/> that, given a span, recursively clones and updates the span
  /// and content length of it and its descendants, and adds the new spans to the given node spans dictionary.
  /// </summary>
  static LayoutSpan FinishSpanTree(Graphics gdi, Dictionary<DocumentNode,List<LayoutSpan>> nodeSpans, LayoutSpan span,
                                   ref int startIndex, int startOffset)
  {
    DocumentNode node = span.GetNode(); // get the span list for this span's node, but don't add it to the dictionary
    List<LayoutSpan> spanList;          // yet, because we will clone the span later and the reference will change
    if(!nodeSpans.TryGetValue(node, out spanList)) nodeSpans[node] = spanList = new List<LayoutSpan>();

    span.Start += startOffset; // recursively shift all spans by 'startOffset'. this is used to insert indices

    if(span.Children.Length != 0) // if the span represents an inline container node...
    {
      span.Size = new Size(); // then reset its size. its height will be calculated here, and its width in StackSpans

      bool addLeadingIndex = spanList.Count == 0; // if this is the first span for this container node, we'll add an
      if(addLeadingIndex)                         // index to allow the cursor to be placed in front of it
      {
        startOffset++;
        startIndex++;
      }

      // recursively finish the children, and calculate the span's height using the now-correct childrens' heights
      for(int i=0; i<span.Children.Length; i++)
      {
        span.Children[i] = FinishSpanTree(gdi, nodeSpans, span.Children[i], ref startIndex, startOffset);
        span.Height = Math.Max(span.Height, span.Children[i].Height);
      }

      // now that all children are finished, calculate the span and content length of the container
      LayoutSpan lastChild = span.Children[span.Children.Length-1];
      span.Start  = span.Children[0].Start - (addLeadingIndex ? 1 : 0);
      span.Length = span.ContentLength = lastChild.End - span.Start;

      // add another index to allow the cursor to be placed at the end of the last child
      lastChild.Length++;
      span.Length++;
      startIndex++;
    }

    // clone the span so it can be used again on the next line if necessary
    LayoutSpan newSpan = span.CreateNew();
    newSpan.BeginLayout(gdi);
    newSpan.Margin      = span.Margin;  // these are the properties deemed necessary for layout. other members that
    newSpan.Padding     = span.Padding; // the span wants to preserve must be copied in CreateNew, currently.
    newSpan.Border      = span.Border;
    newSpan.Size        = span.Size;
    newSpan.Span        = span.Span;
    newSpan.ContentSpan = span.ContentSpan;
    newSpan.Children    = span.Children;
    spanList.Add(newSpan);
    return newSpan; // return the cloned subtree
  }

  /// <summary>Given a start index and node index, resets the <see cref="LayoutRegion.Size"/>,
  /// <see cref="LayoutRegion.Span"/>, and <see cref="LayoutRegion.ContentSpan"/> of the given span and returns it.
  /// </summary>
  static LayoutSpan ResetSpan(LayoutSpan newSpan, int startIndex, int nodeIndex)
  {
    if(newSpan != null)
    {
      newSpan.Size        = new Size();
      newSpan.Span        = new Span(startIndex, 0);
      newSpan.ContentSpan = new Span(nodeIndex, 0);
    }
    return newSpan;
  }

  /// <summary>A helper for <see cref="LayoutLines"/> that, given a span tree where the leaf spans have the correct
  /// sizes (not counting PBM), updates sizes by adding PBM, recursively lays out the spans within their containers,
  /// left justified and aligned on a common baseline, and sets the sizes of interior spans.
  /// </summary>
  static void StackSpans(LayoutRegion parent, LayoutSpan[] spans)
  {
    // stack the spans horizontally within the line and calculate the maximum descent
    int maxDescent = 0;

    foreach(LayoutSpan span in spans)
    {
      UpdateSpanPBM(span);
      if(span.Children.Length != 0) StackSpans(span, span.Children);

      span.Left     = parent.Width - parent.RightPBM;
      parent.Width += span.Width;
      parent.Height = Math.Max(parent.Height, span.Height + parent.TopPBM + parent.BottomPBM);
      maxDescent    = Math.Max(maxDescent, span.Descent);
    }

    // now go back through and position the spans vertically
    // TODO: implement vertical alignment
    foreach(LayoutSpan s in spans)
    {
      s.Top = parent.Height - parent.BottomPBM - s.Height + s.Descent - maxDescent; // just do bottom alignment for now...
    }
  }

  /// <summary>A helper for <see cref="LayoutLines"/> that updates the size and descent of a span by adding space
  /// based on its <see cref="LayoutRegion.NodePart"/>.
  /// </summary>
  static void UpdateSpanPBM(LayoutSpan span)
  {
    switch(span.NodePart)
    {
      case NodePart.Full: // the node has PBM on all sides
        span.Width  += span.LeftPBM + span.RightPBM;
        span.Height += span.Margin.TotalVertical;
        break;
      case NodePart.Start: // the node has top-left PBM
        span.Width  += span.LeftPBM;
        span.Height += span.Margin.Top;
        break;
      case NodePart.End: // the node has bottom-right PBM
        span.Width  += span.RightPBM;
        span.Height += span.Margin.Bottom;
        break;
    }
    // all spans have vertical padding and border
    span.Height  += span.Padding.TotalVertical + span.Border.Height*2;
    span.Descent += span.BottomPBM; // update the descent as well, shifting it upward by the bottom PBM
  }

  /// <summary>A helper for <see cref="LayoutLines"/> that sets the <see cref="LayoutRegion.NodePart"/> member of
  /// the spans in the given dictionary, assuming that the dictionary is completely built.
  /// </summary>
  static void UpdateSpansWithPBM(Dictionary<DocumentNode,List<LayoutSpan>> nodeSpans)
  {
    foreach(List<LayoutSpan> spans in nodeSpans.Values)
    {
      if(spans.Count == 1) // if the node fit into a single span, the span is Full
      {
        spans[0].NodePart = NodePart.Full;
      }
      else if(spans.Count > 1) // otherwise, the node was split into several spans, so mark the Start, End, and Middles
      {
        spans[0].NodePart = NodePart.Start;
        for(int i=1; i<spans.Count-1; i++) spans[i].NodePart = NodePart.Middle;
        spans[spans.Count-1].NodePart = NodePart.End;
      }
    }
  }
  #endregion

  #region Input handling methods
  /// <summary>Moves the cursor by the given amount, clipped to the bounds of the document, and scrolls the canvas
  /// until the cursor is visible.
  /// </summary>
  protected void MoveCursor(int offset, bool extendSelection)
  {
    MoveCursorTo(offset < 0 ? Math.Max(0, CursorIndex+offset) : Math.Min(IndexLength, CursorIndex+offset),
                 extendSelection);
  }

  /// <summary>Moves the cursor to the given location, and scrolls the canvas until the cursor is visible.</summary>
  protected void MoveCursorTo(int index, bool extendSelection)
  {
    ValidateIndex(index);
    UpdateSelection(index, extendSelection);
    CursorIndex = index;
    ScrollTo(index);
  }

  /// <summary>Moves the cursor to the previous line, and scrolls the canvas until the cursor is visible.</summary>
  protected void MoveCursorUp(bool extendSelection)
  {
    LayoutRegion region = GetRegion(CursorIndex);
    // since we're moving to the previous line, we need to move up the region tree until we get to a block region that
    // has a previous sibling
    while(region != null && (region is LayoutSpan || region.Index == 0)) region = region.Parent;
    if(region == null) return; // if there is no such node, then there's no where to move

    LayoutRegion sibling = region.Parent.GetChildren()[region.Index-1];
    int newIndex;
    using(Graphics gdi = Graphics.FromHwnd(Handle))
    {
      newIndex = sibling.GetNearestIndex(gdi, new Point(idealCursorX - sibling.AbsoluteLeft,
                                                        Math.Max(0, sibling.Height - sibling.BottomPBM - 1)));
    }

    UpdateSelection(newIndex, extendSelection);
    CursorIndexSetter(newIndex);
    ScrollTo(newIndex);
  }

  /// <summary>Moves the cursor to the next line, and scrolls the canvas until the cursor is visible.</summary>
  protected void MoveCursorDown(bool extendSelection)
  {
    LayoutRegion region = GetRegion(CursorIndex);
    LayoutRegion[] children = null;

    // since we're moving to the next line, we need to move up the region tree until we get to a block region that
    // has a next sibling
    while(region.Parent != null)
    {
      if(!(region is LayoutSpan))
      {
        children = region.Parent.GetChildren();
        if(region.Index != children.Length-1) break;
      }
      region = region.Parent;
    }
    if(region.Parent == null) return; // if there is no such node, then there's no where to move

    LayoutRegion sibling = children[region.Index+1];
    int newIndex;
    using(Graphics gdi = Graphics.FromHwnd(Handle))
    {
      newIndex = sibling.GetNearestIndex(gdi, new Point(idealCursorX - sibling.AbsoluteLeft, sibling.TopPBM));
    }

    UpdateSelection(newIndex, extendSelection);
    CursorIndexSetter(newIndex);
    ScrollTo(newIndex);
  }

  /// <summary>Moves the cursor to the previous page, and scrolls the canvas until the cursor is visible.</summary>
  protected void MoveCursorUpAPage(bool extendSelection)
  {
    if(vScrollBar == null) return;

    // calculate the document Y position one page above the top of the cursor
    int docY = Math.Max(0, GetDocumentPoint(cursorRect.Location).Y - vScrollBar.LargeChange - 1);

    // move up the region tree until we get to a block region that contains or is above the desired document point
    LayoutRegion origRegion = GetRegion(CursorIndex), region = origRegion;
    while(region != null)
    {
      if(region.AbsoluteBottom < docY || region.AbsoluteTop <= docY) break;
      region = region.Parent;
    }

    if(region != null)
    {
      int newIndex;
      using(Graphics gdi = Graphics.FromHwnd(Handle))
      {
        newIndex = region.GetNearestIndex(gdi,
                                          new Point(idealCursorX - region.AbsoluteLeft, docY - region.AbsoluteTop));
      }

      if(GetRegion(newIndex) == origRegion) // if it ended up in the same space, then maybe the cursor is in a region
      {                                     // that's bigger than a page. move up to the previous line at least.
        MoveCursorUp(extendSelection);
      }
      else
      {
        UpdateSelection(newIndex, extendSelection);
        CursorIndexSetter(newIndex);
        ScrollTo(newIndex);
      }
    }
  }

  /// <summary>Moves the cursor to the next page, and scrolls the canvas until the cursor is visible.</summary>
  protected void MoveCursorDownAPage(bool extendSelection)
  {
    if(vScrollBar == null) return;

    // calculate the document Y position one page below the bottom of the cursor
    int docY = Math.Min(vScrollBar.Maximum,
                        GetDocumentPoint(cursorRect.Location).Y + cursorRect.Height + vScrollBar.LargeChange - 1);

    // move up the region tree until we get to a block region that contains or is below the desired document point
    LayoutRegion origRegion = GetRegion(CursorIndex), region = origRegion;
    while(region != null)
    {
      if(region.AbsoluteTop >= docY || region.AbsoluteBottom > docY) break;
      region = region.Parent;
    }

    if(region != null)
    {
      int newIndex;
      using(Graphics gdi = Graphics.FromHwnd(Handle))
      {
        newIndex = region.GetNearestIndex(gdi,
                                          new Point(idealCursorX - region.AbsoluteLeft, docY - region.AbsoluteTop));
      }

      if(GetRegion(newIndex) == origRegion) // if it ended up in the same space, then maybe the cursor is in a region
      {                                     // that's bigger than a page. move down to the next line at least.
        MoveCursorDown(extendSelection);
      }
      else
      {
        UpdateSelection(newIndex, extendSelection);
        CursorIndexSetter(newIndex);
        ScrollTo(newIndex);
      }
    }
  }

  /// <summary>Moves the cursor to the beginning of the current line, or to the beginning of the outer region if it's
  /// already at the beginning of the current line.
  /// </summary>
  protected void MoveCursorHome(bool extendSelection)
  {
    LayoutRegion region = GetRegion(CursorIndex);
    while(region is LayoutSpan) region = region.Parent; // skip over spans

    if(CursorIndex != region.Start) // if the cursor not at the beginning of the line, put it there
    {
      UpdateSelection(region.Start, extendSelection);
      CursorIndex = region.Start;
    }
    else if(region.Parent != null) // otherwise, it's at the beginning already, so move it to the outer region
    {
      if(region.Parent is LineBlock) region = region.Parent; // skip over line blocks
      UpdateSelection(region.Parent.Start, extendSelection);
      CursorIndex = region.Parent.Start;
    }
  }

  /// <summary>Moves the cursor to the end of the current line, or to the end of the outer region if it's already at
  /// the end of the current line.
  /// </summary>
  protected void MoveCursorEnd(bool extendSelection)
  {
    LayoutRegion region = GetRegion(CursorIndex);
    while(region is LayoutSpan) region = region.Parent; // skip over spans

    int end = region.DocumentContentSpan.End;
    if(CursorIndex != end) // if the cursor not at the end of the line, put it there
    {
      UpdateSelection(end, extendSelection);
      CursorIndex = end;
    }
    else if(region.Parent != null) // otherwise, it's at the end already, so move it to the outer region
    {
      if(region.Parent is LineBlock) region = region.Parent; // skip over line blocks
      int newIndex = region.Parent.DocumentContentSpan.End;
      UpdateSelection(newIndex, extendSelection);
      CursorIndex = newIndex;
    }
  }

  /// <summary>Scrolls by the given number of pixels.</summary>
  protected void Scroll(int hPixels, int vPixels)
  {
    if(hPixels != 0 && hScrollBar != null)
    {
      if(hPixels < 0)
      {
        if(hScrollBar.Value > 0) hScrollBar.Value += Math.Max(hPixels, -hScrollBar.Value);
      }
      else
      {
        if(hScrollBar.Value < HScrollMaximum) hScrollBar.Value += Math.Min(hPixels, HScrollMaximum-hScrollBar.Value);
      }
    }

    if(vPixels != 0 && vScrollBar != null)
    {
      if(vPixels < 0)
      {
        if(vScrollBar.Value > 0) vScrollBar.Value += Math.Max(vPixels, -vScrollBar.Value);
      }
      else
      {
        if(vScrollBar.Value < VScrollMaximum) vScrollBar.Value += Math.Min(vPixels, VScrollMaximum-vScrollBar.Value);
      }
    }
  }

  /// <summary>Scrolls by the given number of chunks. Scrolling one chunk is equivalent to clicking the arrow at the
  /// end of a scrollbar.
  /// </summary>
  protected void ScrollByChunks(int horizontal, int vertical)
  {
    Scroll(hScrollBar == null ? 0 : horizontal * hScrollBar.SmallChange,
           vScrollBar == null ? 0 : vertical   * vScrollBar.SmallChange);
  }

  /// <summary>Scrolls to the left by a small amount.</summary>
  protected void ScrollLeft()
  {
    ScrollByChunks(-1, 0);
  }

  /// <summary>Scrolls to the right by a small amount.</summary>
  protected void ScrollRight()
  {
    ScrollByChunks(1, 0);
  }

  /// <summary>Scrolls up by a small amount.</summary>
  protected void ScrollUp()
  {
    ScrollByChunks(0, -1);
  }

  /// <summary>Scrolls down by a small amount.</summary>
  protected void ScrollDown()
  {
    ScrollByChunks(0, 1);
  }

  /// <summary>Scrolls up by one page.</summary>
  protected void ScrollUpAPage()
  {
    if(vScrollBar != null) Scroll(0, -vScrollBar.LargeChange);
  }

  /// <summary>Scrolls down by one page.</summary>
  protected void ScrollDownAPage()
  {
    if(vScrollBar != null) Scroll(0, vScrollBar.LargeChange);
  }

  /// <summary>Scrolls to the top of the document.</summary>
  protected void ScrollToTop()
  {
    if(vScrollBar != null && vScrollBar.Value != 0) vScrollBar.Value = 0;
    if(hScrollBar != null && hScrollBar.Value != 0) hScrollBar.Value = 0;
  }

  /// <summary>Scrolls to the bottom of the document.</summary>
  protected void ScrollToBottom()
  {
    if(vScrollBar != null && vScrollBar.Value != VScrollMaximum) vScrollBar.Value = VScrollMaximum;
    if(hScrollBar != null && hScrollBar.Value != HScrollMaximum) hScrollBar.Value = HScrollMaximum;
  }

  void UpdateSelection(int newIndex, bool extendSelection)
  {
    if(extendSelection)
    {
      int newStart, newLength;

      if(Selection.Length == 0)
      {
        newStart  = CursorIndex;
        newLength = newIndex - CursorIndex;
      }
      else if(CursorIndex == Selection.Start)
      {
        newStart  = newIndex;
        newLength = Selection.Length + (Selection.Start - newIndex);
      }
      else
      {
        newStart  = Selection.Start;
        newLength = newIndex - Selection.Start;
      }

      Selection = newLength < 0 ? new Span(newStart + newLength, -newLength) : new Span(newStart, newLength);
    }
    else DeselectAll();
  }
  #endregion

  /// <summary>Gets whether a valid layout exists.</summary>
  protected bool HasLayout
  {
    get { return rootBlock != null; }
  }

  /// <summary>Gets the maximum value to which the horizontal scrollbar can be set by the user.</summary>
  protected int HScrollMaximum
  {
    get { return hScrollBar == null ? 0 : hScrollBar.Maximum - hScrollBar.LargeChange + 1; }
  }

  /// <summary>Gets the maximum value to which the vertical scrollbar can be set by the user.</summary>
  protected int VScrollMaximum
  {
    get { return vScrollBar == null ? 0 : vScrollBar.Maximum - vScrollBar.LargeChange + 1; }
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

  /// <include file="documentation.xml" path="/UI/Common/Dispose/node()"/>
  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    document.NodeChanged -= OnNodeChanged;
    Utility.Dispose(ref rootBlock);
  }

  /// <summary>Updates the layout using the given graphics device if the layout is currently invalid.</summary>
  protected void EnsureLayout(Graphics gdi)
  {
    if(!HasLayout)
    {
      rootBlock = CreateRootBlock(gdi);

      // if a scrollbar was created or destroyed, altering the canvas width, we need to redo the layout
      // TODO: it sucks to have to lay out the document up to three times! the layout code should abort as soon as it
      // determines that a scrollbar will be added or removed
      if(CreateOrDestroyScrollbars())
      {
        rootBlock = CreateRootBlock(gdi);
        // now the other scrollbar may have been created or destroyed
        if(CreateOrDestroyScrollbars()) rootBlock = CreateRootBlock(gdi);
      }

      SetAbsolutePositions(rootBlock, rootBlock.Position);
      ResizeScrollbars(gdi);

      if(EnableCursor) SetCursor(Math.Min(cursorIndex, IndexLength));
    }
  }

  /// <summary>Given a point in document coordinates, returns the point in client coordinates.</summary>
  protected Point GetClientPoint(Point documentPt)
  {
    Point canvasStart = CanvasRectangle.Location, scrollPos = ScrollPosition;
    return new Point(documentPt.X + canvasStart.X - scrollPos.X, documentPt.Y + canvasStart.Y - scrollPos.Y);
  }

  /// <summary>Given a point in client coordinates, returns the point in document coordinates.</summary>
  protected Point GetDocumentPoint(Point clientPt)
  {
    Point canvasStart = CanvasRectangle.Location, scrollPos = ScrollPosition;
    return new Point(clientPt.X - canvasStart.X + scrollPos.X, clientPt.Y - canvasStart.Y + scrollPos.Y);
  }

  /// <summary>Given a point in document coordinates, returns the nearest document index.</summary>
  protected int GetNearestIndex(Point docPt)
  {
    using(Graphics gdi = Graphics.FromHwnd(Handle))
    {
      EnsureLayout(gdi);
      return rootBlock.GetNearestIndex(gdi, docPt);
    }
  }

  /// <summary>Gets the innermost <see cref="LayoutRegion"/> that contains the given index.</summary>
  protected LayoutRegion GetRegion(int index)
  {
    ValidateIndex(index);
    Layout();
    return rootBlock.GetRegion(index);
  }

  /// <summary>Gets the innermost <see cref="LayoutRegion"/> that contains the given document point, or null if no
  /// region contains it.
  /// </summary>
  protected LayoutRegion GetRegion(Point docPt)
  {
    Layout();

    if(!rootBlock.Bounds.Contains(docPt)) return null;

    LayoutRegion region = rootBlock;
    while(true)
    {
      tryAgain:
      foreach(LayoutRegion child in region.GetChildren())
      {
        if(child.Bounds.Contains(docPt))
        {
          region = child;
          docPt.Offset(-child.Left, -child.Top);
          goto tryAgain;
        }
      }
      return region;
    }
  }

  /// <summary>Invalidates the entire layout.</summary>
  protected void InvalidateLayout()
  {
    DeselectAll();

    for(int i=mouseOver.Count-1; i >= 0; i--) mouseOver[i].OnMouseLeave(this);
    mouseOver.Clear();

    Utility.Dispose(ref rootBlock);
    Invalidate(CanvasRectangle);
  }

  /// <summary>Invalidates the portion of the canvas containing the given node, causing it to be repainted.</summary>
  protected void InvalidateNode(DocumentNode node)
  {
    Rectangle visibleArea = GetVisibleArea(GetIndexSpan(node));
    if(visibleArea.Width != 0) Invalidate(visibleArea);
  }

  /// <summary>Invalidates the layout of the given node, and triggers a repaint.</summary>
  protected void InvalidateNodeLayout(DocumentNode node)
  {
    if(HasLayout) // if we have a layout, update it and repaint incrementally
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>Invalidates the portion of the canvas containing the given region, causing it to be repainted.</summary>
  protected void InvalidateRegion(LayoutRegion region)
  {
    if(region == null) throw new ArgumentNullException();
    Rectangle area = new Rectangle(GetClientPoint(region.AbsolutePosition), region.Size);
    if(area.IntersectsWith(CanvasRectangle)) Invalidate(area);
  }

  #region Event handlers
  /// <summary>Determines whether the given key is an input key that should be passed through <see cref="OnKeyDown"/>.</summary>
  protected override bool IsInputKey(Keys keyData)
  {
    if((keyData & Keys.Alt) != 0) return false;

    switch(keyData & ~Keys.Modifiers)
    {
      // the base implementation preprocesses these keys, but we want to handle them ourselves
      case Keys.Left: case Keys.Right: case Keys.Up: case Keys.Down: case Keys.Tab: case Keys.Enter:
        return true;
      default:
        return base.IsInputKey(keyData);
    }
  }

  /// <include file="documentation.xml" path="/UI/Document/OnNodeChanged/node()"/>
  protected virtual void OnNodeChanged(Document document, DocumentNode node)
  {
    InvalidateNodeLayout(node);
  }

  /// <summary>Called when the background color changes.</summary>
  protected override void OnBackColorChanged(EventArgs e)
  {
    base.OnBackColorChanged(e);
    Invalidate(CanvasRectangle);
  }

  /// <summary>Called when the mouse is double-clicked on the control.</summary>
  protected override void OnMouseDoubleClick(MouseEventArgs e)
  {
    base.OnMouseDoubleClick(e);

    Point docPt = GetDocumentPoint(e.Location);
    LayoutRegion region = GetRegion(docPt);
    if(region != null) region.OnDoubleClick(this, new MouseEventArgs(e.Button, e.Clicks, docPt.X, docPt.Y, e.Delta));
  }

  /// <summary>Called when the font changes.</summary>
  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);

    // if no font is set at the root, make the assumption that the new control font will be used somewhere and that
    // it's a different size. (optimizing to detect fonts of the same size seems like overkill...)
    if(GetEffectiveFont(Document.Root) == Font)
    {
      InvalidateLayout(); // and if the font is used and is a different size, we need to recalculate the layout
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

  /// <summary>Called when the control receives input focus.</summary>
  protected override void OnGotFocus(EventArgs e)
  {
    base.OnGotFocus(e);

    // restore the cursor if it should be visible
    UpdateCursor();
  }

  /// <summary>Called when a key on the keyboard is depressed or repeated.</summary>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);
    if(e.Handled) return;

    switch(e.KeyCode)
    {
      case Keys.Left:
        if(e.Modifiers == Keys.None) // the left arrow moves the cursor backward by one index, or scrolls to the left
        {                            // by a small amount
          if(EnableCursor) MoveCursor(-1, false);
          else ScrollLeft();
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor) // shift-left moves the cursor backwards and extends the selection
        {
          MoveCursor(-1, true);
        }
        else if(e.Modifiers == Keys.Control) ScrollLeft(); // ctrl-left scrolls to the left by a small amount
        else goto default;
        break;

      case Keys.Right:
        if(e.Modifiers == Keys.None) // the right arrow moves the cursor forward by one index, or scrolls to the right
        {                            // by a small amount
          if(EnableCursor) MoveCursor(1, false);
          else ScrollRight();
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor)
        {
          MoveCursor(1, true);
        }
        else if(e.Modifiers == Keys.Control) ScrollRight(); // ctrl-right scrolls to the right by a small amount
        else goto default;
        break;

      case Keys.Up:
        if(e.Modifiers == Keys.None) // up moves the cursor up one line, or scrolls up by a small amount
        {
          if(EnableCursor) MoveCursorUp(false);
          else ScrollUp();
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor) // shift-up moves the cursor up and extends the selection
        {
          MoveCursorUp(true);
        }
        else if(e.Modifiers == Keys.Control) ScrollUp(); // ctrl-up scrolls up by a small amount
        else goto default;
        break;

      case Keys.Down:
        if(e.Modifiers == Keys.None) // up moves the cursor up one line, or scrolls up by a small amount
        {
          if(EnableCursor) MoveCursorDown(false);
          else ScrollDown();
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor) // shift-down moves the cursor down and extends the selection
        {
          MoveCursorDown(true);
        }
        else if(e.Modifiers == Keys.Control) ScrollDown(); // ctrl-down scrolls down by a small amount
        else goto default;
        break;

      case Keys.Home:
        if(e.Modifiers == Keys.None && EnableCursor) // home moves to the beginning of the line, or to the outer region
        {                                            // if it's already at the beginning
          MoveCursorHome(false);
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor) // shift-home does the same, but extends the selection
        {
          MoveCursorHome(true);
        }
        else if(e.Modifiers == Keys.Control) // ctrl-home moves to the top of the document
        {
          if(EnableCursor) MoveCursorTo(0, false);
          else ScrollToTop();
        }
        else if(e.Modifiers == (Keys.Control | Keys.Shift) && EnableCursor) // shift-ctrl-home extends the selection to
        {                                                                   // the top of the document
          MoveCursorTo(0, true);
        }
        else goto default;
        break;

      case Keys.End:
        if(e.Modifiers == Keys.None && EnableCursor) // end moves to the end of the line, or to the outer region if
        {                                            // it's already at the end
          MoveCursorEnd(false);
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor) // shift-end does the same, but extends the selection
        {
          MoveCursorEnd(true);
        }
        else if(e.Modifiers == Keys.Control) // ctrl-end moves to the end of the document
        {
          if(EnableCursor) MoveCursorTo(IndexLength, false);
          else ScrollToBottom();
        }
        else if(e.Modifiers == (Keys.Control | Keys.Shift) && EnableCursor) // shift-ctrl-end extends the selection to
        {                                                                   // the bottom of the document
          MoveCursorTo(IndexLength, true);
        }
        else goto default;
        break;

      case Keys.PageUp:
        if(e.Modifiers == Keys.None) // page up moves the cursor up a page, or scrolls up a page
        {
          if(EnableCursor) MoveCursorUpAPage(false);
          else ScrollUpAPage();
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor)
        {
          MoveCursorUpAPage(true);
        }
        else goto default;
        break;

      case Keys.PageDown:
        if(e.Modifiers == Keys.None) // page down moves the cursor down a page, or scrolls down a page
        {
          if(EnableCursor) MoveCursorDownAPage(false);
          else ScrollDownAPage();
        }
        else if(e.Modifiers == Keys.Shift && EnableCursor)
        {
          MoveCursorDownAPage(true);
        }
        else goto default;
        break;

      case Keys.A:
        if(e.Modifiers == Keys.Control) SelectAll(); // ctrl-a selects everything
        else goto default;
        break;

      default: return; // return without setting e.Handled to true
    }

    e.Handled = true;
  }


  /// <summary>Called when the control loses input focus.</summary>
  protected override void OnLostFocus(EventArgs e)
  {
    base.OnLostFocus(e);
    HideCursor();
  }

  /// <summary>Called when a mouse button is depressed while the control has mouse focus.</summary>
  protected override void OnMouseDown(MouseEventArgs e)
  {
    base.OnMouseDown(e);

    // TODO: implement auto scrolling using the middle mouse button
    if(e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) // we handle only the left and right buttons
    {
      Focus(); // on a mouse click, give our control the input focus

      Point docPt = GetDocumentPoint(e.Location);

      // move the cursor if selection is enabled and the mouse click was outside the selection
      if(EnableCursor && !IsPointWithinSpan(docPt, Selection))
      {
        int newIndex = GetNearestIndex(docPt);
        UpdateSelection(newIndex, (Control.ModifierKeys & Keys.Shift) != 0);
        CursorIndex = newIndex;
      }

      mouseDown[GetMouseIndex(e.Button)] = docPt;
    }
  }

  /// <summary>Called when the mouse hovers over the control.</summary>
  protected override void OnMouseHover(EventArgs e)
  {
    base.OnMouseHover(e);

    Point docPt = GetDocumentPoint(PointToClient(Cursor.Position));
    LayoutRegion region = GetRegion(docPt);
    if(region != null) region.OnMouseHover(this, docPt);
  }

  /// <summary>Called when the mouse is moved over the control.</summary>
  protected override void OnMouseMove(MouseEventArgs e)
  {
    base.OnMouseMove(e);

    Point docPt = GetDocumentPoint(PointToClient(Cursor.Position));
    LayoutRegion region = GetRegion(docPt);

    LayoutRegion currentlyOver = mouseOver.Count == 0 ? null : mouseOver[mouseOver.Count-1];
    if(region != currentlyOver) // if the mouse moved over a different region or moved off the current region...
    {
      if(region == null) // it was over a node before but now isn't, so call Leave from the leaf up
      {
        for(int i=0; i<mouseOver.Count; i++) mouseOver[i].OnMouseLeave(this);
        mouseOver.Clear();
      }
      else // otherwise, it is over a node now
      {
        e = new MouseEventArgs(e.Button, e.Clicks, docPt.X, docPt.Y, e.Delta);

        if(currentlyOver == null) // but it wasn't over a node previously, so call Enter from the root down to it
        {
          FillAncestorList(mouseOver, region);
          for(int i=mouseOver.Count-1; i >= 0; i--) mouseOver[i].OnMouseEnter(this, e);
          mouseOver.Reverse();
        }
        else // otherwise, it moved from one node to another
        {
          List<LayoutRegion> temp; // the uses of these two arrays are about to be swapped, so swap them now
          temp = mouseOver; mouseOver = mouseOut; mouseOut = temp;

          FillAncestorList(mouseOver, region);
          mouseOver.Reverse();

          // find the index where the two ancestor lists diverge
          int index = 0;
          for(int count=Math.Min(mouseOver.Count, mouseOut.Count); index < count; index++)
          {
            if(mouseOver[index] != mouseOut[index]) break;
          }

          // now call Leave and Enter as necessary
          for(int i=mouseOut.Count-1; i >= index; i--) mouseOut[i].OnMouseLeave(this);
          for(; index < mouseOver.Count; index++) mouseOver[index].OnMouseEnter(this, e);

          mouseOut.Clear();
        }
      }
    }
  }

  /// <summary>Called when a mouse button is released while the control has mouse focus.</summary>
  protected override void OnMouseUp(MouseEventArgs e)
  {
    base.OnMouseUp(e);

    if(e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
    {
      int btnIndex = GetMouseIndex(e.Button);
      if(mouseDown[btnIndex].HasValue)
      {
        Point downPt = mouseDown[btnIndex].Value, upPt = GetDocumentPoint(e.Location);
        int xd = upPt.X - downPt.X, yd = upPt.Y - downPt.Y;

        if(xd*xd + yd*yd < 4) // if the mouse hasn't moved too far since the button was pressed, it's a click
        {
          // a left-click within the selection will alter it
          if(e.Button == MouseButtons.Left && IsPointWithinSpan(downPt, Selection))
          {
            int newIndex = GetNearestIndex(downPt);
            UpdateSelection(newIndex, (Control.ModifierKeys & Keys.Shift) != 0);
            CursorIndex = newIndex;
          }

          LayoutRegion region = GetRegion(downPt);
          if(region != null) region.OnClick(this, new MouseEventArgs(e.Button, e.Clicks, downPt.X, downPt.Y, e.Delta));
        }

        mouseDown[btnIndex] = null;
      }
    }
  }

  /// <summary>Called when the mouse wheel is moved.</summary>
  protected override void OnMouseWheel(MouseEventArgs e)
  {
    base.OnMouseWheel(e);

    // allow scrolling using the mouse wheel. the OS reports a delta of 120 per notch of a standard mouse wheel,
    // and we'll move 3 lines per notch
    const float DeltaPerDetent = 120, LinesPerDetent = 3;
    if(vScrollBar != null) ScrollByChunks(0, (int)Math.Round(e.Delta * -(LinesPerDetent/DeltaPerDetent)));
  }

  /// <summary>Called when the size of the control changes.</summary>
  protected override void OnSizeChanged(EventArgs e)
  {
    base.OnSizeChanged(e);

    // if the control size changed, we need to redo the layout
    if(HasLayout && controlSize != Size) InvalidateLayout();

    controlSize = Size; // keep track of the size so we can tell when it changes
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
    RenderData data = new RenderData(e.Graphics, this, Rectangle.Intersect(e.ClipRectangle, canvasRect), Selection);
    rootBlock.Render(ref data, GetClientPoint(new Point()));

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
    if(BorderStyle != System.Windows.Forms.BorderStyle.None)
    {
      ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle,
        BorderStyle == System.Windows.Forms.BorderStyle.Fixed3D ? Border3DStyle.Sunken : Border3DStyle.Flat);
    }
  }
  #endregion

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
    if((uint)index > (uint)IndexLength) throw new ArgumentOutOfRangeException();
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

  [DllImport("user32.dll", EntryPoint="CreateCaret")]
  static extern bool W32CreateCaret(IntPtr hWnd, IntPtr hBitmap, int width, int height);
  [DllImport("user32.dll", EntryPoint="DestroyCaret")]
  static extern bool W32DestroyCaret();
  [DllImport("user32.dll", EntryPoint="HideCaret")]
  static extern bool W32HideCaret(IntPtr hWnd);
  [DllImport("user32.dll", EntryPoint="SetCaretPos")]
  static extern bool W32SetCaretPos(int x, int y);
  [DllImport("user32.dll", EntryPoint="ShowCaret")]
  static extern bool W32ShowCaret(IntPtr hWnd);
  [DllImport("user32.dll", EntryPoint="ScrollWindow")]
  static extern bool W32ScrollWindow(IntPtr hWnd, int xOffset, int yOffset, ref RECT scrollRegion, ref RECT clipRect);
  #endregion

  /// <summary>Gets the thickness of the border, in pixels.</summary>
  int BorderWidth
  {
    get
    {
      // TODO: we probably shouldn't be making assumptions about how the border will be drawn...
      return BorderStyle == System.Windows.Forms.BorderStyle.Fixed3D ?
        2 : BorderStyle == System.Windows.Forms.BorderStyle.FixedSingle ? 1 : 0;
    }
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

  /// <summary>Performs the work of setting <see cref="CursorIndex"/>, except that it does not alter the desired
  /// cursor X position.
  /// </summary>
  void CursorIndexSetter(int index)
  {
    if(HasLayout) SetCursor(index); // if we have a layout, move the cursor immediately
    else cursorIndex = index; // otherwise, just set the index and it'll be moved when the layout is redone
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
      return true;
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
      return true;
    }

    return false;
  }

  /// <summary>Given a span within the document, enumerates the rectangles of the document within the span.</summary>
  IEnumerable<Rectangle> EnumerateRectangles(Span span)
  {
    // TODO: ensure that this returns the largest rectangles possible (eg, if a whole lineblock is selected, we don't
    // need each individual span within it)
    if(HasLayout && span.Length != 0)
    {
      LayoutRegion region = GetRegion(span.Start);
      using(Graphics gdi = Graphics.FromHwnd(Handle))
      {
        while(true)
        {
          // move up to find the largest region that is fully contained within the span, so that we can return fewer,
          // larger rectangles
          while(region.Parent != null && span.Contains(region.Parent.Span)) region = region.Parent;

          LayoutRegion[] children = region.GetChildren();

          // consider the situation of a block that contains several lines. imagine that the start index is at the
          // beginning of the block end index is near the beginning of the second line. although the span extends the
          // full width of the first line as it wraps, the approach of asking for the two horizontal offsets as is done
          // below would return a rectangle much narrower than is correct. to prevent this, we'll delegate to the
          // children of the region if the span doesn't contain the entire region
          int limit = children.Length != 0 && children[0].Span.Start > span.Start && !span.Contains(region.Span)
            ? children[0].Start : region.End;

          int endIndex = Math.Min(span.End, limit);
          int start = region.GetPixelOffset(gdi, span.Start - region.Start).X;
          int end = region.GetPixelOffset(gdi, endIndex - region.Start).X;

          Rectangle area = new Rectangle(region.AbsoluteLeft + start, region.AbsoluteTop,
                                         end - start, region.Height);
          if(area.Width != 0) yield return area;

          if(endIndex == span.End) break; // if that was the end of the span, we're done

          // this region doesn't contain the whole span, so try to continue from its first child
          if(children.Length != 0 && children[0].Span.Contains(endIndex))
          {
            region = children[0]; // the first child does contain it, so we'll search downward from there
          }
          else // there is no next sibling, or the sibling doesn't contain the next portion, so we'll move upwards until
          {    // we find the region containing the next portion, and then search downward again
            do region = region.Parent; while(region != null && !region.Span.Contains(endIndex));
          }

          // search downward from the region found above
          region = region.GetRegion(endIndex);
          span = new Span(endIndex, span.End-endIndex);
        }
      }
    }
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

  /// <summary>Given a span within the document, returns the portion of the span that is visible within the control,
  /// in client units.
  /// </summary>
  Rectangle GetVisibleArea(Span span)
  {
    Rectangle area = new Rectangle();
    foreach(Rectangle newArea in EnumerateRectangles(span))
    {
      area = area.Width == 0 ? newArea : Rectangle.Union(area, newArea);
    }
    area.Offset(GetClientPoint(new Point()));
    return area;
  }

  /// <summary>Given a point and a span within the document, returns whether the point is within the region covered
  /// by the span.
  /// </summary>
  bool IsPointWithinSpan(Point docPt, Span span)
  {
    foreach(Rectangle newArea in EnumerateRectangles(span))
    {
      if(newArea.Contains(docPt)) return true;
    }
    return false;
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
        vScrollPos = Math.Min(vScrollBar.Value, VScrollMaximum);
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
        hScrollPos = Math.Min(hScrollBar.Value, HScrollMaximum);
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
    if(PlatformIsWindows)
    {
      RECT canvasRect = new RECT(CanvasRectangle);
      W32ScrollWindow(Handle, xOffset, yOffset, ref canvasRect, ref canvasRect);
    }
    else
    {
      // TODO: attempt a manual scroll using the GDI and see how fast it is compared to a full repaint
      Invalidate(CanvasRectangle);
    }

    // update the position of the text cursor as well
    cursorRect.Offset(xOffset, yOffset);
    UpdateCursor();
  }

  /// <summary>Given a document index, moves the graphical text cursor to the given index, and sets
  /// <see cref="CursorIndex"/> to the given value.
  /// </summary>
  void SetCursor(int index)
  {
    using(Graphics gdi = Graphics.FromHwnd(Handle)) SetCursor(gdi, index);
  }

  /// <summary>Given a document index, moves the graphical text cursor to the given index, and sets
  /// <see cref="CursorIndex"/> to the given value.
  /// </summary>
  void SetCursor(Graphics gdi, int index)
  {
    LayoutRegion region = GetRegion(index);
    Point pixelOffset = region.GetPixelOffset(gdi, index - region.Start);

    cursorIndex = index;
    SetCursor(GetClientPoint(new Point(region.AbsoluteLeft + pixelOffset.X, region.AbsoluteTop  + pixelOffset.Y)),
              region.GetCursorHeight(index - region.Start));
  }

  /// <summary>Given the position of the text cursor's top-left corner in client units and its height, moves the
  /// graphical text cursor to the given position and sets <see cref="cursorRect"/>.
  /// </summary>
  void SetCursor(Point clientPoint, int height)
  {
    if(height != cursorRect.Height) HideCursor(); // if the height changed, destroy the old cursor so it'll be recreated

    cursorRect = new Rectangle(clientPoint, new Size(1, height));

    // show the cursor if the control has input focus and the cursor is visible. otherwise, hide it.
    if(Focused && cursorRect.IntersectsWith(CanvasRectangle)) ShowCursor();
    else HideCursor();
  }

  /// <summary>To be called when the cursor may need to be redrawn, for instance because the canvas was scrolled or
  /// resized, or <see cref="EnableCursor"/> was changed.
  /// </summary>
  void UpdateCursor()
  {
    if(EnableCursor)
    {
      if(cursorRect.Width != 0) SetCursor(cursorRect.Location, cursorRect.Height);
      else SetCursor(cursorIndex);
    }
    else HideCursor();
  }

  /// <summary>Creates the background task that draws the text cursor at the position specified by
  /// <see cref="cursorRect"/>, or updates the background task when <see cref="cursorRect"/> has changed.
  /// </summary>
  void ShowCursor()
  {
    if(PlatformIsWindows)
    {
      if(!cursorVisible)
      {
        W32CreateCaret(Handle, IntPtr.Zero, cursorRect.Width, cursorRect.Height);
        cursorVisible = true;
      }
      W32SetCaretPos(cursorRect.X, cursorRect.Y);
      W32ShowCaret(Handle);
    }
    else
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>Terminates the background task that draws the text cursor.</summary>
  void HideCursor()
  {
    if(cursorVisible)
    {
      if(PlatformIsWindows)
      {
        W32HideCaret(Handle);
        W32DestroyCaret();
      }
      else throw new NotImplementedException();
      cursorVisible = false;
    }
  }

  readonly Document document;
  RootRegion rootBlock;
  List<LayoutRegion> mouseOver = new List<LayoutRegion>(), mouseOut = new List<LayoutRegion>();
  Point?[] mouseDown = new Point?[3]; // where the left, middle, and right mouse buttons were pressed, in document pts
  HScrollBar hScrollBar;
  VScrollBar vScrollBar;
  /// <summary>The area of the text cursor within the control.</summary>
  Rectangle cursorRect;
  Size controlSize;
  int cursorIndex, hScrollPos, vScrollPos, idealCursorX;
  Span selection;
  ScrollBars scrollBars;
  System.Windows.Forms.BorderStyle borderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
  bool layoutSuspended, repaintNeeded, cursorVisible, enableCursor = true;

  /// <summary>Given a list and a region, adds the region and all its ancestors to the list.</summary>
  static void FillAncestorList(List<LayoutRegion> list, LayoutRegion region)
  {
    for(; region != null; region = region.Parent) list.Add(region);
  }

  /// <summary>Given a <see cref="MouseButtons"/> of Left, Right, or Middle, returns an integer from 0 to 2
  /// representing the button.
  /// </summary>
  static int GetMouseIndex(MouseButtons button)
  {
    if(button == MouseButtons.Left) return 0;
    else if(button == MouseButtons.Right) return 2;
    else if(button == MouseButtons.Middle) return 1;
    else throw new ArgumentOutOfRangeException();
  }

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

    foreach(LayoutRegion child in region.GetChildren())
    {
      SetAbsolutePositions(child, new Point(absPosition.X+child.Left, absPosition.Y+child.Top));
    }
  }

  static readonly bool PlatformIsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT ||
                                           Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                                           Environment.OSVersion.Platform == PlatformID.WinCE;
}*/

} // namespace AdamMil.UI.RichDocument