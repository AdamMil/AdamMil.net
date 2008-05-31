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
using System.Drawing;
using System.Windows.Forms;
using AdamMil.Collections;

namespace AdamMil.UI.RichDocument
{

#region DocumentNodeEvent
/// <summary>A delegate used to represent events that occur with documents and document nodes.</summary>
public delegate void DocumentNodeEvent(Document document, DocumentNode node);
#endregion

#region Unit
/// <summary>Represents a unit of measurement.</summary>
public enum Unit
{
  /// <summary>The measurement is in pixels on the output device.</summary>
  Pixels,
  /// <summary>The measurement is in multiples of the size of the font, or of a container's font.</summary>
  FontRelative,
  /// <summary>The measurement is in millimeters.</summary>
  Millimeters,
  /// <summary>The measurement is in inches.</summary>
  Inches,
  /// <summary>The measurement is in points (1 point is 1/72 inch).</summary>
  Points,
  /// <summary>The measurement is in percent, usually compared to some feature of a container.</summary>
  Percent
}
#endregion

#region Measurement
/// <summary>Represents a measurement, containing a non-negative size and a unit of measurement.</summary>
public struct Measurement
{
  /// <summary>Initializes the measurement with the given size and unit.</summary>
  public Measurement(float size, Unit unit)
  {
    if(size < 0) throw new ArgumentOutOfRangeException();
    this.size = size;
    this.unit = unit;
  }

  /// <summary>Gets the size of the measurement.</summary>
  public float Size
  {
    get { return size; }
    set
    {
      if(value < 0) throw new ArgumentOutOfRangeException();
      size = value;
    }
  }

  /// <summary>Gets the unit of measurement.</summary>
  public Unit Unit
  {
    get { return unit; }
    set { Unit = value; }
  }

  /// <include file="documentation.xml" path="/UI/Common/Equals/*"/>
  public override bool Equals(object obj)
  {
    return obj is Measurement ? this == (Measurement)obj : false;
  }

  /// <include file="documentation.xml" path="/UI/Common/Equals/*"/>
  public bool Equals(Measurement other)
  {
    return this == other;
  }

  /// <include file="documentation.xml" path="/UI/Common/GetHashCode/*"/>
  public override int GetHashCode()
  {
    return size.GetHashCode() ^ (int)unit;
  }

  /// <include file="documentation.xml" path="/UI/Common/OpEquals/*"/>
  public static bool operator==(Measurement a, Measurement b)
  {
    return a.Size == b.Size && a.Unit == b.Unit;
  }

  /// <include file="documentation.xml" path="/UI/Common/OpNotEquals/*"/>
  public static bool operator!=(Measurement a, Measurement b)
  {
    return a.Size != b.Size || a.Unit != b.Unit;
  }

  float size;
  Unit unit;
}
#endregion

#region FourSide
/// <summary>Contains four measurements, representing the four sides of a rectangle. This object is used, for instance,
/// to specify the padding or margin around an object.
/// </summary>
public sealed class FourSide
{
  /// <summary>Initializes this <see cref="FourSide"/> with all sides equal to 0 pixels.</summary>
  public FourSide() { }
  /// <summary>Initializes this <see cref="FourSide"/> with all sides equal to the given measurement.</summary>
  public FourSide(Measurement amount) : this(amount, amount, amount, amount) { }
  /// <summary>Initializes this <see cref="FourSide"/> with the left and right sides equal to the horizontal
  /// measurement, and the top and bottom sides equal to the vertical measurement.
  /// </summary>
  public FourSide(Measurement horizontal, Measurement vertical) : this(horizontal, vertical, horizontal, vertical) { }
  /// <summary>Initializes this <see cref="FourSide"/> with the left and right sides equal to the given measurements,
  /// and the top and bottom sides equal to the vertical measurement.
  /// </summary>
  public FourSide(Measurement left, Measurement vertical, Measurement right) : this(left, vertical, right, vertical) { }
  /// <summary>Initializes this <see cref="FourSide"/> with the given measurements.</summary>
  public FourSide(Measurement left, Measurement top, Measurement right, Measurement bottom)
  {
    Left   = left;
    Top    = top;
    Right  = right;
    Bottom = bottom;
  }

