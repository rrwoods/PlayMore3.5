namespace PlayMore35 {
    partial class AddItemDialog {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
            this.btnAddItemCancel = new System.Windows.Forms.Button();
            this.btnAddItemAdd = new System.Windows.Forms.Button();
            this.tabAddItem = new System.Windows.Forms.TabControl();
            this.pagWeapons = new System.Windows.Forms.TabPage();
            this.lstAddWeapon = new System.Windows.Forms.ListBox();
            this.pagArmor = new System.Windows.Forms.TabPage();
            this.lstAddArmor = new System.Windows.Forms.ListBox();
            this.pagShields = new System.Windows.Forms.TabPage();
            this.lstAddShield = new System.Windows.Forms.ListBox();
            this.pagItems = new System.Windows.Forms.TabPage();
            this.lstAddItem = new System.Windows.Forms.ListBox();
            this.lstWeaponProperties = new System.Windows.Forms.ListBox();
            this.lstArmorProperties = new System.Windows.Forms.ListBox();
            this.lstShieldProperties = new System.Windows.Forms.ListBox();
            this.tabAddItem.SuspendLayout();
            this.pagWeapons.SuspendLayout();
            this.pagArmor.SuspendLayout();
            this.pagShields.SuspendLayout();
            this.pagItems.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnAddItemCancel
            // 
            this.btnAddItemCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddItemCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAddItemCancel.Location = new System.Drawing.Point(381, 517);
            this.btnAddItemCancel.Name = "btnAddItemCancel";
            this.btnAddItemCancel.Size = new System.Drawing.Size(75, 23);
            this.btnAddItemCancel.TabIndex = 0;
            this.btnAddItemCancel.Text = "Cancel";
            this.btnAddItemCancel.UseVisualStyleBackColor = true;
            // 
            // btnAddItemAdd
            // 
            this.btnAddItemAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddItemAdd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAddItemAdd.Location = new System.Drawing.Point(300, 517);
            this.btnAddItemAdd.Name = "btnAddItemAdd";
            this.btnAddItemAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAddItemAdd.TabIndex = 1;
            this.btnAddItemAdd.Text = "Add";
            this.btnAddItemAdd.UseVisualStyleBackColor = true;
            // 
            // tabAddItem
            // 
            this.tabAddItem.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabAddItem.Controls.Add(this.pagWeapons);
            this.tabAddItem.Controls.Add(this.pagArmor);
            this.tabAddItem.Controls.Add(this.pagShields);
            this.tabAddItem.Controls.Add(this.pagItems);
            this.tabAddItem.Location = new System.Drawing.Point(12, 12);
            this.tabAddItem.Name = "tabAddItem";
            this.tabAddItem.SelectedIndex = 0;
            this.tabAddItem.Size = new System.Drawing.Size(444, 499);
            this.tabAddItem.TabIndex = 2;
            // 
            // pagWeapons
            // 
            this.pagWeapons.Controls.Add(this.lstWeaponProperties);
            this.pagWeapons.Controls.Add(this.lstAddWeapon);
            this.pagWeapons.Location = new System.Drawing.Point(4, 22);
            this.pagWeapons.Name = "pagWeapons";
            this.pagWeapons.Padding = new System.Windows.Forms.Padding(3);
            this.pagWeapons.Size = new System.Drawing.Size(436, 473);
            this.pagWeapons.TabIndex = 0;
            this.pagWeapons.Text = "Weapons";
            this.pagWeapons.UseVisualStyleBackColor = true;
            // 
            // lstAddWeapon
            // 
            this.lstAddWeapon.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstAddWeapon.FormattingEnabled = true;
            this.lstAddWeapon.Location = new System.Drawing.Point(6, 6);
            this.lstAddWeapon.Name = "lstAddWeapon";
            this.lstAddWeapon.Size = new System.Drawing.Size(230, 459);
            this.lstAddWeapon.TabIndex = 0;
            // 
            // pagArmor
            // 
            this.pagArmor.Controls.Add(this.lstArmorProperties);
            this.pagArmor.Controls.Add(this.lstAddArmor);
            this.pagArmor.Location = new System.Drawing.Point(4, 22);
            this.pagArmor.Name = "pagArmor";
            this.pagArmor.Padding = new System.Windows.Forms.Padding(3);
            this.pagArmor.Size = new System.Drawing.Size(436, 473);
            this.pagArmor.TabIndex = 1;
            this.pagArmor.Text = "Armor";
            this.pagArmor.UseVisualStyleBackColor = true;
            // 
            // lstAddArmor
            // 
            this.lstAddArmor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstAddArmor.FormattingEnabled = true;
            this.lstAddArmor.Location = new System.Drawing.Point(6, 6);
            this.lstAddArmor.Name = "lstAddArmor";
            this.lstAddArmor.Size = new System.Drawing.Size(230, 459);
            this.lstAddArmor.TabIndex = 1;
            // 
            // pagShields
            // 
            this.pagShields.Controls.Add(this.lstShieldProperties);
            this.pagShields.Controls.Add(this.lstAddShield);
            this.pagShields.Location = new System.Drawing.Point(4, 22);
            this.pagShields.Name = "pagShields";
            this.pagShields.Size = new System.Drawing.Size(436, 473);
            this.pagShields.TabIndex = 2;
            this.pagShields.Text = "Shields";
            this.pagShields.UseVisualStyleBackColor = true;
            // 
            // lstAddShield
            // 
            this.lstAddShield.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstAddShield.FormattingEnabled = true;
            this.lstAddShield.Location = new System.Drawing.Point(6, 6);
            this.lstAddShield.Name = "lstAddShield";
            this.lstAddShield.Size = new System.Drawing.Size(230, 459);
            this.lstAddShield.TabIndex = 1;
            // 
            // pagItems
            // 
            this.pagItems.Controls.Add(this.lstAddItem);
            this.pagItems.Location = new System.Drawing.Point(4, 22);
            this.pagItems.Name = "pagItems";
            this.pagItems.Size = new System.Drawing.Size(436, 473);
            this.pagItems.TabIndex = 3;
            this.pagItems.Text = "Other Items";
            this.pagItems.UseVisualStyleBackColor = true;
            // 
            // lstAddItem
            // 
            this.lstAddItem.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstAddItem.FormattingEnabled = true;
            this.lstAddItem.Location = new System.Drawing.Point(6, 6);
            this.lstAddItem.Name = "lstAddItem";
            this.lstAddItem.Size = new System.Drawing.Size(424, 459);
            this.lstAddItem.TabIndex = 1;
            // 
            // lstWeaponProperties
            // 
            this.lstWeaponProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstWeaponProperties.FormattingEnabled = true;
            this.lstWeaponProperties.Location = new System.Drawing.Point(242, 6);
            this.lstWeaponProperties.Name = "lstWeaponProperties";
            this.lstWeaponProperties.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lstWeaponProperties.Size = new System.Drawing.Size(188, 459);
            this.lstWeaponProperties.TabIndex = 1;
            // 
            // lstArmorProperties
            // 
            this.lstArmorProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstArmorProperties.FormattingEnabled = true;
            this.lstArmorProperties.Location = new System.Drawing.Point(242, 6);
            this.lstArmorProperties.Name = "lstArmorProperties";
            this.lstArmorProperties.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lstArmorProperties.Size = new System.Drawing.Size(188, 459);
            this.lstArmorProperties.TabIndex = 2;
            // 
            // lstShieldProperties
            // 
            this.lstShieldProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstShieldProperties.FormattingEnabled = true;
            this.lstShieldProperties.Location = new System.Drawing.Point(242, 6);
            this.lstShieldProperties.Name = "lstShieldProperties";
            this.lstShieldProperties.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lstShieldProperties.Size = new System.Drawing.Size(188, 459);
            this.lstShieldProperties.TabIndex = 2;
            // 
            // AddItemDialog
            // 
            this.AcceptButton = this.btnAddItemAdd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAddItemCancel;
            this.ClientSize = new System.Drawing.Size(468, 552);
            this.Controls.Add(this.tabAddItem);
            this.Controls.Add(this.btnAddItemAdd);
            this.Controls.Add(this.btnAddItemCancel);
            this.Name = "AddItemDialog";
            this.Text = "Choose an item";
            this.tabAddItem.ResumeLayout(false);
            this.pagWeapons.ResumeLayout(false);
            this.pagArmor.ResumeLayout(false);
            this.pagShields.ResumeLayout(false);
            this.pagItems.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnAddItemCancel;
        private System.Windows.Forms.Button btnAddItemAdd;
        public System.Windows.Forms.ListBox lstAddWeapon;
        public System.Windows.Forms.ListBox lstAddShield;
        public System.Windows.Forms.ListBox lstAddItem;
        public System.Windows.Forms.ListBox lstAddArmor;
        public System.Windows.Forms.TabControl tabAddItem;
        public System.Windows.Forms.TabPage pagWeapons;
        public System.Windows.Forms.TabPage pagArmor;
        public System.Windows.Forms.TabPage pagShields;
        public System.Windows.Forms.TabPage pagItems;
        public System.Windows.Forms.ListBox lstWeaponProperties;
        public System.Windows.Forms.ListBox lstArmorProperties;
        public System.Windows.Forms.ListBox lstShieldProperties;
    }
}