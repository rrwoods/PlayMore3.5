#nullable enable
namespace PlayMore35 {
    partial class LevelDetailsDialog {
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
            this.lblClassHeader = new System.Windows.Forms.Label();
            this.cboClass = new System.Windows.Forms.ComboBox();
            this.cboFeat1 = new System.Windows.Forms.ComboBox();
            this.cboFeat2 = new System.Windows.Forms.ComboBox();
            this.lblFeatSlot1 = new System.Windows.Forms.Label();
            this.lblFeatSlot2 = new System.Windows.Forms.Label();
            this.lblFeatChoice = new System.Windows.Forms.Label();
            this.cboFeatChoice = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // lblClassHeader
            // 
            this.lblClassHeader.AutoSize = true;
            this.lblClassHeader.Location = new System.Drawing.Point(14, 14);
            this.lblClassHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblClassHeader.Name = "lblClassHeader";
            this.lblClassHeader.Size = new System.Drawing.Size(38, 17);
            this.lblClassHeader.TabIndex = 0;
            this.lblClassHeader.Text = "Class";
            // 
            // cboClass
            // 
            this.cboClass.FormattingEnabled = true;
            this.cboClass.Location = new System.Drawing.Point(17, 34);
            this.cboClass.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cboClass.Name = "cboClass";
            this.cboClass.Size = new System.Drawing.Size(232, 25);
            this.cboClass.TabIndex = 1;
            this.cboClass.SelectedValueChanged += new System.EventHandler(this.SelectClass);
            // 
            // cboFeat1
            // 
            this.cboFeat1.FormattingEnabled = true;
            this.cboFeat1.Location = new System.Drawing.Point(270, 34);
            this.cboFeat1.Name = "cboFeat1";
            this.cboFeat1.Size = new System.Drawing.Size(186, 25);
            this.cboFeat1.TabIndex = 3;
            this.cboFeat1.Visible = false;
            // 
            // cboFeat2
            // 
            this.cboFeat2.FormattingEnabled = true;
            this.cboFeat2.Location = new System.Drawing.Point(462, 34);
            this.cboFeat2.Name = "cboFeat2";
            this.cboFeat2.Size = new System.Drawing.Size(186, 25);
            this.cboFeat2.TabIndex = 4;
            this.cboFeat2.Visible = false;
            // 
            // lblFeatSlot1
            // 
            this.lblFeatSlot1.AutoSize = true;
            this.lblFeatSlot1.Location = new System.Drawing.Point(267, 14);
            this.lblFeatSlot1.Name = "lblFeatSlot1";
            this.lblFeatSlot1.Size = new System.Drawing.Size(57, 17);
            this.lblFeatSlot1.TabIndex = 5;
            this.lblFeatSlot1.Text = "Feat slot";
            this.lblFeatSlot1.Visible = false;
            // 
            // lblFeatSlot2
            // 
            this.lblFeatSlot2.AutoSize = true;
            this.lblFeatSlot2.Location = new System.Drawing.Point(459, 14);
            this.lblFeatSlot2.Name = "lblFeatSlot2";
            this.lblFeatSlot2.Size = new System.Drawing.Size(57, 17);
            this.lblFeatSlot2.TabIndex = 6;
            this.lblFeatSlot2.Text = "Feat slot";
            this.lblFeatSlot2.Visible = false;
            // 
            // lblFeatChoice
            // 
            this.lblFeatChoice.AutoSize = true;
            this.lblFeatChoice.Location = new System.Drawing.Point(267, 62);
            this.lblFeatChoice.Name = "lblFeatChoice";
            this.lblFeatChoice.Size = new System.Drawing.Size(73, 17);
            this.lblFeatChoice.TabIndex = 7;
            this.lblFeatChoice.Text = "Feat choice";
            this.lblFeatChoice.Visible = false;
            // 
            // cboFeatChoice
            // 
            this.cboFeatChoice.FormattingEnabled = true;
            this.cboFeatChoice.Location = new System.Drawing.Point(270, 82);
            this.cboFeatChoice.Name = "cboFeatChoice";
            this.cboFeatChoice.Size = new System.Drawing.Size(186, 25);
            this.cboFeatChoice.TabIndex = 8;
            this.cboFeatChoice.Visible = false;
            // 
            // LevelDetailsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(934, 589);
            this.Controls.Add(this.cboFeatChoice);
            this.Controls.Add(this.lblFeatChoice);
            this.Controls.Add(this.lblFeatSlot2);
            this.Controls.Add(this.lblFeatSlot1);
            this.Controls.Add(this.cboFeat2);
            this.Controls.Add(this.cboFeat1);
            this.Controls.Add(this.cboClass);
            this.Controls.Add(this.lblClassHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LevelDetailsDialog";
            this.Padding = new System.Windows.Forms.Padding(13);
            this.Text = "Level";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblClassHeader;
        private System.Windows.Forms.ComboBox cboClass;
        private System.Windows.Forms.ComboBox cboFeat1;
        private System.Windows.Forms.ComboBox cboFeat2;
        private System.Windows.Forms.Label lblFeatSlot1;
        private System.Windows.Forms.Label lblFeatSlot2;
        private System.Windows.Forms.Label lblFeatChoice;
        private System.Windows.Forms.ComboBox cboFeatChoice;
    }
}