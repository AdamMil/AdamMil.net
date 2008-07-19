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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using AdamMil.UI.Wizard.Design;

namespace AdamMil.UI.Wizard
{

#region Wizard
/// <summary>A wizard control, which displays one or more <see cref="WizardStep">steps</see> and provides navigation
/// between them.
/// </summary>
[Designer(typeof(WizardDesigner)), DefaultEvent("StepChanged"), DefaultProperty("Steps")]
public class Wizard : Control
{
  /// <summary>Initializes a new <see cref="Wizard"/> control.</summary>
  public Wizard()
  {
    steps = new StepCollection(this);
    InitializeComponent();
  }

  #region StepCollection
  /// <summary>A collection of <see cref="WizardStep"/> objects.</summary>
  [Serializable]
  public sealed class StepCollection : Collection<WizardStep>
  {
    internal StepCollection(Wizard owner)
    {
      if(owner == null) throw new ArgumentNullException();
      this.owner = owner;
    }

    /// <include file="documentation.xml" path="/UI/Collection/ClearItems/*"/>
    protected override void ClearItems()
    {
      foreach(WizardStep step in this) OnRemove(step);

      base.ClearItems();

      owner.CurrentStepIndex = -1;
    }

    /// <include file="documentation.xml" path="/UI/Collection/InsertItem/*"/>
    protected override void InsertItem(int index, WizardStep item)
    {
      ValidateItem(item);
      base.InsertItem(index, item);

      OnAdded(item, index);
      FixIndicesFrom(index);

      if(owner.CurrentStepIndex >= index)
      {
        owner.currentStep++;
        owner.UpdateButtons();
      }
      else if(owner.CurrentStepIndex == -1) owner.CurrentStepIndex = 0;
      else owner.UpdateButtons();
    }

    /// <include file="documentation.xml" path="/UI/Collection/RemoveItem/*"/>
    protected override void RemoveItem(int index)
    {
      OnRemove(this[index]);

      base.RemoveItem(index);
      FixIndicesFrom(index);

      if(owner.CurrentStepIndex > index)
      {
        owner.currentStep--;
        owner.UpdateButtons();
      }
      else if(owner.CurrentStepIndex == index)
      {
        if(index < Count-1) owner.OnStepChanged(EventArgs.Empty);
        else owner.CurrentStepIndex--;
      }
    }

    /// <include file="documentation.xml" path="/UI/Collection/SetItem/*"/>
    protected override void SetItem(int index, WizardStep item)
    {
      if(item != this[index])
      {
        ValidateItem(item);
        
        OnRemove(this[index]);
        base.SetItem(index, item);
        OnAdded(item, index);

        if(index == owner.CurrentStepIndex) owner.OnStepChanged(EventArgs.Empty);
      }
    }

    /// <summary>Renumbers all indices from <paramref name="index"/> to the end of the list.</summary>
    void FixIndicesFrom(int index)
    {
      for(; index < Count; index++) this[index].index = index;
    }

    /// <summary>Binds a <see cref="WizardStep"/> to the wizard.</summary>
    void OnAdded(WizardStep item, int index)
    {
      item.index   = index;
      item.owner   = owner;
      item.Visible = false;
      if(!owner.stepContainer.Controls.Contains(item)) owner.stepContainer.Controls.Add(item);
    }

    /// <summary>Unbinds a <see cref="WizardStep"/> from the wizard.</summary>
    void OnRemove(WizardStep item)
    {
      owner.stepContainer.Controls.Remove(item);
      item.index   = -1;
      item.owner   = null;
      item.Visible = true;
    }

    readonly Wizard owner;

    /// <summary>Makes sure the item is not null and that it doesn't already belong to a wizard.</summary>
    static void ValidateItem(WizardStep item)
    {
      if(item == null) throw new ArgumentNullException();
      if(item.owner != null) throw new ArgumentException("This step already belongs to a wizard.");
    }
  }
  #endregion

  /// <summary>Raised when the Back button is clicked.</summary>
  [Category("Action"), Description("Raised when the Back button is clicked.")]
  public event CancelEventHandler BackButtonClicked;

  /// <summary>Raised when the Cancel button is clicked.</summary>
  [Category("Action"), Description("Raised when the Cancel button is clicked.")]
  public event CancelEventHandler CancelButtonClicked;

