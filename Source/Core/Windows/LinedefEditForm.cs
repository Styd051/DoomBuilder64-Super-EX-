
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;


#endregion

namespace CodeImp.DoomBuilder.Windows
{
    internal partial class LinedefEditForm : DelayedForm
    {
        // Variables
        private ICollection<Linedef> lines;

        // Constructor
        public LinedefEditForm()
        {
            // Initialize
            InitializeComponent();

            // Fill flags list
            foreach (KeyValuePair<string, string> lf in General.Map.Config.LinedefFlags)
                flags.Add(lf.Value, lf.Key);

            // Fill actions list
            action.GeneralizedCategories = General.Map.Config.GenActionCategories;
            action.AddInfo(General.Map.Config.SortedLinedefActions.ToArray());

            // Initialize image selectors
            fronthigh.Initialize();
            frontmid.Initialize();
            frontlow.Initialize();
            backhigh.Initialize();
            backmid.Initialize();
            backlow.Initialize();

            // Tag?
            if (General.Map.FormatInterface.HasLinedefTag)
            {
                // Match position after the action group
                idgroup.Top = actiongroup.Bottom + actiongroup.Margin.Bottom + idgroup.Margin.Top;
            }
            else
            {
                idgroup.Visible = false;
            }

            // villsa
            if (General.Map.FormatInterface.InDoom64Mode)
            {
                this.activationtype.Show();
                activationtype.Top = idgroup.Bottom + idgroup.Margin.Bottom + activationtype.Margin.Top;
                this.Height = heightpanel3.Height;
            }
        }

        // villsa 9/12/11
        private void SwitchTextureMask(Linedef l)
        {
            int switchflags = 0;
            int mask = 0;

            if ((l.SwitchMask & 0x2000) == 0x2000)
                switchflags |= 0x2000;

            if ((l.SwitchMask & 0x4000) == 0x4000)
                switchflags |= 0x4000;

            if ((l.SwitchMask & 0x8000) == 0x8000)
                switchflags |= 0x8000;

            if (l.IsFlagSet("65536"))
                switchflags |= 65536;

            mask = (switchflags & 0x6000);

            if (mask == 0)
                return;

            if (mask == 0x2000)
            {
                chkSwitchTextureUpper.Checked = true;

                if ((switchflags & 0x8000) == 0x8000)
                    chkSwitchDisplayMiddle.Checked = true;
                else
                    chkSwitchDisplayLower.Checked = true;
            }
            else if (mask == 0x4000)
            {
                chkSwitchTextureLower.Checked = true;

                if ((switchflags & 0x10000) == 0x10000)
                    chkSwitchDisplayMiddle.Checked = true;
                else if ((switchflags & 0x8000) == 0x8000)
                    chkSwitchDisplayUpper.Checked = true;
            }
            else if (mask == 0x6000)
            {
                chkSwitchTextureMiddle.Checked = true;

                if ((switchflags & 0x8000) == 0x8000)
                    chkSwitchDisplayUpper.Checked = true;
                else
                    chkSwitchDisplayLower.Checked = true;
            }
        }

        // villsa 9/12/11
        private void SetSwitchMask(Linedef l)
        {
            int switchflags = 0;

            // just check for one of the checkboxes.. no need to
            // check for all of them..
            if (chkSwitchTextureLower.Enabled == false)
                return;

            if (chkSwitchTextureLower.Checked == true)
            {
                if (chkSwitchDisplayMiddle.Checked == true)
                {
                    switchflags = 0x4000;
                }
                else if (chkSwitchDisplayUpper.Checked == true)
                {
                    switchflags = (0x4000 | 0x8000);
                }
            }
            else if (chkSwitchTextureMiddle.Checked == true)
            {
                if (chkSwitchDisplayLower.Checked == true)
                {
                    switchflags = (0x2000 | 0x4000);
                }
                else if (chkSwitchDisplayUpper.Checked == true)
                {
                    switchflags = (0x2000 | 0x4000 | 0x8000);
                }
            }
            else if (chkSwitchTextureUpper.Checked == true)
            {
                if (chkSwitchDisplayMiddle.Checked == true)
                {
                    switchflags = (0x2000 | 0x8000);
                }
                else if (chkSwitchDisplayLower.Checked == true)
                {
                    switchflags = 0x2000;
                }
            }

            l.SwitchMask = switchflags;
        }

        private void PreSetActivationFlag(CheckBox c, int flag, int mask)
        {
            if ((flag & mask) == mask)
                c.Checked = true;
        }

