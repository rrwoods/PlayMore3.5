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
            this.SuspendLayout();
            // 
            // lblTopLeft
            // 
            this.lblTopLeft.AutoSize = true;
            this.lblTopLeft.Location = new System.Drawing.Point(15, 15);
            this.lblTopLeft.Name = "lblTopLeft";
            this.lblTopLeft.Size = new System.Drawing.Size(42, 15);
            this.lblTopLeft.TabIndex = 0;
            this.lblTopLeft.Text = "label1";
            this.lblTopLeft.Visible = false;
            // 
            // lblSecondLine
            // 
            this.lblSecondLine.AutoSize = true;
            this.lblSecondLine.Location = new System.Drawing.Point(15, 35);
            this.lblSecondLine.Name = "lblSecondLine";
            this.lblSecondLine.Size = new System.Drawing.Size(42, 15);
            this.lblSecondLine.TabIndex = 1;
            this.lblSecondLine.Text = "label1";
            this.lblSecondLine.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 416);
            this.Controls.Add(this.lblSecondLine);
            this.Controls.Add(this.lblTopLeft);
            this.Font = new System.Drawing.Font("Calibri", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTopLeft;
        private System.Windows.Forms.Label lblSecondLine;
    }
}