  /// <summary>Raised when the Finish button is clicked.</summary>
  [Category("Action"), Description("Raised when the Finish button is clicked.")]
  public event CancelEventHandler FinishButtonClicked;

  /// <summary>Raised when the Help button is clicked.</summary>
  [Category("Action"), Description("Raised when the Help button is clicked.")]
  public event EventHandler HelpButtonClicked;

  /// <summary>Raised when the Next button is clicked.</summary>
  [Category("Action"), Description("Raised when the Next button is clicked.")]
  public event CancelEventHandler NextButtonClicked;

  /// <summary>Raised when current <see cref="WizardStep"/> changes.</summary>
  [Category("Property Changed"), Description("Raised when the current step changes.")]
  public event EventHandler StepChanged;

  /// <summary>Gets or sets the current <see cref="WizardStep"/>. If null, no wizard step is displayed.</summary>
  [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public WizardStep CurrentStep
  {
    get { return CurrentStepIndex == -1 ? null : Steps[CurrentStepIndex]; }
    set
    {
      if(value == null) CurrentStepIndex = -1;
      else if(value.Wizard != this) throw new ArgumentException("The step does not belong to this wizard.");
      else CurrentStepIndex = value.Index;
    }
  }

  /// <summary>Gets or sets the current <see cref="WizardStep"/>, based on its index. If -1, no wizard step is
  /// displayed.
  /// </summary>
  [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int CurrentStepIndex
  {
    get { return currentStep; }
    set
    {
      if(value != CurrentStepIndex)
      {
        if(value < -1 || value >= Steps.Count) throw new ArgumentOutOfRangeException();
        currentStep = value;
        OnStepChanged(EventArgs.Empty);
      }
    }
  }

  /// <summary>Gets or sets whether the Next (or Finish) button is enabled. This property will be reset to true
  /// whenever the current step changes, so you'll have to set it in the <see cref="StepChanged"/> or
  /// <see cref="WizardStep.StepDisplayed"/> event handlers if you want to keep it false.
  /// </summary>
  [Browsable(false), DefaultValue(true)]
  public bool EnableNextButton
  {
    get { return nextButtonEnabled; }
    set
    {
      nextButtonEnabled = value;
      UpdateButtons();
    }
  }

  /// <summary>Gets or sets whether the Help button is displayed.</summary>
  [Category("Appearance"), Description("Determines whether the Help button will be displayed."), DefaultValue(true)]
  public bool ShowHelpButton
  {
    get { return btnHelp.Visible; }
    set { btnHelp.Visible = value; }
  }

  /// <summary>Gets a collection containing the <see cref="WizardStep">steps</see> in the wizard.</summary>
  [Category("Behavior"), Description("A collection of the steps displayed in the wizard."), MergableProperty(false)]
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
  [Editor(typeof(StepCollectionEditor), typeof(UITypeEditor))]
  public StepCollection Steps
  {
    get { return steps; }
  }

  /// <include file="documentation.xml" path="/UI/Common/DefaultSize/*"/>
  protected override System.Drawing.Size DefaultSize
  {
    get { return new Size(540, 400); }
  }

  /// <summary>Called when the Back button is clicked. If the event is not canceled, the wizard will navigate to the
  /// previous step.
  /// </summary>
  protected virtual void OnBackButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnBackButtonClicked(e);
    if(BackButtonClicked != null) BackButtonClicked(this, e);

    if(!e.Cancel && CurrentStepIndex != -1)
    {
      if(DesignMode || CurrentStep.PreviousStep == null) // in design mode, we always want to move through the steps in
      {                                                  // order, so it's predictable and the user can access them all
        if(CurrentStepIndex > 0) CurrentStepIndex--;
      }
      else
      {
        CurrentStepIndex = CurrentStep.PreviousStep.Index;
      }
    }
  }

  /// <summary>Called when the Cancel button is clicked. If the event is not canceled and the parent control is a form,
  /// the form will be closed with <see cref="DialogResult.Cancel"/>.
  /// </summary>
  protected virtual void OnCancelButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnCancelButtonClicked(e);
    if(CancelButtonClicked != null) CancelButtonClicked(this, e);

    if(!e.Cancel && !DesignMode) // don't close the form in design mode
    {
      Form form = Parent as Form;
      if(form != null)
      {
        form.DialogResult = DialogResult.Cancel;
        if(!form.Modal) form.Close();
      }
    }
  }

