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
[Designer(typeof(WizardDesigner)), DefaultEvent("StepChanged"), DefaultProperty("Steps")]
public class Wizard : Control
{
  public Wizard()
  {
    steps = new StepCollection(this);
    InitializeComponent();
  }

  #region StepCollection
  [Serializable]
  public sealed class StepCollection : Collection<WizardStep>
  {
    internal StepCollection(Wizard owner)
    {
      if(owner == null) throw new ArgumentNullException();
      this.owner = owner;
    }

    protected override void ClearItems()
    {
      foreach(WizardStep step in this) OnRemove(step);

      base.ClearItems();

      owner.CurrentStepIndex = -1;
    }

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

    void FixIndicesFrom(int index)
    {
      for(; index < Count; index++) this[index].index = index;
    }

    void OnAdded(WizardStep item, int index)
    {
      item.index   = index;
      item.owner   = owner;
      item.Visible = false;
      if(!owner.stepContainer.Controls.Contains(item)) owner.stepContainer.Controls.Add(item);
    }

    void OnRemove(WizardStep item)
    {
      owner.stepContainer.Controls.Remove(item);
      item.index   = -1;
      item.owner   = null;
      item.Visible = true;
    }

    readonly Wizard owner;

    static void ValidateItem(WizardStep step)
    {
      if(step == null) throw new ArgumentNullException();
      if(step.owner != null) throw new ArgumentException("This step already belongs to a wizard.");
    }
  }
  #endregion

  [Category("Action"), Description("Raised when the Back button is clicked.")]
  public event CancelEventHandler BackButtonClicked;

  [Category("Action"), Description("Raised when the Cancel button is clicked.")]
  public event CancelEventHandler CancelButtonClicked;

  [Category("Action"), Description("Raised when the Finish button is clicked.")]
  public event CancelEventHandler FinishButtonClicked;

  [Category("Action"), Description("Raised when the Help button is clicked.")]
  public event EventHandler HelpButtonClicked;

  [Category("Action"), Description("Raised when the Next button is clicked.")]
  public event CancelEventHandler NextButtonClicked;

  [Category("Property Changed"), Description("Raised when the current step changes.")]
  public event EventHandler StepChanged;

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

  [Category("Appearance"), Description("Determines whether the Help button will be displayed."), DefaultValue(true)]
  public bool ShowHelpButton
  {
    get { return btnHelp.Visible; }
    set { btnHelp.Visible = value; }
  }

  [Category("Behavior"), Description("A collection of the steps displayed in the wizard."), MergableProperty(false)]
  [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
  [Editor(typeof(StepCollectionEditor), typeof(UITypeEditor))]
  public StepCollection Steps
  {
    get { return steps; }
  }

  protected override System.Drawing.Size DefaultSize
  {
    get { return new Size(540, 400); }
  }

  protected virtual void OnBackButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnBackButtonClicked(e);
    if(BackButtonClicked != null) BackButtonClicked(this, e);

    if(!e.Cancel && CurrentStepIndex != -1)
    {
      if(DesignMode || CurrentStep.PreviousStep == null)
      {
        if(CurrentStepIndex > 0) CurrentStepIndex--;
      }
      else
      {
        CurrentStepIndex = CurrentStep.PreviousStep.Index;
      }
    }
  }

  protected virtual void OnCancelButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnCancelButtonClicked(e);
    if(CancelButtonClicked != null) CancelButtonClicked(this, e);

