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
      if(vScrollBar != null) rect.Width -= vScrollBar.Width; // account for the vertical scroll bar
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
        if(value < 0 || value > IndexLength) throw new ArgumentOutOfRangeException();
        if(HasLayout) SetCursor(value); // if we have a layout, move the cursor immediately
        else cursorIndex = value; // otherwise, just set the index and it'll be moved when the layout is redone
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
  /// control's canvas area.
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
  /// there's no selection.
  /// </summary>
  public void Paste()
  {
    Span span = Selection;
    if(span.Length == 0) span.Start = CursorIndex;
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
  protected abstract class LayoutRegion
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

    /// <summary>Gets the width of the left border, margin, and padding.</summary>
    public int LeftPBM
    {
      get { return Border.Width + Margin.Left + Padding.Left; }
    }

    /// <summary>Gets the width of the right border, margin, and padding.</summary>
    public int RightPBM
    {
      get { return Border.Width + Margin.Right + Padding.Right; }
    }

    /// <summary>Gets the width of the top border, margin, and padding.</summary>
    public int TopPBM
    {
      get { return Border.Height + Margin.Top + Padding.Top; }
    }

    /// <summary>Gets the width of the bottom border, margin, and padding.</summary>
    public int BottomPBM
    {
      get { return Border.Height + Margin.Bottom + Padding.Bottom; }
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

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/BeginLayout/*"/>
    public virtual void BeginLayout(Graphics gdi) { }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/*"/>
    public abstract LayoutRegion[] GetChildren();

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetPixelOffset/*"/>
    public virtual Size GetPixelOffset(Graphics gdi, int indexOffset)
    {
      if(indexOffset < 0 || indexOffset > Length) throw new ArgumentOutOfRangeException();
      if(indexOffset > 0 && indexOffset < Length) throw new NotImplementedException(); // the base implementation only supports indivisible nodes

      // if the border is one pixel wide, the cursor will cause the border to flash on and off, so we'll move the
      // cursor one pixel to the outside of the border
      int borderAdjustment = Border.Width == 1 ? 1 : 0;
      return new Size(indexOffset == 0 ? Margin.Left-borderAdjustment : Width-RightPBM+borderAdjustment, 0);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetRegion/*"/>
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

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNode/*"/>
    public virtual DocumentNode GetNode() { return null; }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Render/*"/>
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

    /// <summary>The region's bounds, relative to the parent region.</summary>
    public Rectangle Bounds;
    /// <summary>The region's absolute position in the document.</summary>
    public Point AbsolutePosition;
    /// <summary>The span of indices that this region contains.</summary>
    public Span Span;
    /// <summary>The thickness of the margin, in pixels.</summary>
    public FourSideInt Margin;
    /// <summary>The thickness of the padding, in pixels.</summary>
    public FourSideInt Padding;
    /// <summary>The thickness of the border, in pixels.</summary>
    public Size Border;
    /// <summary>The portion of the <see cref="DocumentNode"/> represented by this region.</summary>
    public NodePart NodePart;

    /// <summary>Renders the background of the region if it is associated with a <see cref="DocumentNode"/>.</summary>
    protected void RenderBackground(ref RenderData data, Point clientPoint)
    {
      DocumentNode node = GetNode();

      if(node != null)
      {
        Color? color = node.Style.BackColor;
        if(color.HasValue && color.Value.A != 0)
        {
          using(Brush brush = new SolidBrush(color.Value))
          {
            Rectangle paddingBox = new Rectangle(clientPoint.X, clientPoint.Y+Border.Height,
                                                 Width, Height-Border.Height*2);
            switch(NodePart) // this code assumes that the layout code has already adjusted the region size to account
            {                // for the missing paddings, margins, and borders
              case NodePart.Full: // all four margins, paddings, borders will be rendered
                paddingBox.X      += Margin.Left + Border.Width;
                paddingBox.Y      += Margin.Top;
                paddingBox.Width  -= Margin.TotalHorizontal + Border.Width*2;
                paddingBox.Height -= Margin.TotalVertical;
                break;
              // the top and left margins and paddings, and the top, left, and bottom borders will be rendered
              case NodePart.Start:
                paddingBox.X      += Margin.Left + Border.Width;
                paddingBox.Y      += Margin.Top;
                paddingBox.Width  -= Margin.Left + Border.Width;
                paddingBox.Height -= Margin.Top;
                break;
              // the bottom and right margins and paddings, and the top, right, and bottom borders will be rendered
              case NodePart.End:
                paddingBox.Width  -= Margin.Right + Border.Width;
                paddingBox.Height -= Margin.Bottom;
                break;
            }
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
        BorderStyle borderStyle = node.Style.BorderStyle;
        Color color = node.Style.BorderColor ?? node.Style.EffectiveForeColor ?? data.Editor.ForeColor;
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
            RectangleF borderRect =
              new RectangleF(clientPoint.X + (Border.Width-1)*0.5f, clientPoint.Y + (Border.Height-1)*0.5f,
                             Width - Border.Width, Height - Border.Height);
            switch(NodePart) // this code assumes that the layout code has already adjusted the region size to account
            {                // for the missing paddings, margins, and borders
              case NodePart.Full: // all four margins, paddings, borders will be rendered
                borderRect.X      += Margin.Left;
                borderRect.Y      += Margin.Top;
                borderRect.Width  -= Margin.TotalHorizontal;
                borderRect.Height -= Margin.TotalVertical;
                break;
              // the top and left margins and paddings, and the top, left, and bottom borders will be rendered
              case NodePart.Start:
                borderRect.X      += Margin.Left;
                borderRect.Y      += Margin.Top;
                borderRect.Width  -= Margin.Left;
                borderRect.Height -= Margin.Top;
                break;
              // the bottom and right margins and paddings, and the top, right, and bottom borders will be rendered
              case NodePart.End:
                borderRect.Width  -= Margin.Right;
                borderRect.Height -= Margin.Bottom;
                break;
            }

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

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/BeginLayout/*"/>
    public sealed override LayoutRegion[] GetChildren()
    {
      return Children;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetChildren/*"/>
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

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNode/*"/>
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
  }
  #endregion

  #region LayoutSpan
  /// <summary>Represents a portion of a <see cref="DocumentNode"/> that can be rendered without line wrapping. There
  /// may be multiple <see cref="LayoutSpan"/> regions referencing the same <see cref="DocumentNode"/> if the document
  /// node was wrapped onto multiple lines.
  /// </summary>
  protected abstract class LayoutSpan : LayoutRegion
  {
    /// <summary>Gets the length of the content from the document node that is rendered in this layout region. This may
    /// be less than <see cref="LayoutRegion.Length"/>, which refers to document indices, depending on
    /// <see cref="HasTrailingNewline"/>.
    /// </summary>
    public int ContentLength
    {
      get { return Length - (HasTrailingNewline ? 1 : 0); }
    }

    /// <summary>Gets or sets the number of pixels from the baseline to the bottom of the region.</summary>
    public int Descent;

    /// <summary>Gets or sets whether the region contains a virtual trailing newline. If true, the length of the
    /// content from the document node is one index less than <see cref="LayoutRegion.Length"/>, to account for the
    /// virtual newline character appended to the region. The <see cref="ContentLength"/> property can be used to
    /// retrieve the correct content length.
    /// </summary>
    public bool HasTrailingNewline;

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/*"/>
    public virtual int LineCount
    {
      get { return 1; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/*"/>
    public abstract LayoutSpan CreateNew(int contentStartIndex);

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/BeginLayout/*"/>
    public sealed override LayoutRegion[] GetChildren()
    {
      return NoChildren;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/GetNextSplitPiece/*"/>
    public abstract SplitPiece GetNextSplitPiece(Graphics gdi, int line, SplitPiece piece, int spaceLeft,
                                                 bool lineIsEmpty);

    static readonly LayoutRegion[] NoChildren = new LayoutRegion[0];
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

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetNode/*"/>
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
      Span = child.Span;
      Size = child.Size;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetRegion/*"/>
    public override LayoutRegion GetRegion(int index)
    {
      if(index == Length) // if the index is at the end of the document, normally no region would contain it. but we
      {                   // want the end of the document to be a valid index, so we'll return something
        // return the innermost child that is not a LineBlock
        LayoutRegion child = Children[0];
        while(true)
        {
          LayoutRegion[] descendants = child.GetChildren();
          LayoutRegion lastDescendant = descendants.Length == 0 ? null : descendants[descendants.Length-1];
          if(lastDescendant == null || lastDescendant is LineBlock) break;
          child = lastDescendant;
        }
        return child;
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
    /// <summary>Initializes this <see cref="TextNodeSpan"/> given the owning <see cref="DocumentEditor"/>, and a
    /// <see cref="TextNode"/> to render.
    /// </summary>
    public TextNodeSpan(TextNode node, DocumentEditor editor)
      : this(node, editor, editor.GetEffectiveFont(node), 0) { }

    TextNodeSpan(TextNode node, DocumentEditor editor, Font font, int startIndex) : base(node)
    {
      Editor     = editor;
      Font       = font;
      StartIndex = startIndex;
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/LineCount/*"/>
    public override int LineCount
    {
      get { return Node.LineCount; }
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/BeginLayout/*"/>
    public override void BeginLayout(Graphics gdi)
    {
      base.BeginLayout(gdi);

      // calculate the font descent, so that adjacent spans of text with different fonts render on the same baseline
      float height = Font.GetHeight(gdi);
      Descent = (int)Math.Round(height - (height * Font.FontFamily.GetCellAscent(Font.Style) /
                                          (float)Font.FontFamily.GetLineSpacing(Font.Style)));
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/CreateNew/*"/>
    public override LayoutSpan CreateNew(int contentStartIndex)
    {
      return new TextNodeSpan(Node, Editor, Font, contentStartIndex);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/GetPixelOffset/*"/>
    public override Size GetPixelOffset(Graphics gdi, int indexOffset)
    {
      if(indexOffset < 0 || indexOffset > Length) throw new ArgumentOutOfRangeException();

      int xPos;
      if(indexOffset == 0) xPos = LeftPBM;
      else if(indexOffset >= ContentLength) xPos = Width - RightPBM;
      else
      {
        const TextFormatFlags MeasureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping |
                                             TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        Size textSize = TextRenderer.MeasureText(gdi, Node.GetText(StartIndex, indexOffset), Font,
                                                 new Size(int.MaxValue, int.MaxValue), MeasureFlags);
        xPos = textSize.Width + LeftPBM;
      }

      return new Size(xPos, TopPBM);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutSpan/GetNextSplitPiece/*"/>
    public override SplitPiece GetNextSplitPiece(Graphics gdi, int line, SplitPiece piece, int spaceLeft,
                                                 bool lineIsEmpty)
    { // strategy for dealing with padding, border, and margin (PBM):
      // for the first piece in the node:
      //   subtract the full horizontal PBM from the available space and fit as many words as possible.
      //   if all words fit
      //     we're done. this span becomes a "Full" span
      //   otherwise
      //     add back the right PBM and add all words that fit, except the last word.      
      //     this span becomes a "Start" span
      // for pieces after the first:
      //   subtract the right horizontal PBM from the available space and fit as many words as possible.
      //   if all words fit
      //     we're done, and this span becomes an "End" span
      //   otherwise
      //     add back the right PBM and add all words that fit, except the last word.      
      //     this span becomes a "Middle" span
      //
      // before returning the piece, the height is increased by the appropriate PBM. the width is increased by the
      // PBM that we subtracted and didn't add back later.
      
      const TextFormatFlags MeasureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping |
                                           TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

      int start = piece != null ? piece.Span.End + piece.Skip : 0, lineLength = Node.GetLineLength(line);
      int charactersLeft = lineLength - start;

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
      Size spanSize = new Size(0, (int)Math.Ceiling(Font.GetHeight(gdi))); // use the font line spacing as the height
      Size wordSize = new Size(), maxTextArea = new Size(int.MaxValue, int.MaxValue);
      Match match = wordRE.Match(cachedText, start, charactersLeft);
      int charactersFit = 0;
      bool addedRight = false; // whether the rightPBM has been 'added back'

      spaceLeft -= RightPBM + (start == 0 ? LeftPBM : 0); // subtract the PBM from the initial amount of space
      while(match.Success)
      {
        Match nextMatch = match.NextMatch();

        wordSize = TextRenderer.MeasureText(gdi, match.Value, Font, maxTextArea, MeasureFlags);
        if(spanSize.Width + wordSize.Width > spaceLeft) // if the word doesn't fit...
        {
          if(addedRight || RightPBM == 0) break; // if we can't add any more space, then we're done

          // otherwise, add the right PBM back to the available space, and see if the word fits now
          spaceLeft += RightPBM;
          addedRight = true;
          // if we shouldn't add the word, or it doesn't fit, then give up
          if(!nextMatch.Success || spanSize.Width + wordSize.Width > spaceLeft) break;
        }

        spanSize.Width += wordSize.Width;
        charactersFit  += match.Length;

        match = nextMatch;
      }

      bool lineWrapped = match.Success; // if the match is still valid, that means there was a word that didn't fit
      if(!lineWrapped) // if all words fit...
      {
        NodePart = start == 0 ? NodePart.Full : NodePart.End;
      }
      else // otherwise, not all words fit.
      {
        NodePart = start == 0 ? NodePart.Start : NodePart.Middle;

        // if there was one big word and it didn't fit...
        if(charactersFit == 0 && !char.IsWhiteSpace(cachedText[start]))
        {
          if(!lineIsEmpty) // if the line is not empty, then we'll return an empty piece with NewLine set to true, so
          {                // we can try again with an empty line, which may have enough space
            return new SplitPiece(new Span(start, 0), new Size(), true);
          }
          else // otherwise, the line is empty, so starting a new line won't help. we need to add the word
          {    // even though it doesn't fit
            spanSize.Width += wordSize.Width;
            charactersFit  += match.Length;
          }
        }
        else // otherwise, we have some words that fit, but one that doesn't
        {
          // there may be some whitespace at the start of the word that does fit, however. so we'll go through it
          // character by character.
          for(int index=start+charactersFit; index < lineLength; index++)
          {
            char c = cachedText[index];
            if(!char.IsWhiteSpace(c)) break;
            Size charSize = TextRenderer.MeasureText(gdi, new string(c, 1), Font, maxTextArea, MeasureFlags);
            if(spanSize.Width + charSize.Width > spaceLeft) break; // if the character didn't fit, we're done
            spanSize.Width += charSize.Width;
            charactersFit++;
          }
        }
      }

      // now update the size and descent based on the PBM
      switch(NodePart)
      {
        case NodePart.Full:
          spanSize.Width  += LeftPBM + RightPBM;
          spanSize.Height += Margin.TotalVertical + Padding.TotalVertical;
          break;
        case NodePart.Start:
          spanSize.Width  += LeftPBM;
          spanSize.Height += Margin.Top + Padding.Top;
          break;
        case NodePart.End:
          spanSize.Width  += RightPBM;
          spanSize.Height += Margin.Bottom + Padding.Bottom;
          break;
      }
      spanSize.Height += Border.Height*2; // we'll display the top/bottom borders all the time
      Descent += Margin.Bottom + Padding.Bottom + Border.Height; // add the bottom PBM to the descent

      // if this is the last piece, and the line ends in a newline character, then add 1 to skip over it
      return new SplitPiece(new Span(start, charactersFit), spanSize, (!lineWrapped && skippedNewLine ? 1 : 0),
                            lineWrapped);
    }

    /// <include file="documentation.xml" path="/UI/DocumentEditor/LayoutRegion/Render/*"/>
    public override void Render(ref RenderData data, Point clientPoint)
    {
      RenderBackgroundAndBorder(ref data, clientPoint);

      const TextFormatFlags DrawFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
                                        TextFormatFlags.SingleLine;
      // TODO: render the selection too
      Color fore = Node.Style.EffectiveForeColor ?? Editor.ForeColor;
      
      // account for the border, margin, and padding, depending on whether this span has them
      if(NodePart == NodePart.Full || NodePart == NodePart.Start)
      {
        clientPoint.Offset(Border.Width + Margin.Left + Padding.Left, Margin.Top + Padding.Top);
      }
      clientPoint.Y += Border.Height; // we always draw the top border if it exists

      TextRenderer.DrawText(data.Graphics, Node.GetText(StartIndex, ContentLength),
                            Font, clientPoint, fore, DrawFlags);
    }

    readonly Font Font;
    readonly DocumentEditor Editor;
    /// <summary>The index within the document node at which this span begins.</summary>
    readonly int StartIndex;
    /// <summary>A string that holds the current line during word-wrapping.</summary>
    string cachedText;

    // TODO: implement splitting at hyphens
    static readonly Regex wordRE = new Regex(@"\s*\S+|\s+", RegexOptions.Singleline | RegexOptions.Compiled);
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

    return null;
  }

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
  Block CreateBlock(Graphics gdi, DocumentNode node, int availableWidth, ref int startIndex)
  {
    Block newBlock = CreateLayoutBlock(node);
    newBlock.BeginLayout(gdi);

    // see if the node has a defined size.
    Measurement? nodeWidth = node.Style.Width, nodeHeight = node.Style.Height;
    if(nodeWidth.HasValue) // if it does, set the 'availableWidth'. later we'll set the node width.
    {
      availableWidth = (int)Math.Round(GetPixels(gdi, node, nodeWidth.Value, availableWidth, Orientation.Horizontal));
    }

    // calculate the horizontal margin and padding
    int hShrinkage, vShrinkage;
    SetBorderMarginAndPadding(gdi, node, newBlock, availableWidth, out hShrinkage, out vShrinkage);

    // now, determine whether any of the children are block nodes
    bool allInline = true;
    foreach(DocumentNode child in node.Children)
    {
      if((child.Layout & RichDocument.Layout.Block) != 0) // if the child is a block node
      {
        allInline = false;
        break;
      }
    }

    newBlock.Start = startIndex++; // create an index for the block so the cursor can be placed in front of it

    if(allInline) // if there are no block children, lay them all out horizontally into a line
    {
      LineBlock lines = LayoutLines(gdi, node, node.Children, availableWidth - hShrinkage, ref startIndex);
      if(lines == null) newBlock.Children = new LayoutRegion[0];
      else
      {
        newBlock.Children = new LayoutRegion[] { lines };
        newBlock.Length = lines.End - newBlock.Start;
        newBlock.Size   = lines.Size;
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
            blocks.Add(LayoutLines(gdi, node, inline, availableWidth - hShrinkage, ref startIndex));
            inline.Clear();
          }

          blocks.Add(CreateBlock(gdi, child, availableWidth - hShrinkage, ref startIndex));
        }
      }

      // if we have some inline nodes left, create a final line block to hold them
      if(inline != null && inline.Count != 0)
      {
        LineBlock lines = LayoutLines(gdi, node, inline, availableWidth - hShrinkage, ref startIndex);
        if(lines != null) blocks.Add(lines);
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

      // calculate the span of the container
      newBlock.Length = newBlock.Children[newBlock.Children.Length-1].End - newBlock.Start;
    }

    // now that the container is exactly the size of its content, we can go back and apply margin and padding
    if(hShrinkage != 0 || vShrinkage != 0)
    {
      int leftAdd = newBlock.Margin.Left + newBlock.Padding.Left + newBlock.Border.Width;
      int  topAdd = newBlock.Margin.Top  + newBlock.Padding.Top  + newBlock.Border.Height;
      foreach(LayoutRegion child in newBlock.Children)
      {
        child.Left += leftAdd;
        child.Top  += topAdd;
      }
      newBlock.Width  += hShrinkage;
      newBlock.Height += vShrinkage;
    }

    // now we'll force the node size if it is defined, but won't go smaller than the content
    if(nodeWidth.HasValue)  newBlock.Width = Math.Max(newBlock.Width, availableWidth);
    if(nodeHeight.HasValue)
    {
      newBlock.Height = Math.Max(newBlock.Height,
                                 (int)Math.Round(GetPixels(gdi, node, nodeHeight.Value, 0, Orientation.Vertical)));
    }

    ApplyHorizontalAlignment(node.Style.HorizontalAlignment, newBlock);
    return newBlock;
  }

  /// <summary>Performs the layout of the document and returns the layout, represented by a <see cref="RootRegion"/>
  /// object.
  /// </summary>
  RootRegion CreateRootBlock(Graphics gdi)
  {
    int startIndex = 0;
    return new RootRegion(CreateBlock(gdi, Document.Root, CanvasRectangle.Width, ref startIndex));
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
    if(measurement.Size == 0) return 0;

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
  LineBlock LayoutLines(Graphics gdi, DocumentNode parent, ICollection<DocumentNode> inlineNodes,
                        int availableWidth, ref int startIndex)
  {
    // the method creates a list of lines, each holding spans, from a list of nodes.

    List<Line> lines = new List<Line>(); // holds the lines for all of these nodes
    List<LayoutSpan> spans = new List<LayoutSpan>(); // holds the spans in the current line
    Size lineSize = new Size(); // the size of the current line so far

    LayoutSpan span = null;
    foreach(DocumentNode node in inlineNodes)
    {
      span = CreateLayoutSpan(node);
      if(span == null) continue; // if this node cannot be rendered, skip to the next one
      span.BeginLayout(gdi);
      int nodeIndex = 0; // the index within the node at which the current span begins

      int hShrinkage, vShrinkage;
      SetBorderMarginAndPadding(gdi, node, span, availableWidth, out hShrinkage, out vShrinkage);

      span.Start = startIndex;
      for(int lineIndex=0; lineIndex < span.LineCount; lineIndex++) // for each line in the document node
      {
        if(lineIndex != 0) // if there was a line break in the document node (and hence a line other than the first),
        {                  // start a new output line too
          FinishLine(gdi, lines, spans, ref span, ref lineSize, ref startIndex, nodeIndex);
        }

        SplitPiece piece = null;
        while(true)
        {
          int spaceLeft = availableWidth - lineSize.Width;
          piece = span.GetNextSplitPiece(gdi, lineIndex, piece, spaceLeft, spaceLeft == availableWidth);
          if(piece == null) break;

          // so extend the size of the current line and span to encompass the new piece
          lineSize.Width += piece.Size.Width;
          lineSize.Height = Math.Max(lineSize.Height, piece.Size.Height);

          span.Size = new Size(span.Width + piece.Size.Width, Math.Max(span.Height, piece.Size.Height));
          
          span.Length += piece.Span.Length; // increase the span length of the size of the split piece
          startIndex   = span.End; // and update the document index
          nodeIndex    = piece.Span.End + piece.Skip; // and update the node index

          // if we need to start a new line before we can receive more of the content, do so
          if(piece.NewLine)
          {
            FinishLine(gdi, lines, spans, ref span, ref lineSize, ref startIndex, nodeIndex);
          }
        }
      }

      // now we've finished the current document node, so output the span (if it's not empty)
      if(span.Length != 0) spans.Add(span);
    }

    // if the last line is not empty, add it
    if(spans.Count != 0)
    {
      if(span.Length != 0)
      {
        // add a virtual newline to the end of the final span (which has already been added to the line)
        span.HasTrailingNewline = true;
        span.Length++;
        startIndex++;
      }

      span = null; // the span has already been added, so don't add it again
      FinishLine(gdi, lines, spans, ref span, ref lineSize, ref startIndex, 0);
    }

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

      // stack the spans horizontally within the line and calculate the maximum descent
      int maxDescent = 0;
      foreach(LayoutSpan s in line.Children)
      {
        s.Position  = new Point(line.Width, 0);
        line.Width += s.Width;
        if(s.Descent > maxDescent) maxDescent = s.Descent;
      }

      // now go back through and position the spans vertically
      // TODO: implement vertical alignment
      foreach(LayoutSpan s in line.Children)
      {
        s.Top += line.Height - s.Height + s.Descent - maxDescent; // just do bottom alignment for now...
      }

      // TODO: implement horizontal alignment
      line.Position = new Point(0, block.Height);
      block.Width   = Math.Max(line.Width, block.Width);
      block.Height += line.Height;
    }

    if(block.Children.Length == 0) return null;

    ApplyHorizontalAlignment(parent.Style.HorizontalAlignment, block);
    block.Start  = block.Children[0].Start;
    block.Length = block.Children[block.Children.Length-1].End - block.Start;
    return block;
  }

  void SetBorderMarginAndPadding(Graphics gdi, DocumentNode node, LayoutRegion region, int availableWidth,
                                 out int hShrinkage, out int vShrinkage)
  {
    FourSide fourSide = node.Style.Margin;
    if(fourSide == null)
    {
      hShrinkage = vShrinkage = 0;
    }
    else
    {
      region.Margin.Left   = (int)Math.Round(GetPixels(gdi, node, fourSide.Left, availableWidth, Orientation.Horizontal));
      region.Margin.Right  = (int)Math.Round(GetPixels(gdi, node, fourSide.Right, availableWidth, Orientation.Horizontal));
      // TODO: we need a base measurement to use for relative calculations
      region.Margin.Top    = (int)Math.Round(GetPixels(gdi, node, fourSide.Top, 0, Orientation.Vertical));
      region.Margin.Bottom = (int)Math.Round(GetPixels(gdi, node, fourSide.Bottom, 0, Orientation.Vertical));
      hShrinkage = region.Margin.TotalHorizontal;
      vShrinkage = region.Margin.TotalVertical;
    }

    fourSide = node.Style.Padding;
    if(fourSide != null)
    {
      region.Padding.Left   = (int)Math.Round(GetPixels(gdi, node, fourSide.Left, availableWidth, Orientation.Horizontal));
      region.Padding.Right  = (int)Math.Round(GetPixels(gdi, node, fourSide.Right, availableWidth, Orientation.Horizontal));
      // TODO: we need a base measurement to use for relative calculations
      region.Padding.Top    = (int)Math.Round(GetPixels(gdi, node, fourSide.Top, 0, Orientation.Vertical));
      region.Padding.Bottom = (int)Math.Round(GetPixels(gdi, node, fourSide.Bottom, 0, Orientation.Vertical));
      hShrinkage += region.Padding.TotalHorizontal;
      vShrinkage += region.Padding.TotalVertical;
    }

    Measurement measurement = node.Style.BorderWidth;
    if(measurement.Size != 0 && node.Style.BorderStyle != RichDocument.BorderStyle.None)
    {
      // TODO: we need a base measurement to use for relative calculations
      region.Border =
        new Size((int)Math.Round(GetPixels(gdi, node, measurement, availableWidth, Orientation.Horizontal)),
                 (int)Math.Round(GetPixels(gdi, node, measurement, 0, Orientation.Vertical)));
      hShrinkage += region.Border.Width  * 2;
      vShrinkage += region.Border.Height * 2;
    }
  }

  /// <summary>A helper for <see cref="LayoutLines"/> which ends the current line.</summary>
  static void FinishLine(Graphics gdi, List<Line> lines, List<LayoutSpan> spans, ref LayoutSpan span, 
                         ref Size lineSize, ref int docStartIndex, int nodeStartIndex)
  {
    if(span != null)
    {
      if(span.Length != 0) // if the current span contains something, add that to the line first
      {
        // the line is being wrapped, so add a virtual newline character to the end of the final span, and account for
        // it in the document index
        span.HasTrailingNewline = true;
        span.Length++;
        docStartIndex++;
        spans.Add(span);

        LayoutSpan newSpan = span.CreateNew(nodeStartIndex);
        newSpan.BeginLayout(gdi);
        newSpan.Border  = span.Border;
        newSpan.Margin  = span.Margin;
        newSpan.Padding = span.Padding;
        span = newSpan;
      }

      span.Start = docStartIndex;
    }

    Line line = new Line(spans.ToArray());
    line.BeginLayout(gdi);
    line.Height = lineSize.Height;
    line.Start  = docStartIndex; // this span will be overwritten later if the line is not empty...
    line.Length = 1;
    lines.Add(line);

    spans.Clear();
    lineSize = new Size();
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
      SetCursor(Math.Min(cursorIndex, IndexLength));
    }
  }

  /// <summary>Invalidates the entire layout.</summary>
  protected void InvalidateLayout()
  {
    rootBlock = null;
    DeselectAll();
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

  /// <include file="documentation.xml" path="/UI/Document/OnNodeChanged/*"/>
  protected virtual void OnNodeChanged(Document document, DocumentNode node)
  {
    InvalidateNodeLayout(node);
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
    RenderData data = new RenderData(e.Graphics, this, Rectangle.Intersect(e.ClipRectangle, canvasRect), Selection);
    rootBlock.Render(ref data, new Point(canvasRect.X - scrollPosition.X, canvasRect.Y - scrollPosition.Y));

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

  /// <summary>Determines whether the given key is an input key that should be passed through <see cref="OnKeyDown"/>.</summary>
  protected override bool IsInputKey(Keys keyData)
  {
    switch(keyData)
    {
      case Keys.Left: case Keys.Right: case Keys.Up: case Keys.Down:
        return true;
      default: return base.IsInputKey(keyData);
    }
  }

  /// <summary>Called when a key on the keyboard is depressed.</summary>
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);
    if(e.Handled) return;

    if(e.KeyCode == Keys.Left)
    {
      CursorIndex = Math.Max(0, CursorIndex-1);
    }
    else if(e.KeyCode == Keys.Right)
    {
      CursorIndex = Math.Min(IndexLength, CursorIndex+1);
    }
    else if(e.KeyCode == Keys.Home && e.Modifiers == Keys.Control)
    {
      CursorIndex = 0;
    }
    else if(e.KeyCode == Keys.End && e.Modifiers == Keys.Control)
    {
      CursorIndex = IndexLength;
    }
  }

  /// <summary>Called when the control loses input focus.</summary>
  protected override void OnLostFocus(EventArgs e)
  {
    base.OnLostFocus(e);
    HideCursor();
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
      else // otherwise, the layout is still valid
      {
        if(hScrollBar != null || vScrollBar != null) // we need to resize the scrollbars to fit the control
        {
          using(Graphics gdi = Graphics.FromHwnd(Handle)) ResizeScrollbars(gdi);
        }

        // and, we need to update the cursor, which may be need to be hidden or shown, depending on the new size
        UpdateCursor();
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
    Layout();
    return rootBlock.GetRegion(index);
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
    if(index < 0 || index > IndexLength) throw new ArgumentOutOfRangeException();

    Point canvasOffset = CanvasRectangle.Location;
    LayoutRegion region = GetRegion(index);
    Size pixelOffset = region.GetPixelOffset(gdi, index-region.Start);

    cursorIndex = index;
    SetCursor(new Point(region.AbsoluteLeft + canvasOffset.X + ScrollPosition.X + pixelOffset.Width,
                        region.AbsoluteTop  + canvasOffset.Y + ScrollPosition.Y + pixelOffset.Height),
              region.Height);
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
  /// resized.
  /// </summary>
  void UpdateCursor()
  {
    if(cursorRect.Width != 0) SetCursor(cursorRect.Location, cursorRect.Height);
    else SetCursor(cursorIndex);
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
  HScrollBar hScrollBar;
  VScrollBar vScrollBar;
  /// <summary>The area of the text cursor within the control.</summary>
  Rectangle cursorRect;
  int cursorIndex, hScrollPos, vScrollPos, controlWidth;
  Span selection;
  ScrollBars scrollBars;
  System.Windows.Forms.BorderStyle borderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
  bool readOnly, layoutSuspended, repaintNeeded, cursorVisible;

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
}

} // namespace AdamMil.UI.RichDocument