  /// <summary>Called when the Help button is clicked.</summary>
  protected virtual void OnHelpButtonClicked(EventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnHelpButtonClicked(e);
    if(HelpButtonClicked != null) HelpButtonClicked(this, e);
  }

  /// <summary>Called when the Finish button is clicked. If the event is not canceled and the parent control is a form,
  /// the form will be closed with <see cref="DialogResult.OK"/>.
  /// </summary>
  protected virtual void OnFinishButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnFinishButtonClicked(e);
    if(FinishButtonClicked != null) FinishButtonClicked(this, e);

    if(!e.Cancel && !DesignMode) // don't close the form in design mode
    {
      Form form = Parent as Form;
      if(form != null)
      {
        form.DialogResult = DialogResult.OK;
        if(!form.Modal) form.Close();
      }
    }
  }

  /// <summary>Called when the Back button is clicked. If the event is not canceled, the wizard will navigate to the
  /// next step.
  /// </summary>
  protected virtual void OnNextButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnNextButtonClicked(e);
    if(NextButtonClicked != null) NextButtonClicked(this, e);

    if(!e.Cancel && CurrentStepIndex != -1)
    {
      if(DesignMode || CurrentStep.NextStep == null) // in design mode, we always want to move through the steps in
      {                                              // order, so it's predictable and the user can access them all
        if(CurrentStepIndex < Steps.Count-1) CurrentStepIndex++;
      }
      else
      {
        CurrentStepIndex = CurrentStep.NextStep.Index;
      }
    }
  }

  /// <summary>Called when the current <see cref="WizardStep"/> changes.</summary>
  protected virtual void OnStepChanged(EventArgs e)
  {
    foreach(Control control in stepContainer.Controls)
    {
      if(control.Visible) control.Visible = false;
    }

    EnableNextButton = true;
    UpdateButtons();

    if(CurrentStepIndex != -1) CurrentStep.Visible = true;

    if(StepChanged != null) StepChanged(this, e);
    if(CurrentStepIndex != -1) CurrentStep.OnStepDisplayed(e);
  }

  /// <summary>Paints a border above the wizard buttons.</summary>
  protected void OnPaintButtonContainer(PaintEventArgs e)
  {
    base.OnPaint(e);

    if(CurrentStepIndex != -1)
    {
      ControlPaint.DrawBorder3D(e.Graphics, buttonContainer.ClientRectangle, Border3DStyle.Etched, Border3DSide.Top);
    }
  }

  /// <summary>Updates the text and enabled state of the wizard buttons, based on the current <see cref="WizardStep"/>.</summary>
  internal void UpdateButtons()
  {
    if(CurrentStepIndex == -1)
    {
      btnBack.Enabled = btnNext.Enabled = false;
      btnNext.Text    = "&Next >";
    }
    else
    {
      bool forceDisable = !DesignMode && !EnableNextButton; // always enable the buttons in design mode
      btnNext.Text    = IsFinishStep ? "&Finish" : "&Next >";
      btnBack.Enabled = CurrentStepIndex > 0 || !DesignMode && CurrentStep.PreviousStep != null;
      btnNext.Enabled = !forceDisable && (IsFinishStep || CurrentStepIndex < Steps.Count-1 ||
                                          !DesignMode && CurrentStep.NextStep != null);
    }
  }

  #region StepContainer
  /// <summary>A panel that contains and displays the <see cref="WizardStep"/> controls.</summary>
  sealed class StepContainer : Panel
  {
    public StepContainer(Wizard owner)
    {
      if(owner == null) throw new ArgumentNullException();
      this.owner = owner;
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
      base.OnControlAdded(e);
      WizardStep step = e.Control as WizardStep;
      if(step != null && step.Wizard == null) owner.Steps.Add(step);
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
      base.OnControlRemoved(e);
      WizardStep step = e.Control as WizardStep;
      if(step != null && step.Wizard == owner) owner.Steps.Remove(step);
    }

    readonly Wizard owner;
  }
  #endregion

  /// <summary>Gets whether the current step is a finish step.</summary>
  bool IsFinishStep
  {
    get
    {
      return Steps.Count != 0 && (CurrentStepIndex == Steps.Count-1 && CurrentStep.NextStep == null ||
                                  CurrentStep is FinishStep);
    }
  }

  void InitializeComponent()
  {
    SetStyle(ControlStyles.Selectable, false); // the wizard itself can't receive keyboard focus

    SuspendLayout();

    Size = DefaultSize;

    buttonContainer = new Panel(); // create a panel to hold the buttons
    buttonContainer.Size     = new Size(Width, 38);
    buttonContainer.Location = new Point(0, Height-buttonContainer.Height);
    buttonContainer.Anchor   = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    buttonContainer.Name     = "buttonContainer";
    buttonContainer.TabIndex = 1000; // the tabindex starts at 1000 so the buttons hopefully come last in the tab order
    buttonContainer.Paint += delegate(object sender, PaintEventArgs e) { OnPaintButtonContainer(e); };

    btnBack = new Button();
    btnBack.Size     = new Size(75, 23);
    btnBack.Location = new Point(219, (buttonContainer.Height - btnBack.Height + 1) / 2); // center it vertically
    btnBack.Anchor   = AnchorStyles.Right | AnchorStyles.Bottom;
    btnBack.Text     = "< &Back";
    btnBack.Name     = "btnBack";
    btnBack.TabIndex = 1001;
    btnBack.Click += delegate { OnBackButtonClicked(new CancelEventArgs()); };

    btnNext = new Button();
    btnNext.Location = new Point(btnBack.Right, btnBack.Top);
    btnNext.Size     = btnBack.Size;
    btnNext.Anchor   = btnBack.Anchor;
    btnNext.Text     = "&Next >";
    btnNext.Name     = "btnNext";
    btnNext.TabIndex = 1002;
    btnNext.Click += delegate
    {
      CancelEventArgs e = new CancelEventArgs();
      if(IsFinishStep) OnFinishButtonClicked(e);
      else OnNextButtonClicked(e);
    };

    btnCancel = new Button();
    btnCancel.Location = new Point(btnNext.Right + 7, btnBack.Top);
    btnCancel.Size     = btnBack.Size;
    btnCancel.Anchor   = btnBack.Anchor;
    btnCancel.Text     = "Cancel";
    btnCancel.Name     = "btnCancel";
    btnCancel.TabIndex = 1003;
    btnCancel.Click += delegate { OnCancelButtonClicked(new CancelEventArgs()); };

    btnHelp = new Button();
    btnHelp.Location   = new Point(btnCancel.Right + 7, btnBack.Top);
    btnHelp.Size       = btnBack.Size;
    btnHelp.Anchor     = btnBack.Anchor;
    btnHelp.Text       = "Help";
    btnHelp.Name       = "btnHelp";
    btnHelp.TabIndex   = 1004;
    btnHelp.Click += delegate { OnHelpButtonClicked(EventArgs.Empty); };

    stepContainer = new StepContainer(this);
    stepContainer.Size     = new Size(Width, Height-buttonContainer.Height);
    stepContainer.Location = new Point();
    stepContainer.Anchor   = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
    stepContainer.TabIndex = 0;

    buttonContainer.Controls.AddRange(new Control[] { btnBack, btnNext, btnCancel, btnHelp });
    Controls.AddRange(new Control[] { stepContainer, buttonContainer });

    ResumeLayout();
  }

  internal Panel stepContainer, buttonContainer;
  internal Button btnBack, btnNext, btnCancel, btnHelp;
  readonly StepCollection steps;
  int currentStep = -1;
  bool nextButtonEnabled = true;
}
#endregion

