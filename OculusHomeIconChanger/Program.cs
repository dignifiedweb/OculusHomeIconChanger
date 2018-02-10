using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OculusHomeIconChangerNS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new OculusHomeIconChanger());
            }
            catch (Exception ex)
            {
                MessageBox.Show("OculusHomeIconChanger Encountered an error and will now close: \n\n" + ex.Message + "\n\n" + ex.StackTrace, "ERROR - OculusHomeIconChanger General Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
