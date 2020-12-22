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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.statsTab = new System.Windows.Forms.TabPage();
            this.inventoryTab = new System.Windows.Forms.TabPage();
            this.lblTotalWeight = new System.Windows.Forms.Label();
            this.lblEquippedWeight = new System.Windows.Forms.Label();
            this.txtFeet = new System.Windows.Forms.Label();
            this.txtRing2 = new System.Windows.Forms.Label();
            this.txtRing1 = new System.Windows.Forms.Label();
            this.txtHands = new System.Windows.Forms.Label();
            this.txtArms = new System.Windows.Forms.Label();
            this.txtShoulders = new System.Windows.Forms.Label();
            this.txtWaist = new System.Windows.Forms.Label();
            this.txtBody = new System.Windows.Forms.Label();
            this.txtTorso = new System.Windows.Forms.Label();
            this.txtNeck = new System.Windows.Forms.Label();
            this.txtFace = new System.Windows.Forms.Label();
            this.txtHead = new System.Windows.Forms.Label();
            this.txtShield = new System.Windows.Forms.Label();
            this.txtArmor = new System.Windows.Forms.Label();
            this.txtOffHandWeapon = new System.Windows.Forms.Label();
            this.txtPrimaryWeapon = new System.Windows.Forms.Label();
            this.lblSlotless = new System.Windows.Forms.Label();
            this.lstSlotless = new System.Windows.Forms.ListBox();
            this.lblFeet = new System.Windows.Forms.Label();
            this.lblRing2 = new System.Windows.Forms.Label();
            this.lblRing1 = new System.Windows.Forms.Label();
            this.lblHands = new System.Windows.Forms.Label();
            this.lblArms = new System.Windows.Forms.Label();
            this.lblShoulders = new System.Windows.Forms.Label();
            this.lblWaist = new System.Windows.Forms.Label();
            this.lblBody = new System.Windows.Forms.Label();
            this.lblTorso = new System.Windows.Forms.Label();
            this.lblNeck = new System.Windows.Forms.Label();
            this.lblFace = new System.Windows.Forms.Label();
            this.lblHead = new System.Windows.Forms.Label();
            this.lblShield = new System.Windows.Forms.Label();
            this.lblArmor = new System.Windows.Forms.Label();
            this.lblOffHandWeapon = new System.Windows.Forms.Label();
            this.lblPrimaryWeapon = new System.Windows.Forms.Label();
            this.grpSampleLocation2 = new System.Windows.Forms.GroupBox();
            this.grpSampleLocation = new System.Windows.Forms.GroupBox();
            this.lstSampleItems = new System.Windows.Forms.ListBox();
            this.chkWeightCounts = new System.Windows.Forms.CheckBox();
            this.txtParam = new System.Windows.Forms.TextBox();
            this.txtParam2 = new System.Windows.Forms.TextBox();
            this.lblParam = new System.Windows.Forms.Label();
            this.lblParam2 = new System.Windows.Forms.Label();
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
            this.lblTopLeft.Location = new System.Drawing.Point(3, 0);
            this.lblTopLeft.Name = "lblTopLeft";
            this.lblTopLeft.Size = new System.Drawing.Size(76, 17);
            this.lblTopLeft.TabIndex = 0;
            this.lblTopLeft.Text = "Sample text";
            this.lblTopLeft.Visible = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
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
            this.tabControl1.Size = new System.Drawing.Size(913, 615);
            this.tabControl1.TabIndex = 1;
            // 
            // statsTab
            // 
            this.statsTab.Controls.Add(this.lblParam2);
            this.statsTab.Controls.Add(this.lblParam);
            this.statsTab.Controls.Add(this.txtParam2);
            this.statsTab.Controls.Add(this.txtParam);
            this.statsTab.Controls.Add(this.lblTopLeft);
            this.statsTab.Location = new System.Drawing.Point(4, 4);
            this.statsTab.Name = "statsTab";
            this.statsTab.Padding = new System.Windows.Forms.Padding(3);
            this.statsTab.Size = new System.Drawing.Size(905, 585);
            this.statsTab.TabIndex = 0;
            this.statsTab.Text = "Stats";
            this.statsTab.UseVisualStyleBackColor = true;
            // 
            // inventoryTab
            // 
            this.inventoryTab.Controls.Add(this.lblTotalWeight);
            this.inventoryTab.Controls.Add(this.lblEquippedWeight);
            this.inventoryTab.Controls.Add(this.txtFeet);
            this.inventoryTab.Controls.Add(this.txtRing2);
            this.inventoryTab.Controls.Add(this.txtRing1);
            this.inventoryTab.Controls.Add(this.txtHands);
            this.inventoryTab.Controls.Add(this.txtArms);
            this.inventoryTab.Controls.Add(this.txtShoulders);
            this.inventoryTab.Controls.Add(this.txtWaist);
            this.inventoryTab.Controls.Add(this.txtBody);
            this.inventoryTab.Controls.Add(this.txtTorso);
            this.inventoryTab.Controls.Add(this.txtNeck);
            this.inventoryTab.Controls.Add(this.txtFace);
            this.inventoryTab.Controls.Add(this.txtHead);
            this.inventoryTab.Controls.Add(this.txtShield);
            this.inventoryTab.Controls.Add(this.txtArmor);
            this.inventoryTab.Controls.Add(this.txtOffHandWeapon);
            this.inventoryTab.Controls.Add(this.txtPrimaryWeapon);
            this.inventoryTab.Controls.Add(this.lblSlotless);
            this.inventoryTab.Controls.Add(this.lstSlotless);
            this.inventoryTab.Controls.Add(this.lblFeet);
            this.inventoryTab.Controls.Add(this.lblRing2);
            this.inventoryTab.Controls.Add(this.lblRing1);
            this.inventoryTab.Controls.Add(this.lblHands);
            this.inventoryTab.Controls.Add(this.lblArms);
            this.inventoryTab.Controls.Add(this.lblShoulders);
            this.inventoryTab.Controls.Add(this.lblWaist);
            this.inventoryTab.Controls.Add(this.lblBody);
            this.inventoryTab.Controls.Add(this.lblTorso);
            this.inventoryTab.Controls.Add(this.lblNeck);
            this.inventoryTab.Controls.Add(this.lblFace);
            this.inventoryTab.Controls.Add(this.lblHead);
            this.inventoryTab.Controls.Add(this.lblShield);
            this.inventoryTab.Controls.Add(this.lblArmor);
            this.inventoryTab.Controls.Add(this.lblOffHandWeapon);
            this.inventoryTab.Controls.Add(this.lblPrimaryWeapon);
            this.inventoryTab.Controls.Add(this.grpSampleLocation2);
            this.inventoryTab.Controls.Add(this.grpSampleLocation);
            this.inventoryTab.Location = new System.Drawing.Point(4, 4);
            this.inventoryTab.Name = "inventoryTab";
            this.inventoryTab.Padding = new System.Windows.Forms.Padding(3);
            this.inventoryTab.Size = new System.Drawing.Size(905, 585);
            this.inventoryTab.TabIndex = 1;
            this.inventoryTab.Text = "Inventory";
            this.inventoryTab.UseVisualStyleBackColor = true;
            this.inventoryTab.Paint += new System.Windows.Forms.PaintEventHandler(this.InventoryTab_Paint);
            // 
            // lblTotalWeight
            // 
            this.lblTotalWeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblTotalWeight.AutoSize = true;
            this.lblTotalWeight.Location = new System.Drawing.Point(202, 565);
            this.lblTotalWeight.Name = "lblTotalWeight";
            this.lblTotalWeight.Size = new System.Drawing.Size(66, 17);
            this.lblTotalWeight.TabIndex = 3;
            this.lblTotalWeight.Text = "Total load";
            // 
            // lblEquippedWeight
            // 
            this.lblEquippedWeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblEquippedWeight.AutoSize = true;
            this.lblEquippedWeight.Location = new System.Drawing.Point(3, 565);
            this.lblEquippedWeight.Name = "lblEquippedWeight";
            this.lblEquippedWeight.Size = new System.Drawing.Size(106, 17);
            this.lblEquippedWeight.TabIndex = 54;
            this.lblEquippedWeight.Text = "Equipped weight";
            // 
            // txtFeet
            // 
            this.txtFeet.AllowDrop = true;
            this.txtFeet.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFeet.Location = new System.Drawing.Point(123, 470);
            this.txtFeet.Name = "txtFeet";
            this.txtFeet.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtFeet.Size = new System.Drawing.Size(231, 24);
            this.txtFeet.TabIndex = 53;
            this.txtFeet.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtFeet.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtFeet.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtFeet.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtRing2
            // 
            this.txtRing2.AllowDrop = true;
            this.txtRing2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtRing2.Location = new System.Drawing.Point(123, 439);
            this.txtRing2.Name = "txtRing2";
            this.txtRing2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtRing2.Size = new System.Drawing.Size(231, 24);
            this.txtRing2.TabIndex = 52;
            this.txtRing2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtRing2.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtRing2.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtRing2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtRing1
            // 
            this.txtRing1.AllowDrop = true;
            this.txtRing1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtRing1.Location = new System.Drawing.Point(123, 408);
            this.txtRing1.Name = "txtRing1";
            this.txtRing1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtRing1.Size = new System.Drawing.Size(231, 24);
            this.txtRing1.TabIndex = 51;
            this.txtRing1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtRing1.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtRing1.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtRing1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtHands
            // 
            this.txtHands.AllowDrop = true;
            this.txtHands.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtHands.Location = new System.Drawing.Point(123, 377);
            this.txtHands.Name = "txtHands";
            this.txtHands.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtHands.Size = new System.Drawing.Size(231, 24);
            this.txtHands.TabIndex = 50;
            this.txtHands.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtHands.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtHands.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtHands.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtArms
            // 
            this.txtArms.AllowDrop = true;
            this.txtArms.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtArms.Location = new System.Drawing.Point(123, 346);
            this.txtArms.Name = "txtArms";
            this.txtArms.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtArms.Size = new System.Drawing.Size(231, 24);
            this.txtArms.TabIndex = 49;
            this.txtArms.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtArms.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtArms.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtArms.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtShoulders
            // 
            this.txtShoulders.AllowDrop = true;
            this.txtShoulders.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtShoulders.Location = new System.Drawing.Point(123, 315);
            this.txtShoulders.Name = "txtShoulders";
            this.txtShoulders.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtShoulders.Size = new System.Drawing.Size(231, 24);
            this.txtShoulders.TabIndex = 48;
            this.txtShoulders.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtShoulders.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtShoulders.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtShoulders.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtWaist
            // 
            this.txtWaist.AllowDrop = true;
            this.txtWaist.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtWaist.Location = new System.Drawing.Point(123, 284);
            this.txtWaist.Name = "txtWaist";
            this.txtWaist.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtWaist.Size = new System.Drawing.Size(231, 24);
            this.txtWaist.TabIndex = 47;
            this.txtWaist.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtWaist.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtWaist.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtWaist.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtBody
            // 
            this.txtBody.AllowDrop = true;
            this.txtBody.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBody.Location = new System.Drawing.Point(123, 253);
            this.txtBody.Name = "txtBody";
            this.txtBody.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtBody.Size = new System.Drawing.Size(231, 24);
            this.txtBody.TabIndex = 46;
            this.txtBody.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtBody.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtBody.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtBody.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtTorso
            // 
            this.txtTorso.AllowDrop = true;
            this.txtTorso.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTorso.Location = new System.Drawing.Point(123, 222);
            this.txtTorso.Name = "txtTorso";
            this.txtTorso.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtTorso.Size = new System.Drawing.Size(231, 24);
            this.txtTorso.TabIndex = 45;
            this.txtTorso.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtTorso.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtTorso.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtTorso.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtNeck
            // 
            this.txtNeck.AllowDrop = true;
            this.txtNeck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNeck.Location = new System.Drawing.Point(123, 191);
            this.txtNeck.Name = "txtNeck";
            this.txtNeck.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtNeck.Size = new System.Drawing.Size(231, 24);
            this.txtNeck.TabIndex = 44;
            this.txtNeck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtNeck.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtNeck.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtNeck.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtFace
            // 
            this.txtFace.AllowDrop = true;
            this.txtFace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFace.Location = new System.Drawing.Point(123, 160);
            this.txtFace.Name = "txtFace";
            this.txtFace.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtFace.Size = new System.Drawing.Size(231, 24);
            this.txtFace.TabIndex = 43;
            this.txtFace.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtFace.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtFace.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtFace.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtHead
            // 
            this.txtHead.AllowDrop = true;
            this.txtHead.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtHead.Location = new System.Drawing.Point(123, 129);
            this.txtHead.Name = "txtHead";
            this.txtHead.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtHead.Size = new System.Drawing.Size(231, 24);
            this.txtHead.TabIndex = 42;
            this.txtHead.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtHead.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slot_DragDrop);
            this.txtHead.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slot_DragEnter);
            this.txtHead.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slot_MouseDown);
            // 
            // txtShield
            // 
            this.txtShield.AllowDrop = true;
            this.txtShield.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtShield.Location = new System.Drawing.Point(123, 98);
            this.txtShield.Name = "txtShield";
            this.txtShield.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtShield.Size = new System.Drawing.Size(231, 24);
            this.txtShield.TabIndex = 41;
            this.txtShield.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtShield.DragDrop += new System.Windows.Forms.DragEventHandler(this.Shield_DragDrop);
            this.txtShield.DragEnter += new System.Windows.Forms.DragEventHandler(this.Shield_DragEnter);
            this.txtShield.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Shield_MouseDown);
            // 
            // txtArmor
            // 
            this.txtArmor.AllowDrop = true;
            this.txtArmor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtArmor.Location = new System.Drawing.Point(123, 67);
            this.txtArmor.Name = "txtArmor";
            this.txtArmor.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtArmor.Size = new System.Drawing.Size(231, 24);
            this.txtArmor.TabIndex = 40;
            this.txtArmor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtArmor.DragDrop += new System.Windows.Forms.DragEventHandler(this.Armor_DragDrop);
            this.txtArmor.DragEnter += new System.Windows.Forms.DragEventHandler(this.Armor_DragEnter);
            this.txtArmor.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Armor_MouseDown);
            // 
            // txtOffHandWeapon
            // 
            this.txtOffHandWeapon.AllowDrop = true;
            this.txtOffHandWeapon.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOffHandWeapon.Location = new System.Drawing.Point(123, 36);
            this.txtOffHandWeapon.Name = "txtOffHandWeapon";
            this.txtOffHandWeapon.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtOffHandWeapon.Size = new System.Drawing.Size(231, 24);
            this.txtOffHandWeapon.TabIndex = 39;
            this.txtOffHandWeapon.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtOffHandWeapon.DragDrop += new System.Windows.Forms.DragEventHandler(this.Weapon_DragDrop);
            this.txtOffHandWeapon.DragEnter += new System.Windows.Forms.DragEventHandler(this.Weapon_DragEnter);
            this.txtOffHandWeapon.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Weapon_MouseDown);
            // 
            // txtPrimaryWeapon
            // 
            this.txtPrimaryWeapon.AllowDrop = true;
            this.txtPrimaryWeapon.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPrimaryWeapon.Location = new System.Drawing.Point(123, 5);
            this.txtPrimaryWeapon.Name = "txtPrimaryWeapon";
            this.txtPrimaryWeapon.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtPrimaryWeapon.Size = new System.Drawing.Size(231, 24);
            this.txtPrimaryWeapon.TabIndex = 38;
            this.txtPrimaryWeapon.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtPrimaryWeapon.DragDrop += new System.Windows.Forms.DragEventHandler(this.Weapon_DragDrop);
            this.txtPrimaryWeapon.DragEnter += new System.Windows.Forms.DragEventHandler(this.Weapon_DragEnter);
            this.txtPrimaryWeapon.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Weapon_MouseDown);
            // 
            // lblSlotless
            // 
            this.lblSlotless.Location = new System.Drawing.Point(8, 503);
            this.lblSlotless.Name = "lblSlotless";
            this.lblSlotless.Size = new System.Drawing.Size(111, 17);
            this.lblSlotless.TabIndex = 37;
            this.lblSlotless.Text = "Slotless";
            this.lblSlotless.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lstSlotless
            // 
            this.lstSlotless.AllowDrop = true;
            this.lstSlotless.FormattingEnabled = true;
            this.lstSlotless.ItemHeight = 17;
            this.lstSlotless.Location = new System.Drawing.Point(123, 503);
            this.lstSlotless.Name = "lstSlotless";
            this.lstSlotless.Size = new System.Drawing.Size(231, 38);
            this.lstSlotless.TabIndex = 36;
            this.lstSlotless.DragDrop += new System.Windows.Forms.DragEventHandler(this.Slotless_DragDrop);
            this.lstSlotless.DragEnter += new System.Windows.Forms.DragEventHandler(this.Slotless_DragEnter);
            this.lstSlotless.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Slotless_MouseDown);
            // 
            // lblFeet
            // 
            this.lblFeet.Location = new System.Drawing.Point(8, 474);
            this.lblFeet.Name = "lblFeet";
            this.lblFeet.Size = new System.Drawing.Size(111, 17);
            this.lblFeet.TabIndex = 35;
            this.lblFeet.Text = "Feet";
            this.lblFeet.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblRing2
            // 
            this.lblRing2.Location = new System.Drawing.Point(8, 443);
            this.lblRing2.Name = "lblRing2";
            this.lblRing2.Size = new System.Drawing.Size(111, 17);
            this.lblRing2.TabIndex = 34;
            this.lblRing2.Text = "Ring";
            this.lblRing2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblRing1
            // 
            this.lblRing1.Location = new System.Drawing.Point(8, 412);
            this.lblRing1.Name = "lblRing1";
            this.lblRing1.Size = new System.Drawing.Size(111, 17);
            this.lblRing1.TabIndex = 33;
            this.lblRing1.Text = "Ring";
            this.lblRing1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblHands
            // 
            this.lblHands.Location = new System.Drawing.Point(8, 381);
            this.lblHands.Name = "lblHands";
            this.lblHands.Size = new System.Drawing.Size(111, 17);
            this.lblHands.TabIndex = 32;
            this.lblHands.Text = "Hands";
            this.lblHands.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblArms
            // 
            this.lblArms.Location = new System.Drawing.Point(8, 350);
            this.lblArms.Name = "lblArms";
            this.lblArms.Size = new System.Drawing.Size(111, 17);
            this.lblArms.TabIndex = 31;
            this.lblArms.Text = "Arms";
            this.lblArms.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblShoulders
            // 
            this.lblShoulders.Location = new System.Drawing.Point(8, 319);
            this.lblShoulders.Name = "lblShoulders";
            this.lblShoulders.Size = new System.Drawing.Size(111, 17);
            this.lblShoulders.TabIndex = 30;
            this.lblShoulders.Text = "Shoulders";
            this.lblShoulders.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblWaist
            // 
            this.lblWaist.Location = new System.Drawing.Point(8, 288);
            this.lblWaist.Name = "lblWaist";
            this.lblWaist.Size = new System.Drawing.Size(111, 17);
            this.lblWaist.TabIndex = 29;
            this.lblWaist.Text = "Waist";
            this.lblWaist.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblBody
            // 
            this.lblBody.Location = new System.Drawing.Point(8, 257);
            this.lblBody.Name = "lblBody";
            this.lblBody.Size = new System.Drawing.Size(111, 17);
            this.lblBody.TabIndex = 28;
            this.lblBody.Text = "Body";
            this.lblBody.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblTorso
            // 
            this.lblTorso.Location = new System.Drawing.Point(8, 226);
            this.lblTorso.Name = "lblTorso";
            this.lblTorso.Size = new System.Drawing.Size(111, 17);
            this.lblTorso.TabIndex = 27;
            this.lblTorso.Text = "Torso";
            this.lblTorso.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblNeck
            // 
            this.lblNeck.Location = new System.Drawing.Point(8, 195);
            this.lblNeck.Name = "lblNeck";
            this.lblNeck.Size = new System.Drawing.Size(111, 17);
            this.lblNeck.TabIndex = 26;
            this.lblNeck.Text = "Neck";
            this.lblNeck.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblFace
            // 
            this.lblFace.Location = new System.Drawing.Point(8, 164);
            this.lblFace.Name = "lblFace";
            this.lblFace.Size = new System.Drawing.Size(111, 17);
            this.lblFace.TabIndex = 25;
            this.lblFace.Text = "Face";
            this.lblFace.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblHead
            // 
            this.lblHead.Location = new System.Drawing.Point(8, 133);
            this.lblHead.Name = "lblHead";
            this.lblHead.Size = new System.Drawing.Size(111, 17);
            this.lblHead.TabIndex = 24;
            this.lblHead.Text = "Head";
            this.lblHead.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblShield
            // 
            this.lblShield.Location = new System.Drawing.Point(8, 102);
            this.lblShield.Name = "lblShield";
            this.lblShield.Size = new System.Drawing.Size(111, 17);
            this.lblShield.TabIndex = 23;
            this.lblShield.Text = "Shield";
            this.lblShield.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblArmor
            // 
            this.lblArmor.Location = new System.Drawing.Point(8, 71);
            this.lblArmor.Name = "lblArmor";
            this.lblArmor.Size = new System.Drawing.Size(111, 17);
            this.lblArmor.TabIndex = 22;
            this.lblArmor.Text = "Armor";
            this.lblArmor.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblOffHandWeapon
            // 
            this.lblOffHandWeapon.Location = new System.Drawing.Point(6, 40);
            this.lblOffHandWeapon.Name = "lblOffHandWeapon";
            this.lblOffHandWeapon.Size = new System.Drawing.Size(111, 17);
            this.lblOffHandWeapon.TabIndex = 21;
            this.lblOffHandWeapon.Text = "Off-hand weapon";
            this.lblOffHandWeapon.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblPrimaryWeapon
            // 
            this.lblPrimaryWeapon.Location = new System.Drawing.Point(6, 9);
            this.lblPrimaryWeapon.Name = "lblPrimaryWeapon";
            this.lblPrimaryWeapon.Size = new System.Drawing.Size(111, 17);
            this.lblPrimaryWeapon.TabIndex = 20;
            this.lblPrimaryWeapon.Text = "Primary weapon";
            this.lblPrimaryWeapon.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // grpSampleLocation2
            // 
            this.grpSampleLocation2.Location = new System.Drawing.Point(513, 6);
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
            this.grpSampleLocation.Location = new System.Drawing.Point(360, 6);
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
            // txtParam
            // 
            this.txtParam.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtParam.Location = new System.Drawing.Point(8, 554);
            this.txtParam.Name = "txtParam";
            this.txtParam.Size = new System.Drawing.Size(32, 25);
            this.txtParam.TabIndex = 1;
            this.txtParam.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtParam.Visible = false;
            // 
            // txtParam2
            // 
            this.txtParam2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtParam2.Location = new System.Drawing.Point(8, 523);
            this.txtParam2.Name = "txtParam2";
            this.txtParam2.Size = new System.Drawing.Size(32, 25);
            this.txtParam2.TabIndex = 2;
            this.txtParam2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtParam2.Visible = false;
            // 
            // lblParam
            // 
            this.lblParam.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblParam.AutoSize = true;
            this.lblParam.Location = new System.Drawing.Point(46, 557);
            this.lblParam.Name = "lblParam";
            this.lblParam.Size = new System.Drawing.Size(116, 17);
            this.lblParam.TabIndex = 3;
            this.lblParam.Text = "Sample parameter";
            this.lblParam.Visible = false;
            // 
            // lblParam2
            // 
            this.lblParam2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblParam2.AutoSize = true;
            this.lblParam2.Location = new System.Drawing.Point(46, 526);
            this.lblParam2.Name = "lblParam2";
            this.lblParam2.Size = new System.Drawing.Size(116, 17);
            this.lblParam2.TabIndex = 4;
            this.lblParam2.Text = "Sample parameter";
            this.lblParam2.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 642);
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
        private System.Windows.Forms.Label lblPrimaryWeapon;
        private System.Windows.Forms.Label lblNeck;
        private System.Windows.Forms.Label lblFace;
        private System.Windows.Forms.Label lblHead;
        private System.Windows.Forms.Label lblShield;
        private System.Windows.Forms.Label lblArmor;
        private System.Windows.Forms.Label lblOffHandWeapon;
        private System.Windows.Forms.Label lblSlotless;
        private System.Windows.Forms.ListBox lstSlotless;
        private System.Windows.Forms.Label lblFeet;
        private System.Windows.Forms.Label lblRing2;
        private System.Windows.Forms.Label lblRing1;
        private System.Windows.Forms.Label lblHands;
        private System.Windows.Forms.Label lblArms;
        private System.Windows.Forms.Label lblShoulders;
        private System.Windows.Forms.Label lblWaist;
        private System.Windows.Forms.Label lblBody;
        private System.Windows.Forms.Label lblTorso;
        private System.Windows.Forms.Label txtFeet;
        private System.Windows.Forms.Label txtRing2;
        private System.Windows.Forms.Label txtRing1;
        private System.Windows.Forms.Label txtHands;
        private System.Windows.Forms.Label txtArms;
        private System.Windows.Forms.Label txtShoulders;
        private System.Windows.Forms.Label txtWaist;
        private System.Windows.Forms.Label txtBody;
        private System.Windows.Forms.Label txtTorso;
        private System.Windows.Forms.Label txtNeck;
        private System.Windows.Forms.Label txtFace;
        private System.Windows.Forms.Label txtHead;
        private System.Windows.Forms.Label txtShield;
        private System.Windows.Forms.Label txtArmor;
        private System.Windows.Forms.Label txtOffHandWeapon;
        private System.Windows.Forms.Label txtPrimaryWeapon;
        private System.Windows.Forms.Label lblEquippedWeight;
        private System.Windows.Forms.Label lblParam2;
        private System.Windows.Forms.Label lblParam;
        private System.Windows.Forms.TextBox txtParam2;
        private System.Windows.Forms.TextBox txtParam;
    }
}