#region WizardStep
/// <summary>A base class for steps within a <see cref="Wizard"/>.</summary>
[Designer(typeof(WizardStepDesigner)), DefaultEvent("NextButtonClicked"), DefaultProperty("Title")]
public abstract class WizardStep : Control
{
  /// <summary>Initializes a new <see cref="WizardStep"/>.</summary>
  public WizardStep()
  {
    SetStyle(ControlStyles.Selectable, false);

    subtitleFont = defaultSubtitleFont = Font;
    titleFont    = defaultTitleFont    = CreateDefaultTitleFont();

    Dock = DockStyle.Fill;
    ResetHeaderColor();
    ResetTitleColor();
  }

  /// <summary>Raised when the Back button is clicked while this step is visible.</summary>
  [Category("Action"), Description("Raised when the Back button is clicked while this step is visible.")]
  public event CancelEventHandler BackButtonClicked;

  /// <summary>Raised when the Cancel button is clicked while this step is visible.</summary>
  [Category("Action"), Description("Raised when the Cancel button is clicked while this step is visible.")]
  public event CancelEventHandler CancelButtonClicked;

  /// <summary>Raised when the Finish button is clicked while this step is visible.</summary>
  [Category("Action"), Description("Raised when the Finish button is clicked while this step is visible.")]
  public event CancelEventHandler FinishButtonClicked;

