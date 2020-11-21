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
            this.lblSecondLine = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mnuCondition = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTopLeft
            // 
            this.lblTopLeft.AutoSize = true;
            this.lblTopLeft.Location = new System.Drawing.Point(12, 24);
            this.lblTopLeft.Name = "lblTopLeft";
            this.lblTopLeft.Size = new System.Drawing.Size(70, 16);
            this.lblTopLeft.TabIndex = 0;
            this.lblTopLeft.Text = "Sample text";
            this.lblTopLeft.Visible = false;
            // 
            // lblSecondLine
            // 
            this.lblSecondLine.AutoSize = true;
            this.lblSecondLine.Location = new System.Drawing.Point(12, 47);
            this.lblSecondLine.Name = "lblSecondLine";
            this.lblSecondLine.Size = new System.Drawing.Size(93, 32);
            this.lblSecondLine.TabIndex = 1;
            this.lblSecondLine.Text = "more stuff here!\r\nsecond line";
            this.lblSecondLine.Visible = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuCondition});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(913, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mnuCondition
            // 
            this.mnuCondition.Name = "mnuCondition";
            this.mnuCondition.Size = new System.Drawing.Size(77, 20);
            this.mnuCondition.Text = "Conditions";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 557);
            this.Controls.Add(this.lblSecondLine);
            this.Controls.Add(this.lblTopLeft);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Scala Sans", 9.749999F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "PlayMore3.5";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTopLeft;
        private System.Windows.Forms.Label lblSecondLine;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuCondition;
    }
}

