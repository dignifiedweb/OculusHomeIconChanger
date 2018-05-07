using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace OculusHomeIconChangerNS
{
    public partial class SplashScreen : Form
    {
        public static BackgroundWorker _bgWorker;

        public SplashScreen()
        {
            InitializeComponent();
        }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            //this.BackColor = Color.LimeGreen;
            //this.TransparencyKey = Color.LimeGreen;

            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += _bgWorker_DoWork;
            _bgWorker.RunWorkerAsync();
        }

        private void _bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                Program.mainForm.Shown += FrmMain_Shown;
                Program.mainForm.Show();
            }));
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