  /// <summary>Raised when the Help button is clicked while this step is visible.</summary>
  [Category("Action"), Description("Raised when the Help button is clicked while this step is visible.")]
  public event EventHandler HelpButtonClicked;

  /// <summary>Raised when the Next button is clicked while this step is visible.</summary>
  [Category("Action"), Description("Raised when the Next button is clicked while this step is visible.")]
  public event CancelEventHandler NextButtonClicked;

  /// <summary>Raised when the user navigates to this step.</summary>
  [Category("Behavior"), Description("Raised when the user navigates to this step.")]
  public event EventHandler StepDisplayed;

  /// <summary>Gets the index of this <see cref="WizardStep"/> within its parent <see cref="Wizard"/>, or -1 if the
  /// step does not belong to a <see cref="Wizard"/>.
  /// </summary>
  [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int Index
  {
    get { return index; }
  }

  /// <summary>Gets or sets the brush used to paint the header area of the wizard step, if <see cref="HeaderImage"/> is
  /// null. If null, the <see cref="HeaderImage"/> or <see cref="HeaderColor"/> will be used.
  /// </summary>
  [Category("Appearance"), Description("The brush used to paint the header area of the wizard, if HeaderImage is null.")]
  [DefaultValue(null)]
  public Brush HeaderBrush
  {
    get { return headerBrush; }
    set
    {
      headerBrush = value;
      if(HeaderImage == null) Invalidate();
    }
  }

  /// <summary>Gets or sets the color used to paint the header area of the wizard step, if <see cref="HeaderImage"/>
  /// and <see cref="HeaderBrush"/> are null.
  /// </summary>
  [Category("Appearance"), Description("The color used to paint the header area of the wizard, if HeaderImage and HeaderBrush are null.")]
  public Color HeaderColor
  {
    get { return headerColor; }
    set
    {
      headerColor = value;
      if(HeaderBrush == null && HeaderImage == null) Invalidate();
    }
  }

  /// <summary>Gets or sets the image displayed in the header area of the wizard step. If null, the
  /// <see cref="HeaderBrush"/> or <see cref="HeaderColor"/> will be used.
  /// </summary>
  [Category("Appearance"), Description("The image displayed in the header area of the wizard.")]
  [DefaultValue(null)]
  public Image HeaderImage
  {
    get { return headerImage; }
    set
    {
      headerImage = value;
      Invalidate();
    }
  }

  /// <summary>Gets or sets whether the header area is displayed to the left of the wizard or at the top.</summary>
  [Category("Appearance"), Description("Gets whether the header area is displayed to the left or the top of the wizard step.")]
  [DefaultValue(Orientation.Horizontal)]
  public Orientation HeaderOrientation
  {
    get { return headerOrientation; }
    set
    {
      headerOrientation = value;
      Invalidate();
    }
  }

  /// <summary>Gets or sets the subtitle text, which describes the wizard step.</summary>
  [Category("Appearance"), Description("The description of the wizard step.")]
  public string Subtitle
  {
    get { return subtitle; }
    set
    {
      subtitle = value;
      if(HeaderOrientation == Orientation.Horizontal) RecalculateHeaderSize();
      Invalidate();
    }
  }

  /// <summary>Gets or sets the font used to render the subtitle text.</summary>
  [Category("Appearance"), Description("The font used to draw the subtitle text.")]
  public Font SubtitleFont
  {
    get { return subtitleFont; }
    set
    {
      if(value == null) throw new ArgumentNullException();
      subtitleFont = value;
      if(HeaderOrientation == Orientation.Horizontal) RecalculateHeaderSize();
      Invalidate();
    }
  }

  /// <summary>Gets or sets the title of the wizard step.</summary>
  [Category("Appearance"), Description("The title of the wizard step.")]
  public string Title
  {
    get { return title; }
    set
    {
      title = value;
      if(HeaderOrientation == Orientation.Horizontal) RecalculateHeaderSize();
      Invalidate();
    }
  }

  /// <summary>Gets or sets the color used to render the title and subtitle text.</summary>
  [Category("Appearance"), Description("The color used to draw the title and subtitle text.")]
  public Color TitleColor
  {
    get { return titleColor; }
    set
    {
      titleColor = value;
      Invalidate();
    }
  }

  /// <summary>Gets or sets the font used to render the title text.</summary>
  [Category("Appearance"), Description("The font used to draw the title text.")]
  public Font TitleFont
  {
    get { return titleFont; }
    set
    {
      if(value == null) throw new ArgumentNullException();
      titleFont = value;
      if(HeaderOrientation == Orientation.Horizontal) RecalculateHeaderSize();
      Invalidate();
    }
  }

  /// <summary>Gets or sets the <see cref="WizardStep"/> that comes after this one in the wizard. If null, the step
  /// at the next index will be used.
  /// </summary>
  [DefaultValue(null), Category("Behavior")]
  [Description("Specifies the next step in the wizard. If null, the step at the next index will be used.")]
  [TypeConverter(typeof(WizardStepTypeConverter))]
  public WizardStep NextStep
  {
    get { return nextStep; }
    set
    {
      if(value != null && Wizard != null && value.Wizard != null && value.Wizard != Wizard)
      {
        throw new ArgumentException("The step belongs to a different wizard.");
      }
      nextStep = value;
    }
  }

  /// <summary>Gets or sets the <see cref="WizardStep"/> that comes before this one in the wizard. If null, the step
  /// at the previous index will be used.
  /// </summary>
  [DefaultValue(null), Category("Behavior")]
  [Description("Specifies the previous step in the wizard. If null, the step at the previous index will be used.")]
  [TypeConverter(typeof(WizardStepTypeConverter))]
  public WizardStep PreviousStep
  {
    get { return prevStep; }
    set
    {
      if(value != null && Wizard != null && value.Wizard != null && value.Wizard != Wizard)
      {
        throw new ArgumentException("The step belongs to a different wizard.");
      }
      if(Wizard != null) Wizard.UpdateButtons();
      prevStep = value;
    }
  }

  /// <summary>Gets the <see cref="Wizard"/> that owns this <see cref="WizardStep"/>, or null if this
  /// <see cref="WizardStep"/> does not belong to a <see cref="Wizard"/>.
  /// </summary>
  protected internal Wizard Wizard
  {
    get { return owner; }
  }

  /// <summary>Gets the thickness of the header area.</summary>
  protected virtual int HeaderSize
  {
    get
    {
      if(HeaderOrientation == Orientation.Horizontal)
      {
        return HeaderImage != null ? Width * HeaderImage.Height / HeaderImage.Width : calculatedHeaderHeight;
      }
      else
      {
        return HeaderImage != null ? Height * HeaderImage.Width / HeaderImage.Height : 164;
      }
    }
  }

  /// <summary>Called when the Back button is clicked while this wizard step is visible.</summary>
  protected internal virtual void OnBackButtonClicked(CancelEventArgs e)
  {
    if(BackButtonClicked != null) BackButtonClicked(this, e);
  }

  /// <summary>Called when the Cancel button is clicked while this wizard step is visible.</summary>
  protected internal virtual void OnCancelButtonClicked(CancelEventArgs e)
  {
    if(CancelButtonClicked != null) CancelButtonClicked(this, e);
  }

  /// <summary>Called when the Help button is clicked while this wizard step is visible.</summary>
  protected internal virtual void OnHelpButtonClicked(EventArgs e)
  {
    if(HelpButtonClicked != null) HelpButtonClicked(this, e);
  }

  /// <summary>Called when the Finish button is clicked while this wizard step is visible.</summary>
  protected internal virtual void OnFinishButtonClicked(CancelEventArgs e)
  {
    if(FinishButtonClicked != null) FinishButtonClicked(this, e);
  }

  /// <summary>Called when the Next button is clicked while this wizard step is visible.</summary>
  protected internal virtual void OnNextButtonClicked(CancelEventArgs e)
  {
    if(NextButtonClicked != null) NextButtonClicked(this, e);
  }

  /// <summary>Called when this wizard step is made visible.</summary>
  protected internal virtual void OnStepDisplayed(EventArgs e)
  {
    if(StepDisplayed != null) StepDisplayed(this, e);
  }

  /// <include file="documentation.xml" path="/UI/Common/OnFontChanged/*"/>
  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);