    if(!e.Cancel)
    {
      Form form = Parent as Form;
      if(form != null)
      {
        form.DialogResult = DialogResult.Cancel;
        if(!form.Modal) form.Close();
      }
    }
  }

  protected virtual void OnHelpButtonClicked(EventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnHelpButtonClicked(e);
    if(HelpButtonClicked != null) HelpButtonClicked(this, e);
  }

  protected virtual void OnFinishButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnFinishButtonClicked(e);
    if(FinishButtonClicked != null) FinishButtonClicked(this, e);

    if(!e.Cancel)
    {
      Form form = Parent as Form;
      if(form != null)
      {
        form.DialogResult = DialogResult.OK;
        if(!form.Modal) form.Close();
      }
    }
  }

  protected virtual void OnNextButtonClicked(CancelEventArgs e)
  {
    if(CurrentStepIndex != -1) CurrentStep.OnNextButtonClicked(e);
    if(NextButtonClicked != null) NextButtonClicked(this, e);

    if(!e.Cancel && CurrentStepIndex != -1)
    {
      if(DesignMode || CurrentStep.NextStep == null)
      {
        if(CurrentStepIndex < Steps.Count-1) CurrentStepIndex++;
      }
      else
      {
        CurrentStepIndex = CurrentStep.NextStep.Index;
      }
    }
  }

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

  protected void OnPaintButtonContainer(PaintEventArgs e)
  {
    base.OnPaint(e);

    if(CurrentStepIndex != -1)
    {
      ControlPaint.DrawBorder3D(e.Graphics, buttonContainer.ClientRectangle, Border3DStyle.Etched, Border3DSide.Top);
    }
  }

  internal void UpdateButtons()
  {
    if(CurrentStepIndex == -1)
    {
      btnBack.Enabled = btnNext.Enabled = false;
      btnNext.Text    = "&Next >";
    }
    else
    {
      bool forceDisable = !DesignMode && !EnableNextButton;
      btnNext.Text    = IsFinishStep ? "&Finish" : "&Next >";
      btnBack.Enabled = CurrentStepIndex > 0 || !DesignMode && CurrentStep.PreviousStep != null;
      btnNext.Enabled = !forceDisable && (IsFinishStep || CurrentStepIndex < Steps.Count-1 ||
                                          !DesignMode && CurrentStep.NextStep != null);
    }
  }

  #region StepContainer
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
    SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
             ControlStyles.UserPaint, true);
    SetStyle(ControlStyles.Selectable, false);

    SuspendLayout();

    Size = DefaultSize;

    buttonContainer = new Panel();
    buttonContainer.Size     = new Size(Width, 38);
    buttonContainer.Location = new Point(0, Height-buttonContainer.Height);
    buttonContainer.Anchor   = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    buttonContainer.Name     = "buttonContainer";
    buttonContainer.TabIndex = 1000;
    buttonContainer.Paint += delegate(object sender, PaintEventArgs e) { OnPaintButtonContainer(e); };

    btnBack = new Button();
    btnBack.Size     = new Size(75, 23);
    btnBack.Location = new Point(219, (buttonContainer.Height - btnBack.Height + 1) / 2);
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
[Designer(typeof(WizardStepDesigner)), DefaultEvent("NextButtonClicked"), DefaultProperty("Title")]
public abstract class WizardStep : Control
{
  public WizardStep()
  {
    SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
             ControlStyles.UserPaint, true);
    SetStyle(ControlStyles.Selectable, false);

    subtitleFont = defaultSubtitleFont = Font;
    titleFont    = defaultTitleFont    = CreateDefaultTitleFont();

