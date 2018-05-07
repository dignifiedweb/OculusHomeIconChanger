using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace OculusHomeIconChangerNS
{
    static class Program
    {
        public static OculusHomeIconChanger mainForm;
        public static SplashScreen _splashScreen;

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
                mainForm = new OculusHomeIconChanger();
                _splashScreen = new SplashScreen();
                _splashScreen.Show();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show("OculusHomeIconChanger Encountered an error and will now close: \n\n" + ex.Message + "\n\n" + ex.StackTrace, "ERROR - OculusHomeIconChanger General Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