  /// <summary>The measurement of the left side.</summary>
  public Measurement Left;
  /// <summary>The measurement of the top side.</summary>
  public Measurement Top;
  /// <summary>The measurement of the right side.</summary>
  public Measurement Right;
  /// <summary>The measurement of the bottom side.</summary>
  public Measurement Bottom;

  /// <include file="documentation.xml" path="/UI/Common/Equals/*"/>
  public override bool Equals(object obj)
  {
    FourSide other = obj as FourSide;
    return other == null ? false : this == other;
  }

  /// <include file="documentation.xml" path="/UI/Common/Equals/*"/>
  public bool Equals(FourSide other)
  {
    return this == other;
  }

  /// <include file="documentation.xml" path="/UI/Common/GetHashCode/*"/>
  public override int GetHashCode()
  {
    return Left.GetHashCode() ^ Top.GetHashCode() ^ Right.GetHashCode() ^ Bottom.GetHashCode();
  }

  /// <summary>Sets all sides to the given measurement.</summary>
  public void SetAll(Measurement value)
  {
    Top = Left = Right = Bottom = value;
  }

  /// <summary>Sets the left and right sides to the given measurement.</summary>
  public void SetHorizontal(Measurement value)
  {
    Left = Right = value;
  }

  /// <summary>Sets the top and bottom sides to the given measurement.</summary>
  public void SetVertical(Measurement value)
  {
    Top = Bottom = value;
  }

  /// <include file="documentation.xml" path="/UI/Common/OpEquals/*"/>
  public static bool operator==(FourSide a, FourSide b)
  {
    return a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;
  }

  /// <include file="documentation.xml" path="/UI/Common/OpNotEquals/*"/>
  public static bool operator!=(FourSide a, FourSide b)
  {
    return a.Left != b.Left || a.Top != b.Top || a.Right != b.Right || a.Bottom != b.Bottom;
  }
}
#endregion

#region Style
/// <summary>Represents the style of a node and its descendants.</summary>
/// <remarks>The style option values are stored in a dictionary indexed by strings. Options normally cascade from
/// parent to child. Options that should not cascade, such as padding and margin, must have key names that begin with
/// '*' (an asterisk).
/// </remarks>
public class Style
{
  /// <summary>Initializes this <see cref="Style"/> with the node whose style it will control.</summary>
  public Style(DocumentNode owner)
  {
    if(owner == null) throw new ArgumentNullException();
    this.owner = owner;
  }

  /// <summary>Initializes this <see cref="Style"/> with the node whose style it will control and a style to clone.</summary>
  /// <param name="owner">The document node whose style this class will control.</param>
  /// <param name="prototype">A style upon which the new style will be based.</param>
  /// <param name="deepCopy">If true, the effective values (the values found by searching the node's ancestors until a
  /// value is found) of all style options will be copied. Otherwise, only the local values will be copied.
  /// </param>
  public Style(DocumentNode owner, Style prototype, bool deepCopy) : this(owner)
  {
    options = prototype.options == null ? null : new Dictionary<string,object>(prototype.options);

    // if we're doing a deep copy, we need to copy the values all the way up the inheritance chain
    if(deepCopy)
    {
      // assume that at least something is set...
      if(options == null) options = new Dictionary<string,object>();
      
      for(DocumentNode node = owner.Parent; node != null; node = node.Parent) // for each ancestor...
      {
        foreach(KeyValuePair<string,object> pair in node.Style.options)
        {
          // copy each option if it's inheritable (ie, doesn't start with '*') and doesn't already exist
          string key = (string)pair.Key;
          if((key.Length == 0 || key[0] != '*') && !options.ContainsKey(pair.Key)) options[pair.Key] = pair.Value;
        }
      }
      
      if(options.Count == 0) options = null;
    }
  }

