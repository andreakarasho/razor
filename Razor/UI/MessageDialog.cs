using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Assistant
{
    /// <summary>
    ///     Summary description for MessageDialog.
    /// </summary>
    public class MessageDialog : Form
    {
        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private readonly Container components = null;
        private readonly bool m_CanIgnore;
        private readonly string m_Message;
        private readonly string m_Title;
        private TextBox message;
        private Button okay;

        public MessageDialog(string title, string message) : this(title, false, message)
        {
        }

        public MessageDialog(string title, bool ignorable, string message, params object[] msgArgs) : this(title, ignorable, string.Format(message, msgArgs))
        {
        }

        public MessageDialog(string title, bool ignorable, string message)
        {
            m_Title = title;
            m_Message = message;
            m_CanIgnore = ignorable;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.message = new System.Windows.Forms.TextBox();
            this.okay = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // message
            // 
            this.message.Location = new System.Drawing.Point(8, 8);
            this.message.Multiline = true;
            this.message.Name = "message";
            this.message.ReadOnly = true;
            this.message.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.message.Size = new System.Drawing.Size(552, 320);
            this.message.TabIndex = 0;
            // 
            // okay
            // 
            this.okay.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okay.Location = new System.Drawing.Point(8, 334);
            this.okay.Name = "okay";
            this.okay.Size = new System.Drawing.Size(88, 26);
            this.okay.TabIndex = 1;
            this.okay.Text = "&Okay";
            this.okay.Click += new System.EventHandler(this.okay_Click);
            // 
            // MessageDialog
            // 
            this.AcceptButton = this.okay;
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 16);
            this.ClientSize = new System.Drawing.Size(567, 366);
            this.ControlBox = false;
            this.Controls.Add(this.okay);
            this.Controls.Add(this.message);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "MessageDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Message";
            this.Load += new System.EventHandler(this.MessageDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private void MessageDialog_Load(object sender, EventArgs e)
        {
            Text = m_Title;
            message.Text = m_Message;
            message.Select(0, 0);
            BringToFront();

            if (m_CanIgnore)
                okay.Text = "&Ignore";
        }

        private void okay_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}