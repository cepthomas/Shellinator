namespace Splunk.Ui
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
            btnGo = new Button();
            btnInitReg = new Button();
            btnClearReg = new Button();
            btnEdit = new Button();
            SuspendLayout();
            // 
            // tvInfo
            // 
            tvInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tvInfo.BorderStyle = BorderStyle.FixedSingle;
            tvInfo.Location = new Point(12, 64);
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new Size(1196, 469);
            tvInfo.TabIndex = 1;
            tvInfo.WordWrap = true;
            // 
            // btnGo
            // 
            btnGo.Location = new Point(76, 18);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(94, 29);
            btnGo.TabIndex = 2;
            btnGo.Text = "GO!!";
            btnGo.UseVisualStyleBackColor = true;
            // 
            // btnInitReg
            // 
            btnInitReg.Location = new Point(222, 24);
            btnInitReg.Name = "btnInitReg";
            btnInitReg.Size = new Size(94, 29);
            btnInitReg.TabIndex = 3;
            btnInitReg.Text = "Init Reg";
            btnInitReg.UseVisualStyleBackColor = true;
            // 
            // btnClearReg
            // 
            btnClearReg.Location = new Point(343, 24);
            btnClearReg.Name = "btnClearReg";
            btnClearReg.Size = new Size(94, 29);
            btnClearReg.TabIndex = 4;
            btnClearReg.Text = "Clear Reg";
            btnClearReg.UseVisualStyleBackColor = true;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(498, 23);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(94, 29);
            btnEdit.TabIndex = 5;
            btnEdit.Text = "Edit";
            btnEdit.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1220, 545);
            Controls.Add(btnEdit);
            Controls.Add(btnClearReg);
            Controls.Add(btnInitReg);
            Controls.Add(btnGo);
            Controls.Add(tvInfo);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer tvInfo;
        private Button btnGo;
        private Button btnInitReg;
        private Button btnClearReg;
        private Button btnEdit;
    }
}
