namespace Assistant.JMap
{
    partial class NewMarker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
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
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.newMarkerName = new System.Windows.Forms.TextBox();
            this.newMarkerExtra = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.newMarkerX = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.newMarkerY = new System.Windows.Forms.TextBox();
            this.newMarkerOk = new System.Windows.Forms.Button();
            this.newMarkerCancel = new System.Windows.Forms.Button();
            this.IsPublicCheckbox = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.OwnerLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Extra:";
            // 
            // newMarkerName
            // 
            this.newMarkerName.Location = new System.Drawing.Point(48, 8);
            this.newMarkerName.Name = "newMarkerName";
            this.newMarkerName.Size = new System.Drawing.Size(131, 20);
            this.newMarkerName.TabIndex = 2;
            // 
            // newMarkerExtra
            // 
            this.newMarkerExtra.Location = new System.Drawing.Point(44, 67);
            this.newMarkerExtra.Multiline = true;
            this.newMarkerExtra.Name = "newMarkerExtra";
            this.newMarkerExtra.Size = new System.Drawing.Size(135, 57);
            this.newMarkerExtra.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 41);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(17, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "X:";
            // 
            // newMarkerX
            // 
            this.newMarkerX.Location = new System.Drawing.Point(24, 38);
            this.newMarkerX.Name = "newMarkerX";
            this.newMarkerX.Size = new System.Drawing.Size(60, 20);
            this.newMarkerX.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(96, 41);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(17, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Y:";
            // 
            // newMarkerY
            // 
            this.newMarkerY.Location = new System.Drawing.Point(114, 38);
            this.newMarkerY.Name = "newMarkerY";
            this.newMarkerY.Size = new System.Drawing.Size(65, 20);
            this.newMarkerY.TabIndex = 7;
            // 
            // newMarkerOk
            // 
            this.newMarkerOk.Location = new System.Drawing.Point(105, 178);
            this.newMarkerOk.Name = "newMarkerOk";
            this.newMarkerOk.Size = new System.Drawing.Size(75, 23);
            this.newMarkerOk.TabIndex = 8;
            this.newMarkerOk.Text = "Ok";
            this.newMarkerOk.UseVisualStyleBackColor = true;
            this.newMarkerOk.Click += new System.EventHandler(this.NewMarkerPoint);
            // 
            // newMarkerCancel
            // 
            this.newMarkerCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.newMarkerCancel.Location = new System.Drawing.Point(7, 178);
            this.newMarkerCancel.Name = "newMarkerCancel";
            this.newMarkerCancel.Size = new System.Drawing.Size(75, 23);
            this.newMarkerCancel.TabIndex = 9;
            this.newMarkerCancel.Text = "Cancel";
            this.newMarkerCancel.UseVisualStyleBackColor = true;
            this.newMarkerCancel.Click += new System.EventHandler(this.CancelPoint);
            // 
            // IsPublicCheckbox
            // 
            this.IsPublicCheckbox.AutoSize = true;
            this.IsPublicCheckbox.Location = new System.Drawing.Point(125, 132);
            this.IsPublicCheckbox.Name = "IsPublicCheckbox";
            this.IsPublicCheckbox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.IsPublicCheckbox.Size = new System.Drawing.Size(55, 17);
            this.IsPublicCheckbox.TabIndex = 11;
            this.IsPublicCheckbox.Text = "Public";
            this.IsPublicCheckbox.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(6, 132);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Placed By:";
            // 
            // OwnerLabel
            // 
            this.OwnerLabel.Location = new System.Drawing.Point(6, 153);
            this.OwnerLabel.Name = "OwnerLabel";
            this.OwnerLabel.Size = new System.Drawing.Size(173, 17);
            this.OwnerLabel.TabIndex = 13;
            this.OwnerLabel.Text = "You";
            // 
            // NewMarker
            // 
            this.AcceptButton = this.newMarkerOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.newMarkerCancel;
            this.ClientSize = new System.Drawing.Size(187, 207);
            this.Controls.Add(this.OwnerLabel);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.IsPublicCheckbox);
            this.Controls.Add(this.newMarkerCancel);
            this.Controls.Add(this.newMarkerOk);
            this.Controls.Add(this.newMarkerY);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.newMarkerX);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.newMarkerExtra);
            this.Controls.Add(this.newMarkerName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewMarker";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NewMarker";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox newMarkerName;
        private System.Windows.Forms.TextBox newMarkerExtra;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox newMarkerX;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox newMarkerY;
        private System.Windows.Forms.Button newMarkerOk;
        private System.Windows.Forms.Button newMarkerCancel;
        private System.Windows.Forms.CheckBox IsPublicCheckbox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label OwnerLabel;
    }
}