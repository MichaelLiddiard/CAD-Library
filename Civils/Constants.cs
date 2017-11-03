﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPP.Civils
{
    class Constants
    {
        /// <summary>
        /// Number of drawing units to offset tanking hatch by
        /// </summary>
        public const float TankingHatchOffest = 1f;

        public const string ProposedGroundName = "Proposed Ground";


        #region Civil Document Store
        /// <summary>
        /// Object dictionary ID for site wide data
        /// </summary>
        public const string SiteID = "JPP_Civil_Site";

        /// <summary>
        /// Object dictionary ID for current plots
        /// </summary>
        public const string PlotID = "JPP_Civil_Plots";

        /// <summary>
        /// Object dictionary ID for plot types
        /// </summary>
        public const string PlotTypeID = "JPP_Civil_Plot_Types";
        #endregion
    }
}