        private void CheckActivationState(CheckBox c, int flag, int mask)
        {
            if (((flag & mask) != mask) && c.Checked == true)
            {
                c.CheckState = CheckState.Indeterminate;
                c.ThreeState = true;
            }
        }

        private void SetActivationFlag(Linedef l, CheckBox c, int mask)
        {
            if (c.CheckState == CheckState.Checked)
                l.Activate |= mask;
            else if (c.CheckState == CheckState.Unchecked)
                l.Activate &= ~mask;
        }

        // This sets up the form to edit the given lines
        public void Setup(ICollection<Linedef> lines)
        {
            Linedef fl;

            // Keep this list
            this.lines = lines;
            if (lines.Count > 1) this.Text = "Edit Linedefs (" + lines.Count + ")";

            ////////////////////////////////////////////////////////////////////////
            // Set all options to the first linedef properties
            ////////////////////////////////////////////////////////////////////////

            // Get first line
            fl = General.GetByIndex(lines, 0);

            // Flags
            foreach (CheckBox c in flags.Checkboxes)
                if (fl.Flags.ContainsKey(c.Tag.ToString())) c.Checked = fl.Flags[c.Tag.ToString()];

            // Action/tags
            action.Value = fl.Action;
            tag.Text = fl.Tag.ToString();

            // Front side and back side checkboxes
            frontside.Checked = (fl.Front != null);
            backside.Checked = (fl.Back != null);

            // Front settings
            if (fl.Front != null)
            {
                fronthigh.TextureName = fl.Front.HighTexture;
                frontmid.TextureName = fl.Front.MiddleTexture;
                frontlow.TextureName = fl.Front.LowTexture;
                fronthigh.Required = fl.Front.HighRequired();
                frontmid.Required = fl.Front.MiddleRequired();
                frontlow.Required = fl.Front.LowRequired();
                frontsector.Text = fl.Front.Sector.Index.ToString();
                frontoffsetx.Text = fl.Front.OffsetX.ToString();
                frontoffsety.Text = fl.Front.OffsetY.ToString();
            }

            // Back settings
            if (fl.Back != null)
            {
                backhigh.TextureName = fl.Back.HighTexture;
                backmid.TextureName = fl.Back.MiddleTexture;
                backlow.TextureName = fl.Back.LowTexture;
                backhigh.Required = fl.Back.HighRequired();
                backmid.Required = fl.Back.MiddleRequired();
                backlow.Required = fl.Back.LowRequired();
                backsector.Text = fl.Back.Sector.Index.ToString();
                backoffsetx.Text = fl.Back.OffsetX.ToString();
                backoffsety.Text = fl.Back.OffsetY.ToString();
            }

            ////////////////////////////////////////////////////////////////////////
            // Now go for all lines and change the options when a setting is different
            ////////////////////////////////////////////////////////////////////////

            // Go for all lines
            foreach (Linedef l in lines)
            {
                // Flags
                foreach (CheckBox c in flags.Checkboxes)
                {
                    if (l.Flags.ContainsKey(c.Tag.ToString()))
                    {
                        if (l.Flags[c.Tag.ToString()] != c.Checked)
                        {
                            c.ThreeState = true;
                            c.CheckState = CheckState.Indeterminate;
                        }
                    }
                }

                // Activations

                // villsa
                if (General.Map.FormatInterface.InDoom64Mode)
                {
                    if (l.Activate > 0)
                    {
                        // 20120219 villsa
                        l.Activate -= (l.Activate & 511);
                        PreSetActivationFlag(activationtypered, l.Activate, 512);
                        PreSetActivationFlag(activationtypeblue, l.Activate, 1024);
                        PreSetActivationFlag(activationtypeyellow, l.Activate, 2048);
                        PreSetActivationFlag(activationtypecross, l.Activate, 4096);
                        PreSetActivationFlag(activationtypeshoot, l.Activate, 8192);
                        PreSetActivationFlag(activationtypeuse, l.Activate, 16384);
                        PreSetActivationFlag(activationtyperepeat, l.Activate, 32768);
                    }

                    SwitchTextureMask(l);
                }

                // Action/tags
                if (l.Action != action.Value) action.Empty = true;
                if (l.Tag.ToString() != tag.Text) tag.Text = "";

                // Front side checkbox
                if ((l.Front != null) != frontside.Checked)
                {
                    frontside.ThreeState = true;
                    frontside.CheckState = CheckState.Indeterminate;
                    frontside.AutoCheck = false;
                }

                // Back side checkbox
                if ((l.Back != null) != backside.Checked)
                {
                    backside.ThreeState = true;
                    backside.CheckState = CheckState.Indeterminate;
                    backside.AutoCheck = false;
                }

                // Front settings
                if (l.Front != null)
                {
                    if (fronthigh.TextureName != l.Front.HighTexture) fronthigh.TextureName = "";
                    if (frontmid.TextureName != l.Front.MiddleTexture) frontmid.TextureName = "";
                    if (frontlow.TextureName != l.Front.LowTexture) frontlow.TextureName = "";
                    if (fronthigh.Required != l.Front.HighRequired()) fronthigh.Required = false;
                    if (frontmid.Required != l.Front.MiddleRequired()) frontmid.Required = false;
                    if (frontlow.Required != l.Front.LowRequired()) frontlow.Required = false;
                    if (frontsector.Text != l.Front.Sector.Index.ToString()) frontsector.Text = "";
                    if (frontoffsetx.Text != l.Front.OffsetX.ToString()) frontoffsetx.Text = "";
                    if (frontoffsety.Text != l.Front.OffsetY.ToString()) frontoffsety.Text = "";
                }

                // Back settings
                if (l.Back != null)
                {
                    if (backhigh.TextureName != l.Back.HighTexture) backhigh.TextureName = "";
                    if (backmid.TextureName != l.Back.MiddleTexture) backmid.TextureName = "";
                    if (backlow.TextureName != l.Back.LowTexture) backlow.TextureName = "";
                    if (backhigh.Required != l.Back.HighRequired()) backhigh.Required = false;
                    if (backmid.Required != l.Back.MiddleRequired()) backmid.Required = false;
                    if (backlow.Required != l.Back.LowRequired()) backlow.Required = false;
                    if (backsector.Text != l.Back.Sector.Index.ToString()) backsector.Text = "";
                    if (backoffsetx.Text != l.Back.OffsetX.ToString()) backoffsetx.Text = "";
                    if (backoffsety.Text != l.Back.OffsetY.ToString()) backoffsety.Text = "";
                }
            }

            foreach (Linedef l in lines)
            {
                // 20120219 villsa
                l.Activate -= (l.Activate & 511);
                CheckActivationState(activationtypered, l.Activate, 512);
                CheckActivationState(activationtypeblue, l.Activate, 1024);
                CheckActivationState(activationtypeyellow, l.Activate, 2048);
                CheckActivationState(activationtypecross, l.Activate, 4096);
                CheckActivationState(activationtypeshoot, l.Activate, 8192);
                CheckActivationState(activationtypeuse, l.Activate, 16384);
                CheckActivationState(activationtyperepeat, l.Activate, 32768);
            }

            // Refresh controls so that they show their image
            backhigh.Refresh();
            backmid.Refresh();
            backlow.Refresh();
            fronthigh.Refresh();
            frontmid.Refresh();
            frontlow.Refresh();
        }