    Dock = DockStyle.Fill;
    ResetHeaderColor();
    ResetTitleColor();
  }

  [Category("Action"), Description("Raised when the Back button is clicked while this step is visible.")]
  public event CancelEventHandler BackButtonClicked;

  [Category("Action"), Description("Raised when the Cancel button is clicked while this step is visible.")]
  public event CancelEventHandler CancelButtonClicked;

  [Category("Action"), Description("Raised when the Finish button is clicked while this step is visible.")]
  public event CancelEventHandler FinishButtonClicked;

  [Category("Action"), Description("Raised when the Help button is clicked while this step is visible.")]
  public event EventHandler HelpButtonClicked;

  [Category("Action"), Description("Raised when the Next button is clicked while this step is visible.")]
  public event CancelEventHandler NextButtonClicked;

  [Category("Behavior"), Description("Raised when the user navigates to this step.")]
  public event EventHandler StepDisplayed;

  [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int Index
  {
    get { return index; }
  }

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

  [Category("Appearance"), Description("Gets whether the header area is displayed to the left or the top of the wizard page.")]
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

  [Category("Appearance"), Description("The description of the wizard page.")]
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

  [Category("Appearance"), Description("The font used to paint the subtitle.")]
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

  [Category("Appearance"), Description("The title of the wizard page.")]
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

  [Category("Appearance"), Description("The color used to paint the title and subtitle.")]
  public Color TitleColor
  {
    get { return titleColor; }
    set
    {
      titleColor = value;
      Invalidate();
    }
  }

  [Category("Appearance"), Description("The font used to paint the title.")]
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

  [DefaultValue(null), Category("Behavior")]
  [Description("Specifies the next step in the wizard. If null, the step at the next index will be used.")]
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

  [DefaultValue(null), Category("Behavior")]
  [Description("Specifies the previous step in the wizard. If null, the step at the previous index will be used.")]
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

  protected internal Wizard Wizard
  {
    get { return owner; }
  }

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

  protected internal virtual void OnBackButtonClicked(CancelEventArgs e)
  {
    if(BackButtonClicked != null) BackButtonClicked(this, e);
  }

  protected internal virtual void OnCancelButtonClicked(CancelEventArgs e)
  {
    if(CancelButtonClicked != null) CancelButtonClicked(this, e);
  }

  protected internal virtual void OnHelpButtonClicked(EventArgs e)
  {
    if(HelpButtonClicked != null) HelpButtonClicked(this, e);
  }

  protected internal virtual void OnFinishButtonClicked(CancelEventArgs e)
  {
    if(FinishButtonClicked != null) FinishButtonClicked(this, e);
  }

  protected internal virtual void OnNextButtonClicked(CancelEventArgs e)
  {
    if(NextButtonClicked != null) NextButtonClicked(this, e);
  }

  protected internal virtual void OnStepDisplayed(EventArgs e)
  {
    if(StepDisplayed != null) StepDisplayed(this, e);
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    base.OnPaint(e);
    PaintHeaderBackground(e);
    PaintTitleText(e);
  }

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

  protected override void OnFontChanged(EventArgs e)
  {
    base.OnFontChanged(e);

    bool titleWasDefault = !ShouldSerializeTitleFont(), subtitleWasDefault = !ShouldSerializeSubtitleFont();
    
    defaultTitleFont    = CreateDefaultTitleFont();
    defaultSubtitleFont = Font;

    if(titleWasDefault) ResetTitleFont();
    if(subtitleWasDefault) ResetSubtitleFont();
  }

  protected override void OnSizeChanged(EventArgs e)
  {
    base.OnSizeChanged(e);
    if(HeaderOrientation == Orientation.Horizontal) RecalculateHeaderSize();
  }

  protected virtual Font CreateDefaultTitleFont()
  {
    return new Font(Font.FontFamily, 10, FontStyle.Bold);
  }

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

  static readonly Color DefaultHeaderColor = Color.Orange;
}
#endregion

#region WizardStartStep
public class StartStep : WizardStep
{
  public StartStep()
  {
    Title    = "Welcome to the Wizard.";
    Subtitle = "Enter a brief description of the wizard here.";

    BackColor         = SystemColors.Window;
    HeaderOrientation = Orientation.Vertical;
  }

  protected override Font CreateDefaultTitleFont()
  {
    return new Font(Font.FontFamily, 16);
  }
}
#endregion

#region WizardIntermediateStep
public class IntermediateStep : WizardStep
{
  public IntermediateStep()
  {
    Title = "Intermediate Step";
  }
}
#endregion

#region WizardFinishStep
public class FinishStep : WizardStep
{
  public FinishStep()
  {
    BackColor = SystemColors.Window;
    Title     = "Finish Step";
  }
}
#endregion

} // namespace AdamMil.UI.Wizard