  /// <summary>Gets or sets the local background color.</summary>
  public Color? BackColor
  {
    get { return GetNullableOption<Color>("BackColor", false); }
    set { SetNullableOption("BackColor", value, value != EffectiveBackColor); }
  }

  /// <summary>Gets the effective background color.</summary>
  public Color? EffectiveBackColor
  {
    get { return GetNullableOption<Color>("BackColor", true); }
  }

  /// <summary>Gets or sets the local foreground color.</summary>
  public Color? ForeColor
  {
    get { return GetNullableOption<Color>("ForeColor", false); }
    set { SetNullableOption("ForeColor", value, value != EffectiveForeColor); }
  }

  /// <summary>Gets the effective foreground color.</summary>
  public Color? EffectiveForeColor
  {
    get { return GetNullableOption<Color>("ForeColor", true); }
  }

  /// <summary>Gets or sets the local font names.</summary>
  public string[] FontNames
  {
    get { return GetOption<string[]>("FontNames", false); }
    set
    {
      if(value != null)
      {
        foreach(string fontName in value)
        {
          if(string.IsNullOrEmpty(fontName)) throw new ArgumentException("A font name was null or empty.");
        }
      }

      SetOption("FontNames", value, value != EffectiveFontNames); // this would ideally do a deep comparison, but it
    }                                                             // shouldn't hurt too much to not do one...
  }

  /// <summary>Gets the effective font names.</summary>
  public string[] EffectiveFontNames
  {
    get { return GetOption<string[]>("FontNames", true); }
  }

  /// <summary>Gets or sets the local font size.</summary>
  public Measurement? FontSize
  {
    get { return GetNullableOption<Measurement>("FontSize", false); }
    set { SetNullableOption("FontSize", value, value != EffectiveFontSize); }
  }

  /// <summary>Gets or sets the effective font size.</summary>
  public Measurement? EffectiveFontSize
  {
    get { return GetNullableOption<Measurement>("FontSize", true); }
  }

  /// <summary>Gets or sets the local font style.</summary>
  public FontStyle? FontStyle
  {
    get { return GetNullableOption<FontStyle>("FontStyle", false); }
    set { SetNullableOption("FontStyle", value, value != EffectiveFontStyle); }
  }

  /// <summary>Gets or sets the effective font style.</summary>
  public FontStyle? EffectiveFontStyle
  {
    get { return GetNullableOption<FontStyle>("FontStyle", true); }
  }

  /// <summary>Gets or sets the local margin. This is also the effective margin, as the margin is not inheritable.</summary>
  public FourSide Margin
  {
    get { return GetOption<FourSide>("*Margin", false); }
    set { SetOption("*Margin", value, value != Margin); }
  }

  /// <summary>Gets or sets the local padding. This is also the effective padding, as padding is not inheritable.</summary>
  public FourSide Padding
  {
    get { return GetOption<FourSide>("*Padding", false); }
    set { SetOption("*Padding", value, value != Padding); }
  }

  /// <include file="documentation.xml" path="/UI/Style/GetOption/*"/>
  /// <returns>Returns the option value, or the default value for the type if no value was found.</returns>
  protected T? GetNullableOption<T>(string optionName, bool searchAncestors) where T : struct
  {
    object value;
    if(searchAncestors)
    {
      Style style = this;
      do
      {
        if(style.options != null && style.options.TryGetValue(optionName, out value)) return (T)value;

        DocumentNode parent = style.owner.Parent;
        style = parent == null ? null : parent.Style;
      } while(style != null);

      return null;
    }
    else
    {
      return options != null && options.TryGetValue(optionName, out value) ? (T?)value : null;
    }
  }

