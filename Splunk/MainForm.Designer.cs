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
            filTree = new Ephemera.NBagOfUis.FilTree();
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            SuspendLayout();
            // 
            // filTree
            // 
            filTree.Location = new Point(747, 45);
            filTree.Name = "filTree";
            filTree.Size = new Size(437, 459);
            filTree.TabIndex = 0;
            // 
            // tvInfo
            // 
            tvInfo.BorderStyle = BorderStyle.FixedSingle;
            tvInfo.Location = new Point(59, 74);
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new Size(515, 380);
            tvInfo.TabIndex = 1;
            tvInfo.WordWrap = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1196, 608);
            Controls.Add(tvInfo);
            Controls.Add(filTree);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.FilTree filTree;
        private Ephemera.NBagOfUis.TextViewer tvInfo;
    }
}
