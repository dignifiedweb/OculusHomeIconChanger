using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using TsudaKageyu;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Configuration;

namespace OculusHomeIconChangerNS
{
    public partial class OculusHomeIconChanger : Form
    {
        private List<OculusHomeAppListItem> _oculusHomeAppsList;
        private string _oculusHomeLocation;
        private string _oculusHomeManifestLocation;
        private string _oculusHomeImagesLocation;
        private string _oculusHomeManifestSecondaryLocation;
        private string _oculusHomeImagesSecondaryLocation;
        private DataGridViewButtonColumn _dgvButton;
        private Bitmap _selectedIconImageOrig;
        private Bitmap _selectedCoverSquareImageOrig;
        private SteamRootObject _steamJsonRoot;
        private int _visibleRowsCount;
        private bool _attemptListFullAppsOnce;
        private bool _formLoaded;

        public const int ICON_WIDTH = 64;

        #region "Constructor"

        public OculusHomeIconChanger()
        {
            InitializeComponent();

            _attemptListFullAppsOnce = false;
            _formLoaded = false;
            _oculusHomeAppsList = new List<OculusHomeAppListItem>();
            _oculusHomeLocation = GetOculusDirFromRegistry();
            string appDataManifestLocation = ConfigurationManager.AppSettings["manifestlocation"];
            string appdataImagesLocation = ConfigurationManager.AppSettings["imageslocation"];
            string appDataManifestSecondaryLocation = ConfigurationManager.AppSettings["manifestsecondarylocation"];
            string appdataImagesSecondaryLocation = ConfigurationManager.AppSettings["imagessecondarylocation"];


            // Check if the appData config has been changed by user, if so just use those direct paths, nothing else
            if (appDataManifestLocation != "[DefaultManifestLocationReplaceIfYouWantToCustomize]" && appdataImagesLocation != "[DefaultImagesLocationReplaceIfYouWantToCustomize]")
            {
                _oculusHomeManifestLocation = appDataManifestLocation;
                _oculusHomeImagesLocation = appdataImagesLocation;
                _oculusHomeManifestSecondaryLocation = appDataManifestSecondaryLocation;
                _oculusHomeImagesSecondaryLocation = appdataImagesSecondaryLocation;
            }
            else
            {
                _oculusHomeManifestLocation = _oculusHomeLocation + @"\CoreData\Manifests";
                _oculusHomeImagesLocation = _oculusHomeLocation + @"\CoreData\Software\StoreAssets";
                _oculusHomeManifestSecondaryLocation = "";
                _oculusHomeImagesSecondaryLocation = "";
            }

            this.Text += " - WARNING: backup your \"" + _oculusHomeLocation + "\\CoreData\" Folder";
        }

        #endregion