  /// <include file="documentation.xml" path="/UI/Style/GetOption/*"/>
  /// <returns>Returns the option value, or null if no value was found.</returns>
  protected T GetOption<T>(string optionName, bool searchAncestors)
  {
    object value;
    if(searchAncestors)
    {
      Style style = this;
      do
      {
        if(style.options != null && style.options.TryGetValue(optionName, out value)) return (T)value;

        DocumentNode parent = style.owner.Parent;
        style = parent == null ? null : parent.Style;
      } while(style != null);

      return default(T);
    }
    else
    {
      return options != null && options.TryGetValue(optionName, out value) ? (T)value : default(T);
    }
  }

  /// <include file="documentation.xml" path="/UI/Style/SetOption/*"/>
  protected void SetOption<T>(string optionName, T value, bool triggerNodeChange)
  {
    if(options == null)
    {
      if(value == null) return;
      options = new Dictionary<string, object>();
    }

    if(value != null) options[optionName] = value;
    else options.Remove(optionName);

    if(triggerNodeChange) owner.OnNodeChanged();
  }

  /// <include file="documentation.xml" path="/UI/Style/SetOption/*"/>
  protected void SetNullableOption<T>(string optionName, T? value, bool triggerNodeChange) where T : struct
  {
    if(options == null)
    {
      if(!value.HasValue) return;
      options = new Dictionary<string,object>();
    }

    if(value.HasValue) options[optionName] = value.Value;
    else options.Remove(optionName);

    if(triggerNodeChange) owner.OnNodeChanged();
  }

  internal readonly DocumentNode owner;
  Dictionary<string,object> options;
}
#endregion

#region Document
/// <summary>A <see cref="Document"/> is the data structure that stores a document's content and metadata.</summary>
/// <remarks>
/// The <see cref="Document"/> object only contains the document's data, and in particular does not contain any
/// user interface functionality. To render a document, use the <see cref="DocumentEditor"/>.
/// </remarks>
public class Document
{
  /// <summary>Initializes a new, empty document.</summary>
  public Document()
  {
    rootNode = new RootNode(this);
  }

  /// <summary>This event is raised when a node (or one of the node's descendants) has changed such that it may need
  /// to be rerendered.
  /// </summary>
  public event DocumentNodeEvent NodeChanged;

  /// <summary>Gets or sets the maximum number of change events that can be stored by the document. A value of -1
  /// indicates that there is no limit. The default is -1. A value of zero is not allowed.
  /// </summary>
  /// <remarks>The value zero is not allowed because it doesn't make much sense to enable undo but not allow any undo
  /// events to be stored. If you want to disable change events, set <see cref="UndoEnabled"/> to false.
  /// </remarks>
  public int UndoLimit
  {
    get { return undoLimit; }
    set
    {
      if(undoLimit != value)
      {
        if(value < -1) throw new ArgumentOutOfRangeException("UndoLimit", value, "UndoLimit cannot be less than -1.");
        if(value == 0)
        {
          throw new ArgumentOutOfRangeException("UndoLimit", "Rather than setting UndoLimit to zero, "+
                                                             "set UndoEnabled to false.");
        }

        bool newLimitIsSmaller = value < undoLimit;
        undoLimit = value;
        if(newLimitIsSmaller) EnforceUndoLimit();
      }
    }
  }

  /// <summary>Gets or sets whether undo is enabled. The default is false.</summary>
  /// <remarks>Enabling undo information stores all of the changes to the document in memory, which can take a
  /// significant amount of extra space.
  /// </remarks>
  public bool UndoEnabled
  {
    get { return undoEnabled; }
    set
    {
      if(value != undoEnabled)
      {
        undoEnabled = value;
        if(!undoEnabled) ClearChangeEvents();
      }
    }
  }
  
  /// <summary>Gets the root node of the document. Use this node to add content to the document.</summary>
  public DocumentNode Root
  {
    get { return rootNode; }
  }

