using System;

namespace JPPCommands
{
    public static class Constants
    {
        public const double Deg_0 = 0.0;
        public const double Deg_1 = 0.01745;
        public const double Deg_45 = 0.78540;
        public const double Deg_60 = 1.04720;
        public const double Deg_90 = 1.57080;
        public const double Deg_135 = 2.35619;
        public const double Deg_150 = 2.61799;
        public const double Deg_180 = 3.14159;
        public const double Deg_225 = 3.92699;
        public const double Deg_240 = 4.18879;
        public const double Deg_270 = 4.71239;
        public const double Deg_315 = 5.49779;
        public const double Deg_330 = 5.75959;
        public const double Deg_360 = 6.28319;

        public const double TextOffset = 0.14142;
        public const double JPP_App_Pt_Len = 0.2;
        public const double Access_Point_Width = 0.9;

        // public const int JPP_App_Point_Mode = 2;
    }

    enum Angle { Plus_90_Degrees, Minus_90_Degrees, Plus_180_Degrees };

    public static class StyleNames
    {
        public const string JPP_APP_Levels_Layer = "JPP_App_Levels";
        public const string JPP_APP_FFLs_Layer = "JPP_App_FFLs";
        public const string JPP_App_Outline_Layer = "JPP_App_Outline";
        public const string JPP_App_Text_Style = "JPP_App_Text";
        public const string JPP_App_Tanking_Layer = "JPP_App_Tanking";
        public const string JPP_App_Exposed_Brick_Layer = "JPP_App_Exposed_Brick";
    }

    public static class JPP_App_Config_Params
    {
        public const string JPP_APP_CONFIG_DATA = "JPP_App_Config_Data";
        public const string JPP_APP_NAME = "JPP_App";
        public const string JPP_APP_NEXT_BLOCK_INDEX = "JPP_App_Next_Block_Index";
        public const string JPP_APP_NEXT_GROUP_INDEX = "JPP_App_Next_Group_Index";
        public const string JPP_APP_NEW_BLOCK_PREFIX = "JPP_App_Outline_";
    }
}