        #region "Main Form Load"
        private void OculusHomeIconChanger_Load(object sender, EventArgs e)
        {
            // Add an up "broken bar" character to the beginning of the label text
            lblSmallLandscapeImage.Text = Convert.ToChar(166) + " " + lblSmallLandscapeImage.Text;
            lblCoverLandscapeImage.Text = Convert.ToChar(166) + " " + lblCoverLandscapeImage.Text;
            lblCoverLandscapeImageLarge.Text = Convert.ToChar(166) + " " + lblCoverLandscapeImageLarge.Text;
            lblIconImage.Text = Convert.ToChar(166) + " " + lblIconImage.Text;
            lblCoverSquareImage.Text = Convert.ToChar(166) + " " + lblCoverSquareImage.Text;

            // Prevent an unhandled exception by asking user to configure the directory in the app.config
            if (!Directory.Exists(_oculusHomeManifestLocation) || !Directory.Exists(_oculusHomeImagesLocation))
            {
                MessageBox.Show("ERROR: could not find the oculus home manifest files\r\n\r\nYou can configure OculusHomeIconChanger.exe.config yourself, there are two directories to manually set and notes in the file.\r\n\r\nOculus Home location found: " + _oculusHomeLocation, "Oculus Home Manifest Files Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // JCarewick - DEBUG - May 6 2018
            // Prevent unhandled exception for secondary manifest location
            if (_oculusHomeManifestSecondaryLocation.Length > 0 && _oculusHomeImagesSecondaryLocation.Length > 0)
            {
                if (!Directory.Exists(_oculusHomeManifestSecondaryLocation) || !Directory.Exists(_oculusHomeImagesSecondaryLocation))
                {
                    MessageBox.Show("ERROR with OculusHomeIconChanger.exe.config - the secondary directories you set don't seem to exist", "Error with OculusHomeIconChanger.exe.config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // JCarewick - DEBUG - May 6 2018
            
            string[] oculusHomeAssetsManifestFiles = Directory.GetFiles(_oculusHomeManifestLocation, "*_*_assets.json");
            if (_oculusHomeManifestSecondaryLocation.Length > 0 && _oculusHomeImagesSecondaryLocation.Length > 0)
            {
                string[] oculusHomeAssetsManifestFilesSecondaryLocation = Directory.GetFiles(_oculusHomeManifestSecondaryLocation, "*_*_assets.json");
                int countOrigManifestArr = oculusHomeAssetsManifestFiles.Length;

                // Add secondary manifest files to the main array
                Array.Resize(ref oculusHomeAssetsManifestFiles, countOrigManifestArr + oculusHomeAssetsManifestFilesSecondaryLocation.Length);
                for (int i = countOrigManifestArr; i < oculusHomeAssetsManifestFiles.Length; i++)
                {
                    // Get index of oculusHomeAssetsManifestFilesSecondaryLocation array
                    int indexOfSecondaryArr = i - countOrigManifestArr;

                    if (indexOfSecondaryArr < oculusHomeAssetsManifestFilesSecondaryLocation.Length)
                    {
                        oculusHomeAssetsManifestFiles[i] = oculusHomeAssetsManifestFilesSecondaryLocation[indexOfSecondaryArr];
                    }
                }
            }

            string siblingFilenameTemp = "";
            Array.Sort(oculusHomeAssetsManifestFiles);
            int tempCount = 0;
            foreach (string filenameFullPath in oculusHomeAssetsManifestFiles)
            {
                tempCount++;

                // Grab Json info into an OculusHomeJsonInfo Object
                // Add third party games only
                try
                {

                    siblingFilenameTemp = filenameFullPath.Replace("_assets", "");

                    // JCarewick - DEBUG - May 6 2018
                    if (!File.Exists(siblingFilenameTemp))
                    {
                        siblingFilenameTemp = _oculusHomeManifestLocation + "\\" + Path.GetFileName(siblingFilenameTemp);
                    }

                    if (File.Exists(siblingFilenameTemp))
                    {
                        OculusHomeApp_AssetsJson oculusHomeApp_AssetsJson = GetOculusHomeApp_AssetsJsonFromPath(filenameFullPath);

                        if (oculusHomeApp_AssetsJson != null)
                        {
                            // Only edit third party icons
                            if (oculusHomeApp_AssetsJson.thirdParty)
                            {
                                // JCarewick - DEBUG - May 6 2018 // Why was this here?
                                // siblingFilenameTemp = filenameFullPath.Replace("_assets", "");

                                if (File.Exists(siblingFilenameTemp))
                                {
                                    OculusHomeAppJson oculusHomeAppJson = GetOculusHomeAppJsonFromPath(siblingFilenameTemp);

                                    OculusHomeAppListItem app = new OculusHomeAppListItem();
                                    app.canonicalName = oculusHomeAppJson.canonicalName;
                                    app.displayName = oculusHomeAppJson.displayName;
                                    app.displayNameOrig = (string)oculusHomeAppJson.displayName.Clone();
                                    app.launchFile = oculusHomeAppJson.launchFile;
                                    app.fileModifiedDateTime = File.GetLastWriteTime(siblingFilenameTemp);

                                    string imageLoadPath = _oculusHomeImagesLocation + "\\" + app.canonicalName + "_assets\\";

                                    // JCarewick - DEBUG - May 6 2018
                                    if (filenameFullPath.Contains(_oculusHomeManifestSecondaryLocation) && _oculusHomeManifestSecondaryLocation.Length > 0)
                                    {
                                        imageLoadPath = _oculusHomeImagesSecondaryLocation + "\\" + app.canonicalName + "_assets\\";
                                    }

                                    string cover_landscape_image = imageLoadPath + "cover_landscape_image.jpg";
                                    string cover_landscape_image_large = imageLoadPath + "cover_landscape_image_large.png";
                                    string cover_square_image = imageLoadPath + "cover_square_image.jpg";
                                    string icon_image = imageLoadPath + "icon_image.jpg";
                                    string small_landscape_image = imageLoadPath + "small_landscape_image.jpg";

                                    app.icon = GetBitmapFromFile(icon_image, true);
                                    app.cover_landscape_image = GetBitmapFromFile(cover_landscape_image);
                                    app.cover_square_image = GetBitmapFromFile(cover_square_image);
                                    app.icon_image = GetBitmapFromFile(icon_image);
                                    app.small_landscape_image = GetBitmapFromFile(small_landscape_image);
                                    app.cover_landscape_image_large = GetBitmapFromFile(cover_landscape_image_large);

                                    _oculusHomeAppsList.Add(app);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(filenameFullPath + "\r\n\r\n" + ex.Message, "json parse exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if (_oculusHomeAppsList.Count > 0)
            {
                try
                {
                    GetSteamAppIdJson();

                    // JCarewick - DEBUG - May 6 2018
                    //List<OculusHomeAppListItem> steamapps = (from apps in _oculusHomeAppsList
                    //                                         where (apps.canonicalName.ToLower().Contains("steamapps"))
                    //                                         select apps).ToList();
                    List<OculusHomeAppListItem> steamapps = (from apps in _oculusHomeAppsList
                                                             where (apps.canonicalName.ToLower().Contains("steamapps") || apps.displayName.ToLower().Contains("vr"))
                                                             select apps).ToList();
                    foreach (OculusHomeAppListItem steamapp in steamapps)
                    {
                        // JCarewick - DEBUG - May 6 2018
                        string steamId = GetSteamAppID(steamapp.displayName);
                        if (steamId != null)
                        {
                            steamapp.steamID = GetSteamAppID(steamapp.displayName);
                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error grabbing steam api json", "Error grabbing steam api json", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Set datagridview source and sort it
                radSortByDateDesc.Select();

                _dgvButton = new DataGridViewButtonColumn();
                _dgvButton.HeaderText = "Explore";
                _dgvButton.Name = "Explore";
                _dgvButton.Text = "Explore to Exe";
                _dgvButton.UseColumnTextForButtonValue = true;
                dgvAppList.Columns.Add(_dgvButton);
                dgvAppList.Columns["Explore"].Width = 120;
            }

            // Add a key down listener so that shortcuts can be created for the app
            this.KeyPreview = true;
            this.KeyDown += OculusHomeIconChanger_KeyDown;

            // Set tooltip on oculus service restart button
            toolTipRestartOculusButton.SetToolTip(btnRestartOculusService, "Required to update icons in Oculus Home");
        }

        #endregion

        #region "Methods"

        private string GetOculusDirFromRegistry()
        {
            string oculusDirectory = @"C:\Program Files (x86)\Oculus";

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey registryKey = hklm.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\\Uninstall\\Oculus"))
                {
                    if (registryKey != null)
                    {
                        oculusDirectory = (string)registryKey.GetValue("InstallLocation");
                    }
                }
            }

            return oculusDirectory;
        }

        private Bitmap GetBitmapFromWebsite(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                return new Bitmap(responseStream);
            }
            catch (WebException ex)
            {
                throw new WebException(ex.Message);
            }
        }

        private Bitmap GetBitmapFromFile(string path, bool isIcon = false)
        {
            if (File.Exists(path))
            {
                if (path.Contains(".exe"))
                {
                    return GetBitmapFromExecutable(path);
                }
                else
                {

                    // Open the image as a stream, so it doens't keep a file handle lock on the file
                    using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open))
                    {
                        Bitmap bmp = new Bitmap(fs);

                        if (isIcon)
                        {
                            return new Bitmap((Bitmap)bmp.Clone(), new Size(ICON_WIDTH, ICON_WIDTH));
                        }

                        return bmp;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Extract a bitmap object from an executable using the Library by Tsuda Kageyu
        /// http://www.codeproject.com/Articles/26824/Extract-icons-from-EXE-or-DLL-files
        /// Grab the largest available graphic
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>Bitmap grahic object, convertable to other formats</returns>
        private Bitmap GetBitmapFromExecutable(String filename)
        {
            Bitmap tmpBitmap = null;
            Icon icon = null;
            Icon[]
            splitIcons = null;

            try
            {
                var extractor = new IconExtractor(filename);
                icon = extractor.GetIcon(0);
                splitIcons = IconUtil.Split(icon);
                int lastIconWidth = 0;

                foreach (var i in splitIcons)
                {
                    var size = i.Size;
                    var bits = IconUtil.GetBitCount(i);
                    if (size.Width > lastIconWidth)
                    {
                        tmpBitmap = IconUtil.ToBitmap(i);
                    }
                    i.Dispose();
                }

                return tmpBitmap;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error with extracting icon", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return tmpBitmap;
            }
        }

        private OculusHomeApp_AssetsJson GetOculusHomeApp_AssetsJsonFromPath(string path)
        {
            string readJsonAsText = File.ReadAllText(path);
            JsonTextReader jsonReader = new JsonTextReader(new StringReader(readJsonAsText));
            JsonSerializer jsonSerialize = new JsonSerializer();
            return (OculusHomeApp_AssetsJson)jsonSerialize.Deserialize(jsonReader, typeof(OculusHomeApp_AssetsJson));
        }

        private OculusHomeAppJson GetOculusHomeAppJsonFromPath(string path)
        {
            string readJsonAsText = File.ReadAllText(path);
            JsonTextReader jsonReader = new JsonTextReader(new StringReader(readJsonAsText));
            JsonSerializer jsonSerialize = new JsonSerializer();
            return (OculusHomeAppJson)jsonSerialize.Deserialize(jsonReader, typeof(OculusHomeAppJson));
        }

        private void GetSteamAppIdJson()
        {
            string steamApiJson = "";
            HttpWebRequest request = WebRequest.Create("https://api.steampowered.com/ISteamApps/GetAppList/v2/") as HttpWebRequest;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            WebHeaderCollection header = response.Headers;
            var encoding = ASCIIEncoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                steamApiJson = reader.ReadToEnd();
            }

            // https://stackoverflow.com/questions/41418245/how-do-i-convert-a-json-string-for-better-use-in-c-sharp
            JsonTextReader jsonReader = new JsonTextReader(new StringReader(steamApiJson));
            JsonSerializer jsonSerialize = new JsonSerializer();
            _steamJsonRoot = (SteamRootObject)jsonSerialize.Deserialize(jsonReader, typeof(SteamRootObject));

            // Fix for apps that are like "Eleven Table Tennis VR" where the steam name is "Eleven: Table Tennis VR"
            List<SteamApp> colonTest = (from apps in _steamJsonRoot.applist.apps
                                        where apps.name.ToLower().Contains(":")
                                        select apps).ToList();

            foreach (SteamApp steamApp in colonTest)
            {
                steamApp.name = steamApp.name.Replace(":", "");
            }
        }

        private bool SteamGameHasVRSupport(string steamId)
        {
            string steamAppStoreInfo = "";

            if (steamId == "400940")
            {

            }

            try
            {

                HttpWebRequest request = WebRequest.Create("http://store.steampowered.com/api/appdetails/?appids=" + steamId) as HttpWebRequest;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                WebHeaderCollection header = response.Headers;
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    steamAppStoreInfo = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                steamAppStoreInfo = "";
            }

            if (steamAppStoreInfo.ToLower().Contains("vr"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string GetSteamAppID(string appname)
        {
            List<SteamApp> steamAppList = (from apps in _steamJsonRoot.applist.apps
                                           where apps.name.ToLower().Equals(appname.ToLower())
                                           select apps).ToList();

            // JCarewick - DEBUG - May 6 2018  -delete me
            if (appname.Contains("Titanic"))
            {

            }

            // If App not found, try stripping out a " VR" at the end of title
            if (steamAppList.Count < 1)
            {
                // Fix for "Space Pirate Trainer VR"
                if (appname.Length > 3)
                {
                    string checkForVREnding = appname.Substring(appname.Length - 3).Trim().ToLower();
                    if (checkForVREnding.Contains("vr"))
                    {
                        appname = appname.ToLower().Replace(checkForVREnding, "").Trim();
                    }
                }

                // Fix for "VanishingRealms" if an app doesn't contain a space, convert camel casing to spaces
                if (!appname.Contains(" "))
                {
                    appname = System.Text.RegularExpressions.Regex.Replace(appname, "(\\B[A-Z])", " $1");
                }

                steamAppList = (from apps in _steamJsonRoot.applist.apps
                                where apps.name.ToLower().Contains(appname.ToLower())
                                select apps).ToList();
            }

            if (steamAppList.Count > 0)
            {
                return steamAppList[0].appid.ToString();
            }
            else
            {
                return null;
            }
        }

        // https://codereview.stackexchange.com/questions/157667/getting-the-dominant-rgb-color-of-a-bitmap
        public System.Drawing.Color GetDominantColor(Bitmap bmp)
        {
            if (bmp == null)
            {
                throw new ArgumentNullException("bmp");
            }

            BitmapData srcData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(srcData.PixelFormat) / 8;

            int stride = srcData.Stride;

            IntPtr scan0 = srcData.Scan0;

            long[] totals = new long[] { 0, 0, 0 };

            int width = bmp.Width * bytesPerPixel;
            int height = bmp.Height;

            unsafe
            {
                byte* p = (byte*)(void*)scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += bytesPerPixel)
                    {
                        totals[0] += p[x + 0];
                        totals[1] += p[x + 1];
                        totals[2] += p[x + 2];
                    }

                    p += stride;
                }
            }

            long pixelCount = bmp.Width * height;

            int avgB = Convert.ToInt32(totals[0] / pixelCount);
            int avgG = Convert.ToInt32(totals[1] / pixelCount);
            int avgR = Convert.ToInt32(totals[2] / pixelCount);

            bmp.UnlockBits(srcData);

            return Color.FromArgb(avgR, avgG, avgB);

        }

        private void GrabImageFromPictureBoxAndSave(OculusHomeAppListItem selectedApp)
        {
            if (selectedApp.photosChanged)
            {
                // Grab a copy (clone) of the bitmaps for icon_image and cover_square image to give user ability to adjust before switching to next app to edit
                _selectedIconImageOrig = new Bitmap((Bitmap)selectedApp.icon_image.Clone());
                _selectedCoverSquareImageOrig = new Bitmap((Bitmap)selectedApp.cover_square_image.Clone());

                // Save the current background color and image (how picturebox looks) to the savable selectedApp object
                Bitmap icon_image_pic = new Bitmap(pic_icon_image.Width, pic_icon_image.Height);
                pic_icon_image.DrawToBitmap(icon_image_pic, pic_icon_image.ClientRectangle);
                selectedApp.icon_image = icon_image_pic;

                Bitmap cover_square_image_pic = new Bitmap(pic_cover_square_image.Width, pic_cover_square_image.Height);
                pic_cover_square_image.DrawToBitmap(cover_square_image_pic, pic_cover_square_image.ClientRectangle);
                selectedApp.cover_square_image = cover_square_image_pic;

                // Set the icon to the new image
                selectedApp.icon = new Bitmap((Bitmap)selectedApp.cover_square_image.Clone(), new Size(ICON_WIDTH, ICON_WIDTH));

                // For compatability with image formats, convert the cover_landscape_image and small_landscape image as well
                Bitmap cover_landscape_image = new Bitmap(pic_cover_landscape_image.Width, pic_cover_landscape_image.Height);
                pic_cover_landscape_image.DrawToBitmap(cover_landscape_image, pic_cover_landscape_image.ClientRectangle);
                selectedApp.cover_landscape_image = cover_landscape_image;

                Bitmap small_landscape_image = new Bitmap(pic_small_landscape_image.Width, pic_small_landscape_image.Height);
                pic_small_landscape_image.DrawToBitmap(small_landscape_image, pic_small_landscape_image.ClientRectangle);
                selectedApp.small_landscape_image = small_landscape_image;

                Bitmap cover_landscape_image_large = new Bitmap(pic_cover_landscape_image_large.Width, pic_cover_landscape_image_large.Height);
                pic_cover_landscape_image_large.DrawToBitmap(cover_landscape_image_large, pic_cover_landscape_image_large.ClientRectangle);
                selectedApp.cover_landscape_image_large = cover_landscape_image_large;

                RefreshDataGridViewMain();
            }
        }

        private void StartStopOculusService(string startStop)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "net";
            proc.StartInfo.Arguments = startStop + " \"Oculus VR Runtime Service\"";
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();

            // Wait until the process has finished
            while (proc.HasExited == false)
            {
                Thread.Sleep(32);
            }

        }

        #endregion

        #region "Event Handlers"

        #region "Keyboard Event Handlers"
        // Todo: Implement ctrl+s for save all
        private void OculusHomeIconChanger_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)       // Ctrl-S Save
            {
                //DialogResult diagResult = MessageBox.Show("Do you want to save?", "Do you want to save?", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                //if (diagResult.Equals(DialogResult.Yes))
                //{
                //    SaveChanges();
                //}
                MessageBox.Show("Not implemented yet", "ERROR: need to implement this yet", MessageBoxButtons.OK, MessageBoxIcon.Error);

                e.SuppressKeyPress = true;  // Stops bing! Also sets handled which stop event bubbling
            }
        }

        private void dgvAppList_KeyUpDown(object sender, KeyEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
                return;
            if (dgv.CurrentRow.Selected)
            {
                SelectAppInDataGrid();
            }
        }
        #endregion

        #region "Radio Button Event Handlers"

        private void radSortByDateDesc_CheckedChanged(object sender, EventArgs e)
        {
            _oculusHomeAppsList = _oculusHomeAppsList.OrderByDescending(i => i.fileModifiedDateTime).ToList();

            // Refresh datagrid and refresh app selected
            RefreshDataGridViewMain();
            ApplySelectedFilter();

            // JCarewick - DEBUG - May 6 2018
            if (_formLoaded)
            {
                SelectAppInDataGrid();
            }

            _formLoaded = true;
        }

        private void radShowAllApps_CheckedChanged(object sender, EventArgs e)
        {
            // JCarewick - DEBUG - May 4 2018
            if (radShowAllApps.Checked)
            {

                dgvAppList.CurrentCell = null;
                _visibleRowsCount = 0;
                foreach (DataGridViewRow row in dgvAppList.Rows)
                {
                    row.Visible = true;
                    _visibleRowsCount++;
                }

                // Select first dispalyed app and reset pictures to that
                // JCarewick - DEBUG - May 4 2018
                if (dgvAppList.DataSource != null)
                {
                    dgvAppList.FirstDisplayedCell.Selected = true;
                    DataGridViewRow selectedRow = dgvAppList.Rows[dgvAppList.FirstDisplayedCell.RowIndex];
                    OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                    SetGUIItemsToSelectedApp(selectedApp);
                }

                // JCarewick - DEBUG - May 4 2018
                SelectAppInDataGrid();
            }
        }

        private void radSortByDateAsc_CheckedChanged(object sender, EventArgs e)
        {
            _oculusHomeAppsList = _oculusHomeAppsList.OrderBy(i => i.fileModifiedDateTime).ToList();

            // Refresh datagrid and refresh app selected
            RefreshDataGridViewMain();
            SelectAppInDataGrid();
            ApplySelectedFilter();
        }

        private void radSortByName_CheckedChanged(object sender, EventArgs e)
        {
            _oculusHomeAppsList = _oculusHomeAppsList.OrderBy(i => i.displayName).ToList();

            // Refresh datagrid and refresh app selected
            RefreshDataGridViewMain();
            SelectAppInDataGrid();
            ApplySelectedFilter();
        }

        private void radShowOnlySteamApps_CheckedChanged(object sender, EventArgs e)
        {
            // JCarewick - DEBUG - May 4 2018
            if (radShowOnlySteamApps.Checked)
            {
                dgvAppList.CurrentCell = null;
                _visibleRowsCount = 0;
                foreach (DataGridViewRow row in dgvAppList.Rows)
                {
                    // JCarewick - DEBUG - May 6 2018
                    //if (row.Cells["canonicalName"].Value.ToString().ToLower().Contains("steamapps"))
                    //{
                        //List<OculusHomeAppListItem> steamApp = (from apps in _oculusHomeAppsList
                        //                                        where apps.canonicalName.ToLower().Contains("steamapps")
                        //                                        && apps.steamID != null
                        //                                        && apps.canonicalName == row.Cells["canonicalName"].Value.ToString()
                        //                                        select apps).ToList();

                        List<OculusHomeAppListItem> steamApp = (from apps in _oculusHomeAppsList
                                                                where (apps.steamID != null
                                                                    || apps.displayName.ToLower().Contains("vr"))
                                                                    && apps.canonicalName == row.Cells["canonicalName"].Value.ToString()
                                                                select apps).ToList();

                        if (steamApp.Count == 1)
                        {
                            if (steamApp[0].steamID == null)
                            {
                                String steamIdTest = GetSteamAppID(steamApp[0].displayName);
                                steamApp[0].steamID = steamIdTest;
                            }

                            if (steamApp[0].steamID != null)
                            {
                                if (SteamGameHasVRSupport(steamApp[0].steamID))
                                {
                                    row.Visible = true;
                                    _visibleRowsCount++;
                                }
                                else
                                {
                                    row.Visible = false;
                                }
                            }
                            else
                            {
                                row.Visible = false;
                            }
                        }
                        else
                        {
                            row.Visible = false;
                        }
                    //}
                    //else
                    //{
                    //    row.Visible = false;
                    //}
                }

                // JCarewick - DEBUG - May 4 2018
                if (dgvAppList.DataSource != null && dgvAppList.FirstDisplayedCell != null)
                {
                    // Select first dispalyed app and reset pictures to that
                    dgvAppList.FirstDisplayedCell.Selected = true;
                    DataGridViewRow selectedRow = dgvAppList.Rows[dgvAppList.FirstDisplayedCell.RowIndex];
                    OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                    SetGUIItemsToSelectedApp(selectedApp);
                }

                // JCarewick - DEBUG - May 4 2018
                SelectAppInDataGrid();

            }
        }

        private void rad_icon_image_stretched_CheckedChanged(object sender, EventArgs e)
        {
            pic_icon_image.SizeMode = PictureBoxSizeMode.StretchImage;
            pic_cover_square_image.SizeMode = PictureBoxSizeMode.StretchImage;

            // Save the picturebox to the selected app
            DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
            OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
            GrabImageFromPictureBoxAndSave(selectedApp);
        }

        private void rad_icon_image_center_CheckedChanged(object sender, EventArgs e)
        {
            pic_icon_image.SizeMode = PictureBoxSizeMode.CenterImage;
            pic_cover_square_image.SizeMode = PictureBoxSizeMode.CenterImage;

            // Save the picturebox to the selected app
            DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
            OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
            GrabImageFromPictureBoxAndSave(selectedApp);
        }

        private void rad_icon_image_zoom_CheckedChanged(object sender, EventArgs e)
        {
            pic_icon_image.SizeMode = PictureBoxSizeMode.Zoom;
            pic_cover_square_image.SizeMode = PictureBoxSizeMode.Zoom;

            // Save the picturebox to the selected app
            DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
            OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
            GrabImageFromPictureBoxAndSave(selectedApp);
        }

        #endregion

        #region "Button Event Handlers"
        private void btnChooseBgColor_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = pic_icon_image.BackColor;
            DialogResult diagResult = colorDialog1.ShowDialog();
            if (diagResult.Equals(DialogResult.OK))
            {
                pic_cover_square_image.BackColor = colorDialog1.Color;
                pic_icon_image.BackColor = colorDialog1.Color;

                DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
                OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                GrabImageFromPictureBoxAndSave(selectedApp);
            }
        }


        private void btnGetSteamBanners_Click(object sender, EventArgs e)
        {
            // Load banners for Steam App ID if found
            if (dgvAppList.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
                OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                string steamIdForApp = "";

                if (txtSteamIDFound.Text.Length > 0)
                {
                    steamIdForApp = txtSteamIDFound.Text;
                }
                else
                {
                    steamIdForApp = GetSteamAppID(selectedApp.displayName);
                    txtSteamIDFound.Text = steamIdForApp;
                    selectedApp.steamID = steamIdForApp;
                }

                if (steamIdForApp == null)
                {
                    MessageBox.Show("Steam ID not found for " + selectedApp.displayName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnSave.Enabled = true;

                try
                {
                    // JCarewick - DEBUG - May 7 2018
                    string steamCDNLocation, iconImageToUse, coverImageToUse, coverLandscapeToUse, smallLandscapeToUse, coverImageLargeToUse;
                    if (ConfigurationManager.AppSettings["steamCDNLocation"].ToString().Length > 0)
                    {
                        steamCDNLocation = ConfigurationManager.AppSettings["steamCDNLocation"].ToString();
                        iconImageToUse = ConfigurationManager.AppSettings["iconImageToUse"].ToString();
                        coverImageToUse = ConfigurationManager.AppSettings["coverImageToUse"].ToString();
                        coverLandscapeToUse = ConfigurationManager.AppSettings["coverLandscapeToUse"].ToString();
                        smallLandscapeToUse = ConfigurationManager.AppSettings["smallLandscapeToUse"].ToString();
                        coverImageLargeToUse = ConfigurationManager.AppSettings["coverLandscapeImageLarge"].ToString();
                    }
                    else
                    {
                        steamCDNLocation = @"http://cdn.edgecast.steamstatic.com/steam/apps/";
                        iconImageToUse = "header.jpg";
                        coverImageToUse = "header.jpg";
                        coverLandscapeToUse = "capsule_616x353.jpg";
                        smallLandscapeToUse = "capsule_467x181.jpg";
                        coverImageLargeToUse = "capsule_616x353.jpg";
                    }

                    string iconImageFullPath = steamCDNLocation + steamIdForApp + "/" + iconImageToUse;
                    string coverSquareFullPath = steamCDNLocation + steamIdForApp + "/" + coverImageToUse;
                    string coverLandscapeFullPath = steamCDNLocation + steamIdForApp + "/" + coverLandscapeToUse;
                    string smallLandscapeFullPath = steamCDNLocation + steamIdForApp + "/" + smallLandscapeToUse;
                    string coverLandScapeLargeFullPath = steamCDNLocation + steamIdForApp + "/" + coverImageLargeToUse;

                    // Not used:
                    //string steamHeader292x136 = steamCDNLocation + steamIdForApp + "/header_292x136.jpg";

                    selectedApp.icon_image = new Bitmap(GetBitmapFromWebsite(iconImageFullPath), new Size(245, 115)); // copy @ 245px X 115 (53.33333% of header)
                    selectedApp.cover_square_image = GetBitmapFromWebsite(coverSquareFullPath); // steamHeader 192x192
                    selectedApp.cover_landscape_image = GetBitmapFromWebsite(coverLandscapeFullPath);
                    selectedApp.cover_landscape_image_large = GetBitmapFromWebsite(coverLandScapeLargeFullPath);
                    selectedApp.small_landscape_image = GetBitmapFromWebsite(smallLandscapeFullPath);
                    selectedApp.icon = new Bitmap((Bitmap)selectedApp.small_landscape_image.Clone(), new Size(ICON_WIDTH, ICON_WIDTH));

                    SetGUIItemsToSelectedApp(selectedApp);
                    RefreshDataGridViewMain();

                    // Now, save a copy of the cover_square_image and icon_image to be used to edit with the picture box
                    // the state of the picturebox is then saved to the selectedApp

                    // Get Dominant color of steam image and set the picturebox background to that color
                    Color dominantColor = GetDominantColor(selectedApp.cover_square_image);
                    pic_cover_square_image.BackColor = dominantColor;
                    pic_icon_image.BackColor = dominantColor;

                    // Set photo column to changed to be able to save only photos changed
                    selectedApp.photosChanged = true;

                    // Save the picturebox to the selected app
                    GrabImageFromPictureBoxAndSave(selectedApp);

                }
                catch (WebException ex)
                {
                    MessageBox.Show("No banner found for steam id: " + steamIdForApp, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Error Finding Steam ID\nTry selecting \"all third party apps\" radio button", "Error Finding Steam ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnChangeImageButtons(object sender, EventArgs e)
        {
            btnSave.Enabled = true;
            Button btnSender = (Button)sender; // btn_pic_cover_square_image
            string picBoxName = btnSender.Name.Replace("btn_", ""); //pic_cover_square_image
            Control[] controls = this.Controls.Find(picBoxName, true);
            if (controls.Length > 0)
            {
                if (controls[0] is PictureBox)
                {
                    BrowseForImageForPictureBox((PictureBox)controls[0]);
                }
            }
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            // Get apps that have images that need to be saved
            List<OculusHomeAppListItem> appsWithImagesToSave = (from apps in _oculusHomeAppsList
                                                                where apps.photosChanged == true
                                                                select apps).ToList();

            foreach (OculusHomeAppListItem app in appsWithImagesToSave)
            {
                bool photoChangeSuccess = false;
                try
                {
                    string imgPath = _oculusHomeImagesLocation + "\\" + app.canonicalName + "_assets";

                    // JCarewick - DEBUG - May 6 2018
                    // Accomodate the secondary manifest and image directory
                    if (!Directory.Exists(imgPath))
                    {
                        imgPath = _oculusHomeImagesSecondaryLocation + "\\" + app.canonicalName + "_assets";
                    }

                    if (Directory.Exists(imgPath))
                    {
                        app.icon_image.Save(imgPath + "\\icon_image.jpg", ImageFormat.Jpeg);
                        app.cover_landscape_image.Save(imgPath + "\\cover_landscape_image.jpg", ImageFormat.Jpeg);
                        app.cover_square_image.Save(imgPath + "\\cover_square_image.jpg", ImageFormat.Jpeg);
                        app.small_landscape_image.Save(imgPath + "\\small_landscape_image.jpg", ImageFormat.Jpeg);
                        app.cover_landscape_image_large.Save(imgPath + "\\cover_landscape_image_large.png", ImageFormat.Png);

                        photoChangeSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR: " + ex.Message);
                }


                if (photoChangeSuccess)
                {
                    app.photosChanged = false;
                }
            }

            // Get apps that have names that need to be saved
            List<OculusHomeAppListItem> appsWithNameChanges = (from apps in _oculusHomeAppsList
                                                                where apps.nameChanged == true
                                                                select apps).ToList();

            foreach (OculusHomeAppListItem app in appsWithNameChanges)
            {
                bool nameChangeSuccess = false;
                try
                {
                    string appJsonFileToEdit = _oculusHomeManifestLocation + "\\" + app.canonicalName + ".json";
                    if (File.Exists(appJsonFileToEdit))
                    {
                        // Didn't have time to do this proper, just a placeholder find replace in textfile, then re-save the json file
                        string readJsonAsText = File.ReadAllText(appJsonFileToEdit);
                        
                        // JCarewick - DEBUG - May 6 2018
                        //string displayNameOrig = "\"displayName\":\"" + app.displayNameOrig + "\"";
                        //string displayNameNew = "\"displayName\":\"" + app.displayName + "\"";
                        string displayNameOrig = "\"" + app.displayNameOrig + "\",";
                        string displayNameNew = "\"" + app.displayName + "\",";
                        readJsonAsText = readJsonAsText.Replace(displayNameOrig, displayNameNew);
                        File.WriteAllText(appJsonFileToEdit, readJsonAsText);
                        nameChangeSuccess = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR: " + ex.Message);
                }


                if (nameChangeSuccess)
                {
                    app.nameChanged = false;
                }
            }

            MessageBox.Show("Save Changes Successful!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnSave.Enabled = false;

        }

        private void btnRestartOculusService_Click(object sender, EventArgs e)
        {
            string origButtonText = btnRestartOculusService.Text;
            btnRestartOculusService.Enabled = false;
            btnRestartOculusService.Text = "(restarting oculus...)";
            StartStopOculusService("stop");
            StartStopOculusService("start");
            btnRestartOculusService.Text = origButtonText;
            btnRestartOculusService.Enabled = true;
        }

        #endregion

        private void dgvAppList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridView dgv = sender as DataGridView;
                if (dgv == null)
                    return;
                if (dgv.CurrentRow.Selected)
                {
                    SelectAppInDataGrid();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error clicking on datagridview" + ex.Message);
            }
        }

        private void dgvAppList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //  Handle the delete row button
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
                OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;

                if (File.Exists(selectedApp.launchFile))
                {
                    string pathToBrowse = Path.GetDirectoryName(selectedApp.launchFile);
                    System.Diagnostics.Process.Start(pathToBrowse);
                }
                else
                {
                    MessageBox.Show("Cannot browse to:\r\n" + selectedApp.launchFile + "\r\n\r\n(permissions may be the issue)", selectedApp.displayName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }

            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            dgvAppList.CurrentCell = null;
            bool rowFound = false;
            foreach (DataGridViewRow row in dgvAppList.Rows)
            {
                if (row.Cells["displayName"].Value.ToString().ToLower().Contains(txtSearch.Text.ToLower()))
                {
                    row.Visible = true;
                    rowFound = true;
                }
                else
                {
                    row.Visible = false;
                    rowFound = false;
                }
            }

            if (rowFound)
            {
                // Select first dispalyed app and reset pictures to that
                dgvAppList.FirstDisplayedCell.Selected = true;
                DataGridViewRow selectedRow = dgvAppList.Rows[dgvAppList.FirstDisplayedCell.RowIndex];
                OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                SetGUIItemsToSelectedApp(selectedApp);
            }

            if (txtSearch.Text.Length == 0)
            {
                ApplySelectedFilter();
            }
        }

        private void txtAppName_TextChanged(object sender, EventArgs e)
        {
            if (txtAppName.Text.Length > 0)
            {
                DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
                if (selectedRow != null)
                {
                    OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                    if (selectedApp.displayName != txtAppName.Text)
                    {
                        btnSave.Enabled = true;
                        selectedApp.displayName = txtAppName.Text;
                        selectedApp.nameChanged = true;
                    }
                }
            }
        }

        #endregion

        #region "User Interface Code"

        private void RefreshDataGridViewMain()
        {
            // Clear the data grid view and load data
            dgvAppList.DataSource = _oculusHomeAppsList;
            dgvAppList.Refresh();

            dgvAppList.Columns["icon"].Width = ICON_WIDTH;
            dgvAppList.Columns["canonicalName"].Visible = false;

            dgvAppList.Columns["displayName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Auto-resize the rows to fit the full icon
            for (int i = 0; i < dgvAppList.Rows.Count; i++)
            {
                dgvAppList.AutoResizeRow(i);
            }
        }

        private void dgvAppList_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            SelectAppInDataGrid();
        }

        /// <summary>
        /// Action to take when App is selected in DataGridView
        /// </summary>
        private void SelectAppInDataGrid()
        {
            if ((dgvAppList.Rows.Count > 0 && _visibleRowsCount > 0) || (txtSearch.Text.Length > 0 && dgvAppList.Rows.Count > 0))
            {
                DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
                OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;
                SetGUIItemsToSelectedApp(selectedApp);
            }
            // If original load of steam apps is blank, try loading full list
            else if (radShowOnlySteamApps.Checked && _attemptListFullAppsOnce == false)
            {
                radShowAllApps.Select();
                _attemptListFullAppsOnce = true;
            }
            else
            {
                ClearEditAppSelectedArea();
            }
        }

        /// <summary>
        /// Clear all textboxes, etc in the "Edit selected app" area
        /// Called when no apps can be selected
        /// </summary>
        private void ClearEditAppSelectedArea()
        {
            // JCarewick - DEBUG - May 4 2018

            txtAppName.Text = "";
            txtSteamIDFound.Text = "";
            pic_small_landscape_image.Image = null;
            pic_icon_image.Image = null;
            pic_cover_square_image.Image = null;
            pic_cover_landscape_image.Image = null;
            pic_cover_landscape_image_large.Image = null;

            if (radShowOnlySteamApps.Checked)
            {
                MessageBox.Show("No games found with the name 'steamapps' in manifest filename, which is normally where steam games are located (in a steamapps folder somewhere)", "No 'steamapps' found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("No third party apps found, the manifest folder location may be configured wrong. Try configuring the OculusHomeChanger.exe.config.", "No third party apps found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetGUIItemsToSelectedApp(OculusHomeAppListItem selectedApp)
        {
            // Set the name
            txtAppName.Text = selectedApp.displayName;
            txtAppName.Refresh();
            txtSteamIDFound.Text = selectedApp.steamID;

            // Set the image
            pic_cover_landscape_image.Image = selectedApp.cover_landscape_image;
            pic_cover_square_image.Image = selectedApp.cover_square_image;
            pic_small_landscape_image.Image = selectedApp.small_landscape_image;
            pic_icon_image.Image = selectedApp.icon_image;
            pic_cover_landscape_image_large.Image = selectedApp.cover_landscape_image_large;
        }

        private void ApplySelectedFilter()
        {
            if (radShowOnlySteamApps.Checked)
            {
                radShowOnlySteamApps_CheckedChanged(null, null);

            }
            else if (radShowAllApps.Checked)
            {
                radShowAllApps_CheckedChanged(null, null);
            }
        }

        private void BrowseForImageForPictureBox(PictureBox picBox)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Filter = "Image Files (*.png, *.jpg, *.bmp, *.exe)|*.png;*.jpg;*.bmp;*.exe";

            DataGridViewRow selectedRow = dgvAppList.SelectedRows[0];
            OculusHomeAppListItem selectedApp = (OculusHomeAppListItem)selectedRow.DataBoundItem;

            // Accomodate the secondary manifest and image directory
            string imgPath = _oculusHomeImagesLocation + "\\" + selectedApp.canonicalName + "_assets";
            if (!Directory.Exists(imgPath))
            {
                imgPath = _oculusHomeImagesSecondaryLocation + "\\" + selectedApp.canonicalName + "_assets";
            }

            // If dir exists, browse to where the images are located
            if (Directory.Exists(imgPath))
            {
                diag.InitialDirectory = imgPath;
            }
            DialogResult result = diag.ShowDialog();

            if (result.Equals(DialogResult.OK))
            {
                picBox.Image = GetBitmapFromFile(diag.FileName);
                selectedApp.photosChanged = true;

                switch (picBox.Name)
                {
                    case "pic_cover_square_image":
                        selectedApp.cover_square_image = (Bitmap)picBox.Image;
                        break;
                    case "pic_icon_image":
                        selectedApp.icon_image = (Bitmap)picBox.Image;
                        break;
                    case "pic_small_landscape_image":
                        selectedApp.small_landscape_image = (Bitmap)picBox.Image;
                        break;
                    case "pic_cover_landscape_image":
                        selectedApp.cover_landscape_image = (Bitmap)picBox.Image;
                        break;
                    case "pic_cover_landscape_image_large":
                        selectedApp.cover_landscape_image_large = (Bitmap)picBox.Image;
                        break;
                }

                GrabImageFromPictureBoxAndSave(selectedApp);
                RefreshDataGridViewMain();
            }
        }


        #endregion
    }
}
