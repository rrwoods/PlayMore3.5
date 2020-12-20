namespace PlayMore35 {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.lblTopLeft = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCharacterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCondition = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.statsTab = new System.Windows.Forms.TabPage();
            this.inventoryTab = new System.Windows.Forms.TabPage();
            this.lblTotalWeight = new System.Windows.Forms.Label();
            this.grpSampleLocation2 = new System.Windows.Forms.GroupBox();
            this.grpSampleLocation = new System.Windows.Forms.GroupBox();
            this.lstSampleItems = new System.Windows.Forms.ListBox();
            this.chkWeightCounts = new System.Windows.Forms.CheckBox();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.statsTab.SuspendLayout();
            this.inventoryTab.SuspendLayout();
            this.grpSampleLocation.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTopLeft
            // 
            this.lblTopLeft.AutoSize = true;
            this.lblTopLeft.Location = new System.Drawing.Point(3, 3);
            this.lblTopLeft.Name = "lblTopLeft";
            this.lblTopLeft.Size = new System.Drawing.Size(76, 17);
            this.lblTopLeft.TabIndex = 0;
            this.lblTopLeft.Text = "Sample text";
            this.lblTopLeft.Visible = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.mnuCondition});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(913, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveCharacterToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // saveCharacterToolStripMenuItem
            // 
            this.saveCharacterToolStripMenuItem.Name = "saveCharacterToolStripMenuItem";
            this.saveCharacterToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.saveCharacterToolStripMenuItem.Text = "&Save character...";
            this.saveCharacterToolStripMenuItem.Click += new System.EventHandler(this.SaveCharacter_Click);
            // 
            // mnuCondition
            // 
            this.mnuCondition.Name = "mnuCondition";
            this.mnuCondition.Size = new System.Drawing.Size(77, 20);
            this.mnuCondition.Text = "&Conditions";
            // 
            // tabControl1
            // 
            this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.statsTab);
            this.tabControl1.Controls.Add(this.inventoryTab);
            this.tabControl1.Location = new System.Drawing.Point(0, 27);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(913, 479);
            this.tabControl1.TabIndex = 1;
            // 
            // statsTab
            // 
            this.statsTab.Controls.Add(this.lblTopLeft);
            this.statsTab.Location = new System.Drawing.Point(4, 4);
            this.statsTab.Name = "statsTab";
            this.statsTab.Padding = new System.Windows.Forms.Padding(3);
            this.statsTab.Size = new System.Drawing.Size(905, 449);
            this.statsTab.TabIndex = 0;
            this.statsTab.Text = "Stats";
            this.statsTab.UseVisualStyleBackColor = true;
            // 
            // inventoryTab
            // 
            this.inventoryTab.Controls.Add(this.lblTotalWeight);
            this.inventoryTab.Controls.Add(this.grpSampleLocation2);
            this.inventoryTab.Controls.Add(this.grpSampleLocation);
            this.inventoryTab.Location = new System.Drawing.Point(4, 4);
            this.inventoryTab.Name = "inventoryTab";
            this.inventoryTab.Padding = new System.Windows.Forms.Padding(3);
            this.inventoryTab.Size = new System.Drawing.Size(905, 449);
            this.inventoryTab.TabIndex = 1;
            this.inventoryTab.Text = "Inventory";
            this.inventoryTab.UseVisualStyleBackColor = true;
            this.inventoryTab.Paint += new System.Windows.Forms.PaintEventHandler(this.InventoryTab_Paint);
            // 
            // lblTotalWeight
            // 
            this.lblTotalWeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblTotalWeight.AutoSize = true;
            this.lblTotalWeight.Location = new System.Drawing.Point(6, 429);
            this.lblTotalWeight.Name = "lblTotalWeight";
            this.lblTotalWeight.Size = new System.Drawing.Size(123, 17);
            this.lblTotalWeight.TabIndex = 3;
            this.lblTotalWeight.Text = "Total weight carried";
            // 
            // grpSampleLocation2
            // 
            this.grpSampleLocation2.Location = new System.Drawing.Point(161, 6);
            this.grpSampleLocation2.Name = "grpSampleLocation2";
            this.grpSampleLocation2.Size = new System.Drawing.Size(147, 73);
            this.grpSampleLocation2.TabIndex = 2;
            this.grpSampleLocation2.TabStop = false;
            this.grpSampleLocation2.Text = "Sample location";
            this.grpSampleLocation2.Visible = false;
            // 
            // grpSampleLocation
            // 
            this.grpSampleLocation.Controls.Add(this.lstSampleItems);
            this.grpSampleLocation.Controls.Add(this.chkWeightCounts);
            this.grpSampleLocation.Location = new System.Drawing.Point(8, 6);
            this.grpSampleLocation.Name = "grpSampleLocation";
            this.grpSampleLocation.Size = new System.Drawing.Size(147, 132);
            this.grpSampleLocation.TabIndex = 0;
            this.grpSampleLocation.TabStop = false;
            this.grpSampleLocation.Text = "Sample location";
            this.grpSampleLocation.Visible = false;
            // 
            // lstSampleItems
            // 
            this.lstSampleItems.FormattingEnabled = true;
            this.lstSampleItems.ItemHeight = 17;
            this.lstSampleItems.Location = new System.Drawing.Point(7, 52);
            this.lstSampleItems.Name = "lstSampleItems";
            this.lstSampleItems.Size = new System.Drawing.Size(134, 21);
            this.lstSampleItems.TabIndex = 3;
            // 
            // chkWeightCounts
            // 
            this.chkWeightCounts.AutoSize = true;
            this.chkWeightCounts.Location = new System.Drawing.Point(6, 24);
            this.chkWeightCounts.Name = "chkWeightCounts";
            this.chkWeightCounts.Size = new System.Drawing.Size(103, 21);
            this.chkWeightCounts.TabIndex = 2;
            this.chkWeightCounts.Text = "Count weight";
            this.chkWeightCounts.UseVisualStyleBackColor = true;
            this.chkWeightCounts.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 506);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "PlayMore3.5";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.statsTab.ResumeLayout(false);
            this.statsTab.PerformLayout();
            this.inventoryTab.ResumeLayout(false);
            this.inventoryTab.PerformLayout();
            this.grpSampleLocation.ResumeLayout(false);
            this.grpSampleLocation.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTopLeft;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuCondition;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveCharacterToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage statsTab;
        private System.Windows.Forms.TabPage inventoryTab;
        private System.Windows.Forms.GroupBox grpSampleLocation2;
        private System.Windows.Forms.GroupBox grpSampleLocation;
        private System.Windows.Forms.Label lblTotalWeight;
        private System.Windows.Forms.CheckBox chkWeightCounts;
        private System.Windows.Forms.ListBox lstSampleItems;
    }
}

