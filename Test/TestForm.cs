using System;
using System.Windows.Forms;
using Com = Splunk.Common;


namespace Splunk.Test
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();

            // new Process { StartInfo = new ProcessStartInfo(fn) { UseShellExecute = true } }.Start();

            // C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe A1 B2 C3
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void Go_Click(object sender, EventArgs e)
        {
            // ShellStuff ss = new ShellStuff();
            // ss.ExecInNewProcess1();
        }
    }
}
