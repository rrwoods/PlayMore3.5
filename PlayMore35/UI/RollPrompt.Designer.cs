namespace PlayMore35.UI {
    partial class RollPrompt {
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
            this.mskResult = new System.Windows.Forms.MaskedTextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.lblResult = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // mskResult
            // 
            this.mskResult.Location = new System.Drawing.Point(79, 12);
            this.mskResult.Mask = "####";
            this.mskResult.Name = "mskResult";
            this.mskResult.Size = new System.Drawing.Size(43, 20);
            this.mskResult.TabIndex = 0;
            this.mskResult.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.mskResult.TextChanged += new System.EventHandler(this.Result_TextChanged);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(128, 10);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(48, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // lblResult
            // 
            this.lblResult.AutoSize = true;
            this.lblResult.Location = new System.Drawing.Point(12, 15);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(61, 13);
            this.lblResult.TabIndex = 2;
            this.lblResult.Text = "Roll Result:";
            // 
            // RollPrompt
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(192, 47);
            this.ControlBox = false;
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.mskResult);
            this.Name = "RollPrompt";
            this.Text = "RollPrompt";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MaskedTextBox mskResult;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblResult;
    }
}