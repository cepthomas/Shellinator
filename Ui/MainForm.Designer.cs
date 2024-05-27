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
            SuspendLayout();
            // 
            // tvInfo
            // 
            tvInfo.BorderStyle = BorderStyle.FixedSingle;
            tvInfo.Location = new Point(12, 64);
            tvInfo.MaxText = 50000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new Size(1196, 450);
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1220, 552);
            Controls.Add(btnGo);
            Controls.Add(tvInfo);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer tvInfo;
        private Button btnGo;
    }
}