    bool titleWasDefault = !ShouldSerializeTitleFont(), subtitleWasDefault = !ShouldSerializeSubtitleFont();

    defaultTitleFont    = CreateDefaultTitleFont();
    defaultSubtitleFont = Font;

    if(titleWasDefault) ResetTitleFont();
    if(subtitleWasDefault) ResetSubtitleFont();
  }

  /// <include file="documentation.xml" path="/UI/Common/OnPaint/*"/>
  protected override void OnPaint(PaintEventArgs e)
  {
    base.OnPaint(e);
    PaintHeaderBackground(e); // draw the header
    PaintTitleText(e);        // and the title and subtitle text
  }

  /// <include file="documentation.xml" path="/UI/Common/OnSizeChanged/*"/>
  protected override void OnSizeChanged(EventArgs e)
  {
    base.OnSizeChanged(e);
    // since the text is painted within the header for horizontal headers, recalculate header size if the size changes,
    // because it may cause the text to wrap differently and take up more or less vertical space
    if(HeaderOrientation == Orientation.Horizontal) RecalculateHeaderSize();
  }

  /// <summary>Draws the background of the header. The default implementation uses the <see cref="HeaderImage"/>,
  /// <see cref="HeaderBrush"/>, and <see cref="HeaderColor"/> properties to paint the background.
  /// </summary>
  protected virtual void PaintHeaderBackground(PaintEventArgs e)
  {
    Size headerSize = HeaderOrientation == Orientation.Horizontal ?
      new Size(Width, HeaderSize) : new Size(HeaderSize, Height);

    // draw the header image or brush, stretching it to fill the entire width or height of the wizard
    if(HeaderImage != null)
    {
      e.Graphics.DrawImage(HeaderImage, 0, 0, headerSize.Width, headerSize.Height);
    }
    else
    {
      Brush brush = HeaderBrush != null ? HeaderBrush : new SolidBrush(HeaderColor);
      e.Graphics.FillRectangle(brush, 0, 0, headerSize.Width, headerSize.Height);
      if(HeaderBrush == null) brush.Dispose();

      if(HeaderOrientation == Orientation.Horizontal)
      {
        ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(new Point(), headerSize),
                                  Border3DStyle.Etched, Border3DSide.Bottom);
      }
    }
  }

  /// <summary>Draws the title and subtitle text. The default implementation places the text within the header if the
  /// header is horizontal, or to the right of the header if the header is vertical.
  /// </summary>
  protected virtual void PaintTitleText(PaintEventArgs e)
  {
    using(Brush brush = new SolidBrush(TitleColor))
    {
      int xOffset = HeaderOrientation == Orientation.Horizontal ? 0 : HeaderSize, yOffset = 0;

      if(!string.IsNullOrEmpty(Title))
      {
        int height = (int)Math.Ceiling(TitleFont.GetHeight(e.Graphics)), shift = height/2;
        xOffset += shift;
        yOffset += shift;
        e.Graphics.DrawString(Title, TitleFont, brush, new Point(xOffset, yOffset));
        yOffset += height;
      }

      if(!string.IsNullOrEmpty(Subtitle))
      {
        int shift = (int)Math.Ceiling(SubtitleFont.GetHeight(e.Graphics)) / 2;
        xOffset += shift;
        yOffset += shift;
        e.Graphics.DrawString(Subtitle, SubtitleFont, brush,
                              new Rectangle(xOffset, yOffset, Width-xOffset-shift, Height-yOffset));
      }
    }
  }

  /// <summary>Called to create calculate the header height, when the header is horizontally oriented. The default
  /// implementation measures the height of the title and subtitle text to determine the height of the header.
  /// </summary>
  protected virtual int CalculateHeaderHeight()
  {
    using(Graphics g = Graphics.FromHwnd(Handle))
    {
      int xOffset = 0, height = 0;

      if(!string.IsNullOrEmpty(Title))
      {
        int fontHeight = (int)Math.Ceiling(TitleFont.GetHeight(g)), halfHeight = fontHeight/2;
        xOffset += halfHeight;
        height  += fontHeight + halfHeight;

        if(string.IsNullOrEmpty(Subtitle)) height += halfHeight;
      }

      if(!string.IsNullOrEmpty(Subtitle))
      {
        int fontHeight = (int)Math.Ceiling(SubtitleFont.GetHeight(g)), halfHeight = fontHeight/2;
        int textHeight = (int)Math.Ceiling(g.MeasureString(Subtitle, SubtitleFont,
                                           new Size(Width-xOffset-halfHeight*2, Height-height-halfHeight)).Height);
        height += textHeight + halfHeight*2;
      }

      return Math.Max(60, height);
    }
  }

  /// <summary>Called to create the default title font. The default implementation returns the same font face as
  /// <see cref="Control.Font"/>, but with a size of 10 points, and bold.
  /// </summary>
  protected virtual Font CreateDefaultTitleFont()
  {
    return new Font(Font.FontFamily, 10, FontStyle.Bold);
  }

  #region ShouldSerialize*/Reset* methods
  bool ShouldSerializeHeaderColor()
  {
    return HeaderColor != DefaultHeaderColor;
  }

  void ResetHeaderColor()
  {
    HeaderColor = DefaultHeaderColor;
  }

  bool ShouldSerializeSubtitleFont()
  {
    return SubtitleFont != defaultSubtitleFont;
  }

  void ResetSubtitleFont()
  {
    SubtitleFont = defaultSubtitleFont;
  }

  bool ShouldSerializeTitleColor()
  {
    return TitleColor != SystemColors.WindowText;
  }

  void ResetTitleColor()
  {
    TitleColor = SystemColors.WindowText;
  }

  bool ShouldSerializeTitleFont()
  {
    return TitleFont != defaultTitleFont;
  }

  void ResetTitleFont()
  {
    TitleFont = defaultTitleFont;
  }
  #endregion

  void RecalculateHeaderSize()
  {
    calculatedHeaderHeight = CalculateHeaderHeight();
  }

  internal Wizard owner;
  internal int index = -1;

  WizardStep nextStep, prevStep;
  string title = "Title Goes Here", subtitle = "And the subtitle goes here.";
  Font defaultTitleFont, defaultSubtitleFont, titleFont, subtitleFont;
  Image headerImage;
  Brush headerBrush;
  Color headerColor, titleColor;
  int calculatedHeaderHeight;
  Orientation headerOrientation;

  static readonly Color DefaultHeaderColor = Color.FromArgb(255, 192, 64); // an orangish color
}
#endregion

