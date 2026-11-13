namespace PlayMore35 {
    partial class PickTime {
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
            this.lblModeDescription = new System.Windows.Forms.Label();
            this.lblMonthHeader = new System.Windows.Forms.Label();
            this.lblDayHeader = new System.Windows.Forms.Label();
            this.lblYearHeader = new System.Windows.Forms.Label();
            this.cmbMonth = new System.Windows.Forms.ComboBox();
            this.nudDay = new SpinnerNumericUpDown();
            this.nudYear = new SpinnerNumericUpDown();
            this.lblTimeHeader = new System.Windows.Forms.Label();
            this.nudHour = new SpinnerNumericUpDown();
            this.lblColon1 = new System.Windows.Forms.Label();
            this.nudMinute = new SpinnerNumericUpDown("D2");
            this.lblColon2 = new System.Windows.Forms.Label();
            this.nudSecond = new SpinnerNumericUpDown("D2");
            this.cmbAmPm = new System.Windows.Forms.ComboBox();
            this.lblValidation = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudDay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudYear)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHour)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinute)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSecond)).BeginInit();
            this.SuspendLayout();
            // 
            // lblModeDescription
            // 
            this.lblModeDescription.Location = new System.Drawing.Point(14, 12);
            this.lblModeDescription.Name = "lblModeDescription";
            this.lblModeDescription.Size = new System.Drawing.Size(360, 52);
            this.lblModeDescription.TabIndex = 0;
            // 
            // lblMonthHeader
            // 
            this.lblMonthHeader.Location = new System.Drawing.Point(14, 70);
            this.lblMonthHeader.Name = "lblMonthHeader";
            this.lblMonthHeader.Size = new System.Drawing.Size(140, 18);
            this.lblMonthHeader.TabIndex = 1;
            this.lblMonthHeader.Text = "Month";
            // 
            // lblDayHeader
            // 
            this.lblDayHeader.Location = new System.Drawing.Point(162, 70);
            this.lblDayHeader.Name = "lblDayHeader";
            this.lblDayHeader.Size = new System.Drawing.Size(76, 18);
            this.lblDayHeader.TabIndex = 2;
            this.lblDayHeader.Text = "Day";
            // 
            // lblYearHeader
            // 
            this.lblYearHeader.Location = new System.Drawing.Point(246, 70);
            this.lblYearHeader.Name = "lblYearHeader";
            this.lblYearHeader.Size = new System.Drawing.Size(128, 18);
            this.lblYearHeader.TabIndex = 3;
            this.lblYearHeader.Text = "Year";
            // 
            // cmbMonth
            // 
            this.cmbMonth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMonth.FormattingEnabled = true;
            this.cmbMonth.Location = new System.Drawing.Point(14, 90);
            this.cmbMonth.Name = "cmbMonth";
            this.cmbMonth.Size = new System.Drawing.Size(140, 25);
            this.cmbMonth.TabIndex = 4;
            // 
            // nudDay
            // 
            this.nudDay.Location = new System.Drawing.Point(162, 90);
            this.nudDay.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.nudDay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudDay.Name = "nudDay";
            this.nudDay.Size = new System.Drawing.Size(76, 25);
            this.nudDay.TabIndex = 5;
            this.nudDay.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudYear
            // 
            this.nudYear.Location = new System.Drawing.Point(246, 90);
            this.nudYear.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.nudYear.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudYear.Name = "nudYear";
            this.nudYear.Size = new System.Drawing.Size(128, 25);
            this.nudYear.TabIndex = 6;
            this.nudYear.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblTimeHeader
            // 
            this.lblTimeHeader.Location = new System.Drawing.Point(14, 122);
            this.lblTimeHeader.Name = "lblTimeHeader";
            this.lblTimeHeader.Size = new System.Drawing.Size(260, 18);
            this.lblTimeHeader.TabIndex = 7;
            this.lblTimeHeader.Text = "Time (Hour : Minute : Second)";
            // 
            // nudHour
            // 
            this.nudHour.Location = new System.Drawing.Point(14, 142);
            this.nudHour.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
            this.nudHour.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudHour.Name = "nudHour";
            this.nudHour.Size = new System.Drawing.Size(68, 25);
            this.nudHour.TabIndex = 8;
            this.nudHour.Value = new decimal(new int[] { 12, 0, 0, 0 });
            // 
            // lblColon1
            // 
            this.lblColon1.Location = new System.Drawing.Point(84, 145);
            this.lblColon1.Name = "lblColon1";
            this.lblColon1.Size = new System.Drawing.Size(12, 20);
            this.lblColon1.TabIndex = 9;
            this.lblColon1.Text = ":";
            this.lblColon1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nudMinute
            // 
            this.nudMinute.Location = new System.Drawing.Point(98, 142);
            this.nudMinute.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            this.nudMinute.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudMinute.Name = "nudMinute";
            this.nudMinute.Size = new System.Drawing.Size(68, 25);
            this.nudMinute.TabIndex = 10;
            // 
            // lblColon2
            // 
            this.lblColon2.Location = new System.Drawing.Point(168, 145);
            this.lblColon2.Name = "lblColon2";
            this.lblColon2.Size = new System.Drawing.Size(12, 20);
            this.lblColon2.TabIndex = 11;
            this.lblColon2.Text = ":";
            this.lblColon2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nudSecond
            // 
            this.nudSecond.Increment = new decimal(new int[] { 6, 0, 0, 0 });
            this.nudSecond.Location = new System.Drawing.Point(182, 142);
            this.nudSecond.Maximum = new decimal(new int[] { 54, 0, 0, 0 });
            this.nudSecond.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudSecond.Name = "nudSecond";
            this.nudSecond.Size = new System.Drawing.Size(68, 25);
            this.nudSecond.TabIndex = 12;
            // 
            // cmbAmPm
            // 
            this.cmbAmPm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAmPm.FormattingEnabled = true;
            this.cmbAmPm.Location = new System.Drawing.Point(258, 142);
            this.cmbAmPm.Name = "cmbAmPm";
            this.cmbAmPm.Size = new System.Drawing.Size(72, 25);
            this.cmbAmPm.TabIndex = 13;
            // 
            // lblValidation
            // 
            this.lblValidation.ForeColor = System.Drawing.Color.Firebrick;
            this.lblValidation.Location = new System.Drawing.Point(14, 173);
            this.lblValidation.Name = "lblValidation";
            this.lblValidation.Size = new System.Drawing.Size(360, 20);
            this.lblValidation.TabIndex = 14;
            this.lblValidation.Visible = false;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(14, 198);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 28);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(214, 198);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(160, 28);
            this.btnOK.TabIndex = 16;
            this.btnOK.Text = "Advance Time";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // PickTime
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(388, 238);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblValidation);
            this.Controls.Add(this.cmbAmPm);
            this.Controls.Add(this.nudSecond);
            this.Controls.Add(this.lblColon2);
            this.Controls.Add(this.nudMinute);
            this.Controls.Add(this.lblColon1);
            this.Controls.Add(this.nudHour);
            this.Controls.Add(this.lblTimeHeader);
            this.Controls.Add(this.nudYear);
            this.Controls.Add(this.nudDay);
            this.Controls.Add(this.cmbMonth);
            this.Controls.Add(this.lblYearHeader);
            this.Controls.Add(this.lblDayHeader);
            this.Controls.Add(this.lblMonthHeader);
            this.Controls.Add(this.lblModeDescription);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PickTime";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advance In-Game Time";
            ((System.ComponentModel.ISupportInitialize)(this.nudDay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudYear)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHour)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinute)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSecond)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Label lblModeDescription = null!;
        private System.Windows.Forms.Label lblMonthHeader = null!;
        private System.Windows.Forms.Label lblDayHeader = null!;
        private System.Windows.Forms.Label lblYearHeader = null!;
        private System.Windows.Forms.ComboBox cmbMonth = null!;
        private SpinnerNumericUpDown nudDay = null!;
        private SpinnerNumericUpDown nudYear = null!;
        private System.Windows.Forms.Label lblTimeHeader = null!;
        private SpinnerNumericUpDown nudHour = null!;
        private System.Windows.Forms.Label lblColon1 = null!;
        private SpinnerNumericUpDown nudMinute = null!;
        private System.Windows.Forms.Label lblColon2 = null!;
        private SpinnerNumericUpDown nudSecond = null!;
        private System.Windows.Forms.ComboBox cmbAmPm = null!;
        private System.Windows.Forms.Label lblValidation = null!;
        private System.Windows.Forms.Button btnOK = null!;
        private System.Windows.Forms.Button btnCancel = null!;
    }
}