        // Front side (un)checked
        private void frontside_CheckStateChanged(object sender, EventArgs e)
        {
            // Enable/disable panel
            // NOTE: Also enabled when checkbox is grayed!
            frontgroup.Enabled = (frontside.CheckState != CheckState.Unchecked);
        }

        // Back side (un)checked
        private void backside_CheckStateChanged(object sender, EventArgs e)
        {
            // Enable/disable panel
            // NOTE: Also enabled when checkbox is grayed!
            backgroup.Enabled = (backside.CheckState != CheckState.Unchecked);
        }

        // This selects all text in a textbox
        private void SelectAllText(object sender, EventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        // Apply clicked
        private void apply_Click(object sender, EventArgs e)
        {
            string undodesc = "linedef";
            Sector s;
            int index;

            // Verify the tag
            if (General.Map.FormatInterface.HasLinedefTag && ((tag.GetResult(0) < General.Map.FormatInterface.MinTag) || (tag.GetResult(0) > General.Map.FormatInterface.MaxTag)))
            {
                General.ShowWarningMessage("Linedef tag must be between " + General.Map.FormatInterface.MinTag + " and " + General.Map.FormatInterface.MaxTag + ".", MessageBoxButtons.OK);
                return;
            }

            // Verify the action
            if ((action.Value < General.Map.FormatInterface.MinAction) || (action.Value > General.Map.FormatInterface.MaxAction))
            {
                General.ShowWarningMessage("Linedef action must be between " + General.Map.FormatInterface.MinAction + " and " + General.Map.FormatInterface.MaxAction + ".", MessageBoxButtons.OK);
                return;
            }

            // Verify texture offsets
            if ((backoffsetx.GetResult(0) < General.Map.FormatInterface.MinTextureOffset) || (backoffsetx.GetResult(0) > General.Map.FormatInterface.MaxTextureOffset) ||
               (backoffsety.GetResult(0) < General.Map.FormatInterface.MinTextureOffset) || (backoffsety.GetResult(0) > General.Map.FormatInterface.MaxTextureOffset) ||
               (frontoffsetx.GetResult(0) < General.Map.FormatInterface.MinTextureOffset) || (frontoffsetx.GetResult(0) > General.Map.FormatInterface.MaxTextureOffset) ||
               (frontoffsety.GetResult(0) < General.Map.FormatInterface.MinTextureOffset) || (frontoffsety.GetResult(0) > General.Map.FormatInterface.MaxTextureOffset))
            {
                General.ShowWarningMessage("Texture offset must be between " + General.Map.FormatInterface.MinTextureOffset + " and " + General.Map.FormatInterface.MaxTextureOffset + ".", MessageBoxButtons.OK);
                return;
            }

            // Make undo
            if (lines.Count > 1) undodesc = lines.Count + " linedefs";
            General.Map.UndoRedo.CreateUndo("Edit " + undodesc);

            // Go for all the lines
            foreach (Linedef l in lines)
            {
                // Apply all flags
                foreach (CheckBox c in flags.Checkboxes)
                {
                    if (c.CheckState == CheckState.Checked) l.SetFlag(c.Tag.ToString(), true);
                    else if (c.CheckState == CheckState.Unchecked) l.SetFlag(c.Tag.ToString(), false);
                }

                l.Activate -= (l.Activate & 511);
                SetActivationFlag(l, activationtypered, 512);
                SetActivationFlag(l, activationtypeblue, 1024);
                SetActivationFlag(l, activationtypeyellow, 2048);
                SetActivationFlag(l, activationtypecross, 4096);
                SetActivationFlag(l, activationtypeshoot, 8192);
                SetActivationFlag(l, activationtypeuse, 16384);
                SetActivationFlag(l, activationtyperepeat, 32768);

                SetSwitchMask(l);

                // Action/tag
                l.Tag = General.Clamp(tag.GetResult(l.Tag), General.Map.FormatInterface.MinTag, General.Map.FormatInterface.MaxTag);
                if (!action.Empty) l.Action = action.Value;

                // Remove front side?
                if ((l.Front != null) && (frontside.CheckState == CheckState.Unchecked))
                {
                    l.Front.Dispose();
                }
                // Create or modify front side?
                else if (frontside.CheckState == CheckState.Checked)
                {
                    // Make sure we have a valid sector (make a new one if needed)
                    if (l.Front != null) index = l.Front.Sector.Index; else index = -1;
                    index = frontsector.GetResult(index);
                    if ((index > -1) && (index < General.Map.Map.Sectors.Count))
                    {
                        s = General.Map.Map.GetSectorByIndex(index);
                        if (s == null) s = General.Map.Map.CreateSector();
                        if (s != null)
                        {
                            // Create new sidedef?
                            if (l.Front == null) General.Map.Map.CreateSidedef(l, true, s);
                            if (l.Front != null)
                            {
                                // Change sector?
                                if (l.Front.Sector != s) l.Front.SetSector(s);

                                // Apply settings
                                l.Front.OffsetX = General.Clamp(frontoffsetx.GetResult(l.Front.OffsetX), General.Map.FormatInterface.MinTextureOffset, General.Map.FormatInterface.MaxTextureOffset);
                                l.Front.OffsetY = General.Clamp(frontoffsety.GetResult(l.Front.OffsetY), General.Map.FormatInterface.MinTextureOffset, General.Map.FormatInterface.MaxTextureOffset);
                                l.Front.SetTextureHigh(fronthigh.GetResult(l.Front.HighTexture));
                                l.Front.SetTextureMid(frontmid.GetResult(l.Front.MiddleTexture));
                                l.Front.SetTextureLow(frontlow.GetResult(l.Front.LowTexture));
                            }
                        }
                    }
                }

                // Remove back side?
                if ((l.Back != null) && (backside.CheckState == CheckState.Unchecked))
                {
                    l.Back.Dispose();
                }
                // Create or modify back side?
                else if (backside.CheckState == CheckState.Checked)
                {
                    // Make sure we have a valid sector (make a new one if needed)
                    if (l.Back != null) index = l.Back.Sector.Index; else index = -1;
                    index = backsector.GetResult(index);
                    if ((index > -1) && (index < General.Map.Map.Sectors.Count))
                    {
                        s = General.Map.Map.GetSectorByIndex(index);
                        if (s == null) s = General.Map.Map.CreateSector();
                        if (s != null)
                        {
                            // Create new sidedef?
                            if (l.Back == null) General.Map.Map.CreateSidedef(l, false, s);
                            if (l.Back != null)
                            {
                                // Change sector?
                                if (l.Back.Sector != s) l.Back.SetSector(s);

                                // Apply settings
                                l.Back.OffsetX = General.Clamp(backoffsetx.GetResult(l.Back.OffsetX), General.Map.FormatInterface.MinTextureOffset, General.Map.FormatInterface.MaxTextureOffset);
                                l.Back.OffsetY = General.Clamp(backoffsety.GetResult(l.Back.OffsetY), General.Map.FormatInterface.MinTextureOffset, General.Map.FormatInterface.MaxTextureOffset);
                                l.Back.SetTextureHigh(backhigh.GetResult(l.Back.HighTexture));
                                l.Back.SetTextureMid(backmid.GetResult(l.Back.MiddleTexture));
                                l.Back.SetTextureLow(backlow.GetResult(l.Back.LowTexture));
                            }
                        }
                    }
                }
            }

            // Update the used textures
            General.Map.Data.UpdateUsedTextures();

            // Done
            General.Map.IsChanged = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Cancel clicked
        private void cancel_Click(object sender, EventArgs e)
        {
            // Be gone
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // This finds a new (unused) tag
        private void newtag_Click(object sender, EventArgs e)
        {
            tag.Text = General.Map.Map.GetNewTag().ToString();
        }

        // Action changes
        private void action_ValueChanges(object sender, EventArgs e)
        {
            int showaction = 0;

            // Only when line type is known
            if (General.Map.Config.LinedefActions.ContainsKey(action.Value)) showaction = action.Value;
        }

        // Browse Action clicked
        private void browseaction_Click(object sender, EventArgs e)
        {
            action.Value = ActionBrowserForm.BrowseAction(this, action.Value);
        }

        // Custom fields on front sides
        private void customfrontbutton_Click(object sender, EventArgs e)
        {
            // Make collection of front sides
            List<MapElement> sides = new List<MapElement>(lines.Count);
            foreach (Linedef l in lines) if (l.Front != null) sides.Add(l.Front);

            // Edit these
            CustomFieldsForm.ShowDialog(this, "Front side custom fields", "sidedef", sides, General.Map.Config.SidedefFields);
        }

        // Custom fields on back sides
        private void custombackbutton_Click(object sender, EventArgs e)
        {
            // Make collection of back sides
            List<MapElement> sides = new List<MapElement>(lines.Count);
            foreach (Linedef l in lines) if (l.Back != null) sides.Add(l.Back);

            // Edit these
            CustomFieldsForm.ShowDialog(this, "Back side custom fields", "sidedef", sides, General.Map.Config.SidedefFields);
        }

        // Help!
        private void LinedefEditForm_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            General.ShowHelp("w_linedefedit.html");
            hlpevent.Handled = true;
        }

        private void chkSwitchDisplayUpper_CheckedChanged_1(object sender, EventArgs e)
        {
            if (this.chkSwitchDisplayUpper.Checked)
            {
                this.chkSwitchDisplayLower.Checked = false;
                this.chkSwitchDisplayMiddle.Checked = false;
            }
        }

        private void chkSwitchDisplayMiddle_CheckedChanged_1(object sender, EventArgs e)
        {
            if (this.chkSwitchDisplayMiddle.Checked)
            {
                this.chkSwitchDisplayLower.Checked = false;
                this.chkSwitchDisplayUpper.Checked = false;
            }
        }

        private void chkSwitchDisplayLower_CheckedChanged_1(object sender, EventArgs e)
        {
            if (this.chkSwitchDisplayLower.Checked)
            {
                this.chkSwitchDisplayUpper.Checked = false;
                this.chkSwitchDisplayMiddle.Checked = false;
            }
        }

        private void chkSwitchTextureUpper_CheckedChanged_1(object sender, EventArgs e)
        {
            if (this.chkSwitchTextureUpper.Checked)
            {
                this.chkSwitchTextureLower.Checked = false;
                this.chkSwitchTextureMiddle.Checked = false;
            }
        }

        private void chkSwitchTextureMiddle_CheckedChanged_1(object sender, EventArgs e)
        {
            if (this.chkSwitchTextureMiddle.Checked)
            {
                this.chkSwitchTextureLower.Checked = false;
                this.chkSwitchTextureUpper.Checked = false;
            }
        }

        private void chkSwitchTextureLower_CheckedChanged_1(object sender, EventArgs e)
        {
            if (this.chkSwitchTextureLower.Checked)
            {
                this.chkSwitchTextureUpper.Checked = false;
                this.chkSwitchTextureMiddle.Checked = false;
            }
        }
    }
}