#region WizardStartStep
/// <summary>Implements a wizard start step, which has a white background, a vertical header area, and a large title
/// font.
/// </summary>
public class StartStep : WizardStep
{
  /// <summary>Initializes a new <see cref="StartStep"/>.</summary>
  public StartStep()
  {
    Title    = "Welcome to the Wizard.";
    Subtitle = "Enter a brief description of the wizard here.";

    BackColor         = SystemColors.Window;
    HeaderOrientation = Orientation.Vertical;
  }

  /// <summary>Called to create the default title font. This implementation returns the same font face as
  /// <see cref="Control.Font"/>, but with a size of 16 points.
  /// </summary>
  protected override Font CreateDefaultTitleFont()
  {
    return new Font(Font.FontFamily, 16);
  }
}
#endregion

#region WizardMiddleStep
/// <summary>Implements a basic wizard step.</summary>
public class MiddleStep : WizardStep
{
  /// <summary>Initializes a new <see cref="MiddleStep"/>.</summary>
  public MiddleStep()
  {
    Title = "Intermediate Step";
  }
}
#endregion

#region WizardFinishStep
/// <summary>Implements a finish step, which has a white background.</summary>
/// <remarks>Any <see cref="WizardStep"/> can be a finish step in the wizard, not only those deriving from this class.
/// But this class implements a common style for finish steps.
/// </remarks>
public class FinishStep : WizardStep
{
  /// <summary>Initializes a new <see cref="FinishStep"/>.</summary>
  public FinishStep()
  {
    BackColor = SystemColors.Window;
    Title     = "Finish Step";
  }
}
#endregion

} // namespace AdamMil.UI.Wizard