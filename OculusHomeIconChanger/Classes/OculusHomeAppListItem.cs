using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace OculusHomeIconChangerNS
{
    public class OculusHomeAppListItem
    {
        public Bitmap icon { get; set; } // icon used for datagridview only
        public string displayName { get; set; } // non-assets file

        [System.ComponentModel.Browsable(false)] // hide from datagridview
        public string displayNameOrig { get; set; }

        public string canonicalName { get; set; } // non-assets file

        [System.ComponentModel.Browsable(false)] // hide from datagridview
        public string launchFile { get; set; } // non-assets file

        [System.ComponentModel.Browsable(false)]
        public DateTime fileModifiedDateTime { get; set; } // read from file direct

        [System.ComponentModel.Browsable(false)]
        public Bitmap cover_landscape_image { get; set; }

        [System.ComponentModel.Browsable(false)]
        public Bitmap icon_image { get; set; } // icon_image.jpg from assets folder

        [System.ComponentModel.Browsable(false)]
        public Bitmap cover_square_image { get; set; }
        
        [System.ComponentModel.Browsable(false)]
        public Bitmap small_landscape_image { get; set; }

        [System.ComponentModel.Browsable(false)]
        public bool photosChanged { get; set; }

        [System.ComponentModel.Browsable(false)]
        public bool nameChanged { get; set; }

        [System.ComponentModel.Browsable(false)]
        public string steamID { get; set; }

        // Default Constructor
        public OculusHomeAppListItem()
        {
            // Default no edits (used for save method after)
            photosChanged = false;
            nameChanged = false;
        }
    }
}
