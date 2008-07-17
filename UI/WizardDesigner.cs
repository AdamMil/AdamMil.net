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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace AdamMil.UI.Wizard.Design
{

#region StepCollectionEditor
sealed class StepCollectionEditor : CollectionEditor
{
  public StepCollectionEditor(Type typeToEdit) : base(typeToEdit) { }

  protected override Type[] CreateNewItemTypes()
  {
    return new Type[] { typeof(StartStep), typeof(IntermediateStep), typeof(FinishStep) };
  }
}
#endregion

#region WizardActionList
sealed class WizardActionList : DesignerActionList
{
  public WizardActionList(IComponent component) : base(component) { }

  public override DesignerActionItemCollection GetSortedActionItems()
  {
    Wizard wizard = GetWizard();
    
    DesignerActionItemCollection actions = new DesignerActionItemCollection();
    actions.Add(new DesignerActionHeaderItem("Wizard Steps"));
    actions.Add(new DesignerActionPropertyItem("Steps", "Edit Steps", "Wizard Steps"));
    actions.Add(new DesignerActionMethodItem(this, "AddStep", "Add Step", "Wizard Steps", true));

    if(wizard.CurrentStepIndex != -1)
    {
      actions.Add(new DesignerActionMethodItem(this, "RemoveStep", "Remove Step", "Wizard Steps", true));
    }

    return actions;
  }

  [Editor(typeof(StepCollectionEditor), typeof(UITypeEditor))]
  public Wizard.StepCollection Steps
  {
    get { return GetWizard().Steps; }
  }

  void AddStep()
  {
    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
    if(host != null)
    {
      Wizard wizard = GetWizard();
      WizardStep step = (WizardStep)host.CreateComponent(typeof(IntermediateStep));
      wizard.Steps.Insert(wizard.CurrentStepIndex + (wizard.CurrentStep is FinishStep ? 0 : 1), step);
      wizard.CurrentStep = step;
    }
  }

  Wizard GetWizard()
  {
    return (Wizard)Component;
  }

  void RemoveStep()
  {
    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
    if(host != null)
    {
      Wizard wizard = GetWizard();
      if(wizard.CurrentStepIndex != -1)
      {
        WizardStep step = wizard.CurrentStep;
        wizard.Steps.RemoveAt(wizard.CurrentStepIndex);
        host.DestroyComponent(step);
        SelectWizard();
      }
    }
  }

  void SelectWizard()
  {
    ISelectionService service = (ISelectionService)GetService(typeof(ISelectionService));
    if(service != null) service.SetSelectedComponents(new IComponent[] { Component }, SelectionTypes.Replace);
  }
}
#endregion

#region WizardDesigner
sealed class WizardDesigner : ParentControlDesigner
{
  public override DesignerActionListCollection ActionLists
  {
    get { return actions; }
  }

  public override System.Collections.ICollection AssociatedComponents
  {
    get { return GetWizard().Steps; }
  }

  public override bool CanParent(Control control)
  {
    return control is WizardStep;
  }

  public override void Initialize(IComponent component)
  {
    base.Initialize(component);

    ISelectionService service = (ISelectionService)GetService(typeof(ISelectionService));
    if(service != null)
    {
      service.SelectionChanged -= service_SelectionChanged; // make sure only one handler is added
      service.SelectionChanged += service_SelectionChanged;
    }
    
    actions = new DesignerActionListCollection(new DesignerActionList[] { new WizardActionList(Control) });
  }

  public override void InitializeNewComponent(System.Collections.IDictionary defaultValues)
  {
    base.InitializeNewComponent(defaultValues);

    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
    if(host != null)
    {
      Wizard wizard = GetWizard();
      wizard.Steps.Add((WizardStep)host.CreateComponent(typeof(StartStep)));
      wizard.Steps.Add((WizardStep)host.CreateComponent(typeof(IntermediateStep)));
      wizard.Steps.Add((WizardStep)host.CreateComponent(typeof(FinishStep)));
    }
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    ISelectionService service = (ISelectionService)GetService(typeof(ISelectionService));
    if(service != null) service.SelectionChanged -= service_SelectionChanged;

    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
    if(host != null)
    {
      foreach(WizardStep step in GetWizard().Steps) host.DestroyComponent(step);
    }
  }

  protected override bool GetHitTest(Point point)
  {
    if(isSelected)
    {
      Wizard wizard = GetWizard();
      Point buttonPoint = wizard.buttonContainer.PointToClient(point);
      if(wizard.btnBack.Bounds.Contains(buttonPoint) || wizard.btnNext.Bounds.Contains(buttonPoint) ||
         wizard.btnCancel.Bounds.Contains(buttonPoint) || wizard.btnHelp.Bounds.Contains(buttonPoint))
      {
        return true;
      }
    }

    return base.GetHitTest(point);
  }

  protected override void OnDragComplete(DragEventArgs e)
  {
    if(forwardingDrag)
    {
      GetStepDesigner().InternalOnDragComplete(e);
      forwardingDrag = false;
    }
    else
    {
      base.OnDragComplete(e);
    }
  }

  protected override void OnDragDrop(DragEventArgs e)
  {
    if(forwardingDrag)
    {
      GetStepDesigner().InternalOnDragDrop(e);
      forwardingDrag = false;
    }
    else
    {
      base.OnDragDrop(e);
    }
  }

  protected override void OnDragEnter(DragEventArgs e)
  {
    WizardStepDesigner stepDesigner = GetStepDesigner();
    WizardStep currentStep = GetWizard().CurrentStep;

    if(stepDesigner != null && currentStep.ClientRectangle.Contains(currentStep.PointToClient(new Point(e.X, e.Y))))
    {
      forwardingDrag = true;
      stepDesigner.InternalOnDragEnter(e);
    }
    else
    {
      base.OnDragEnter(e);
    }
  }

  protected override void OnDragLeave(EventArgs e)
  {
    if(forwardingDrag)
    {
      GetStepDesigner().InternalOnDragLeave(e);
      forwardingDrag = false;
    }
    else
    {
      base.OnDragLeave(e);
    }
  }

  protected override void OnDragOver(DragEventArgs e)
  {
    if(forwardingDrag) GetStepDesigner().InternalOnDragOver(e);
    else base.OnDragOver(e);
  }

  protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
  {
    if(forwardingDrag) GetStepDesigner().InternalOnGiveFeedback(e);
    else base.OnGiveFeedback(e);
  }

  protected override void OnPaintAdornments(PaintEventArgs pe)
  {
    base.OnPaintAdornments(pe);

    Wizard wizard = GetWizard();
    if(wizard.Steps.Count == 0)
    {
      ControlPaint.DrawBorder(pe.Graphics, wizard.ClientRectangle,
                              SystemColors.ControlDark, ButtonBorderStyle.Dashed);
    }
  }

  WizardStepDesigner GetStepDesigner()
  {
    Wizard wizard = GetWizard();
    if(wizard.CurrentStepIndex != -1)
    {
      IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
      if(host != null) return (WizardStepDesigner)host.GetDesigner(wizard.CurrentStep);
    }
    return null;
  }

  Wizard GetWizard()
  {
    return (Wizard)Control;
  }

  void service_SelectionChanged(object sender, EventArgs e)
  {
    ISelectionService service = (ISelectionService)GetService(typeof(ISelectionService));
    isSelected = service != null && service.GetComponentSelected(Control);

    if(service != null)
    {
      // when the wizard is reloaded, it goes back to the first step. but the user may have been working on another
      // step. so if any controls are selected that belong to a different step, switch to that step.s
      Wizard wizard = GetWizard();
      if(wizard != null && wizard.CurrentStepIndex == 0)
      {
        foreach(IComponent selected in service.GetSelectedComponents())
        {
          Control control = selected as Control;
          if(control != null)
          {
            WizardStep step = null;
            do
            {
              step    = control as WizardStep;
              control = control.Parent;
            } while(step == null && control != null);

            if(step != null && step.Wizard == wizard)
            {
              wizard.CurrentStep = step;
              break;
            }
          }
        }
      }
    }
  }

  DesignerActionListCollection actions;
  bool isSelected, forwardingDrag;
}
#endregion

#region WizardStepDesigner
sealed class WizardStepDesigner : ParentControlDesigner
{
  public override DesignerActionListCollection ActionLists
  {
    get
    {
      if(actions == null)
      {
        Wizard wizard = GetStep().Wizard;
        if(wizard != null)
        {
          actions = new DesignerActionListCollection(new DesignerActionList[] { new WizardActionList(wizard) });
        }
      }
     
      return actions;
    }
  }

  public override SelectionRules SelectionRules
  {
    get { return base.SelectionRules & ~(SelectionRules.Moveable | SelectionRules.AllSizeable); }
  }

  public override bool CanBeParentedTo(IDesigner parentDesigner)
  {
    return parentDesigner is WizardDesigner;
  }

  internal void InternalOnDragComplete(DragEventArgs e)
  {
    OnDragComplete(e);
  }

  internal void InternalOnDragDrop(DragEventArgs e)
  {
    OnDragDrop(e);
  }

  internal void InternalOnDragEnter(DragEventArgs e)
  {
    OnDragEnter(e);
  }

  internal void InternalOnDragLeave(EventArgs e)
  {
    OnDragLeave(e);
  }

  internal void InternalOnDragOver(DragEventArgs e)
  {
    OnDragOver(e);
  }

  internal void InternalOnGiveFeedback(GiveFeedbackEventArgs e)
  {
    OnGiveFeedback(e);
  }

  WizardStep GetStep()
  {
    return (WizardStep)Control;
  }

  DesignerActionListCollection actions;
}
#endregion

} // namespace AdamMil.UI.Wizard.Design