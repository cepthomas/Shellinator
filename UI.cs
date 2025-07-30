using Ephemera.NBagOfTricks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shellinator
{
    public partial class UI : Form
    {
        /// <summary>Limit the size.</summary>
        public int MaxText { get; set; } = 10000;

        public UI()
        {
            InitializeComponent();
        }

        /// <summary>A message to display to the user.</summary>
        /// <param name="text">The message.</param>
        /// <param name="color">Specific color to use.</param>
        public void AppendLine(string text, Color? color = null)
        {
            AppendText($"> {text}{Environment.NewLine}", color);
        }

        /// <summary>A message to display to the user. Doesn't add EOL.</summary>
        /// <param name="text">The message.</param>
        /// <param name="color">Specific color to use.</param>
        public void AppendText(string text, Color? color = null)
        {
            this.InvokeIfRequired(_ =>
            {
                // Maybe trim buffer.
                if (MaxText > 0 && OutputText.TextLength > MaxText)
                {
                    OutputText.Select(0, MaxText / 5);
                    OutputText.SelectedText = "";
                }

                OutputText.SelectionBackColor = BackColor; // default

                if (color is not null)
                {
                    OutputText.SelectionBackColor = (Color)color;
                }

                OutputText.AppendText(text);
                OutputText.ScrollToCaret();
            });
        }
    }
}
