using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace JPP.Civils
{
    public class PlotStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlotStatus)
            {
                string path; 
                switch (value)
                {
                    case PlotStatus.Approved:
                        path = "/Civils;component/Resources/_checked.png";
                        break;

                    case PlotStatus.Error:
                        path = "/Civils;component/Resources/error.png";
                        break;

                    case PlotStatus.ForApproval:
                        path = "/Civils;component/Resources/approval.png";
                        break;

                    case PlotStatus.Warning:
                        path = "/Civils;component/Resources/warning.png";
                        break;

                    default:
                        throw new NotImplementedException();
                        break;
                }

                return path;//new BitmapImage(new Uri(path, UriKind.Relative));

            } else
            {
                throw new NotImplementedException();
            }            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
