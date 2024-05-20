using System;
using System.Drawing;
using System.Windows.Forms;

namespace Splunk
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            SuspendLayout();
            // 
            // tvInfo
            // 
            tvInfo.BorderStyle = BorderStyle.FixedSingle;
            tvInfo.Location = new Point(59, 74);
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new Size(800, 500);
            tvInfo.TabIndex = 1;
            tvInfo.WordWrap = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1196, 608);
            Controls.Add(tvInfo);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer tvInfo;
    }
}
