namespace Splunk.Test
{
    partial class TestForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            rtbInfo = new RichTextBox();
            btnGo = new Button();
            SuspendLayout();
            // 
            // rtbInfo
            // 
            rtbInfo.Location = new Point(49, 154);
            rtbInfo.Name = "rtbInfo";
            rtbInfo.Size = new Size(718, 391);
            rtbInfo.TabIndex = 0;
            rtbInfo.Text = "";
            // 
            // btnGo
            // 
            btnGo.Location = new Point(57, 43);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(94, 29);
            btnGo.TabIndex = 1;
            btnGo.Text = "GO!!!";
            btnGo.UseVisualStyleBackColor = true;
            btnGo.Click += btnGo_Click;
            // 
            // TestForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(960, 599);
            Controls.Add(btnGo);
            Controls.Add(rtbInfo);
            Name = "TestForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox rtbInfo;
        private Button btnGo;
    }
}