  /// <summary>Performs the change and then, if undo is enabled, adds the change to the document. If undo is not
  /// enabled, the change event is disposed of (by calling <see cref="ChangeEvent.Dispose()"/>).
  /// </summary>
  public void AddChangeEvent(ChangeEvent change)
  {
    if(change == null) throw new ArgumentNullException();

    change.Do();
    version++;

    if(UndoEnabled) // if undo is enabled, keep the change event around
    {
      RemoveSubsequentChanges();
      changeEvents.Add(change);
      undoPosition++;
      EnforceUndoLimit();
    }
    else // otherwise dispose of the event immediately
    {
      change.Dispose();
    }
  }

  /// <summary>Redoes the next change to the document.</summary>
  public void Redo()
  {
    if(!UndoEnabled) throw new InvalidOperationException("Undo is disabled.");
    if(undoPosition == changeEvents.Count) throw new InvalidOperationException("There are no more change events.");
    changeEvents[undoPosition++].Do();
    version++;
  }

  /// <summary>Undoes the last change to the document.</summary>
  public void Undo()
  {
    if(!UndoEnabled) throw new InvalidOperationException("Undo is disabled.");
    if(undoPosition == 0) throw new InvalidOperationException("There are no more change events.");
    changeEvents[--undoPosition].Undo();
    version--;
  }

  /// <include file="documentation.xml" path="/UI/Document/OnNodeChanged/*"/>
  protected internal virtual void OnNodeChanged(DocumentNode node)
  {
    if(node == null) throw new ArgumentNullException();
    if(NodeChanged != null) NodeChanged(this, node);
  }

  /// <summary>Gets the version of the document. The version number is incremented after each change, and decremented
  /// after each undo operation.
  /// </summary>
  internal uint Version
  {
    get { return version; }
  }

  /// <summary>Removes all change events from the document.</summary>
  void ClearChangeEvents()
  {
    foreach(ChangeEvent change in changeEvents) change.Dispose();
    changeEvents.Clear();
    changeEvents.Capacity = Math.Min(changeEvents.Capacity, 64); // shrink the capacity of the buffer somewhat
    undoPosition = 0; // reset the undo cursor
  }

  /// <summary>Ensures that there are not more than <see cref="UndoLimit"/> change events in the document by removing
  /// old events as necessary.
  /// </summary>
  void EnforceUndoLimit()
  {
    if(UndoLimit != -1)
    {
      int toRemove = changeEvents.Count - UndoLimit;
      if(toRemove > 0)
      {
        // we'll first remove from the leftmost side up to the undo position (ie, we'll remove the oldest undo events)
        while(toRemove != 0 && undoPosition != 0)
        {
          changeEvents.RemoveFirst().Dispose();
          toRemove--;
          undoPosition--;
        }

        // then, we'll remove from the rightmost side of the change list (ie, we'll remove the newest redo events)
        while(toRemove-- != 0) changeEvents.RemoveLast().Dispose();
      }
    }
  }

  /// <summary>Removes all changes after the current undo position. That is, all redo events.</summary>
  /// <remarks>This method is called before a new change event is added, because adding a new change invalidates all
  /// subsequent changes in the history.</remarks>
  void RemoveSubsequentChanges()
  {
    while(undoPosition < changeEvents.Count) changeEvents.RemoveLast().Dispose();
  }

  /// <summary>The root node of the document.</summary>
  readonly DocumentNode rootNode;

  /// <summary>The change events in the document.</summary>
  readonly CircularList<ChangeEvent> changeEvents = new CircularList<ChangeEvent>();

  /// <summary>The version number of the document.</summary>
  uint version;

  /// <summary>How many change events are allowed to be stored.</summary>
  int undoLimit = -1;

  /// <summary>Where the "cursor" is in the chain of change events. The event to undo is at undoPosition-1 and the event
  /// to redo is at undoPosition.
  /// </summary>
  int undoPosition;

  /// <summary>Whether or not undo information is being tracked.</summary>
  bool undoEnabled;
}
#endregion

} // namespace AdamMil.UI.RichDocument