namespace Node1.Wf
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.lblCoordinationStatus = new System.Windows.Forms.Label();
            this.tmrRole = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lblCoordinationStatus
            // 
            this.lblCoordinationStatus.AutoSize = true;
            this.lblCoordinationStatus.Location = new System.Drawing.Point(12, 9);
            this.lblCoordinationStatus.Name = "lblCoordinationStatus";
            this.lblCoordinationStatus.Size = new System.Drawing.Size(35, 13);
            this.lblCoordinationStatus.TabIndex = 0;
            this.lblCoordinationStatus.Text = "label1";
            // 
            // tmrRole
            // 
            this.tmrRole.Enabled = true;
            this.tmrRole.Interval = 1000;
            this.tmrRole.Tick += new System.EventHandler(this.tmrRole_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.lblCoordinationStatus);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCoordinationStatus;
        private System.Windows.Forms.Timer tmrRole;
    }
}

