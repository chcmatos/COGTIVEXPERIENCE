using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace COGTIVE.Utils
{
    public static class ResourceHelper
    {
        public static Color GetResourceColor(string resName)
        {
            var res = Application.Current.Resources[resName];
            return res is SolidColorBrush b ? b.Color :
                res is Color color ? color :
                throw new InvalidOperationException($"Resource \"{resName}\" is not a color or was not found it!");
        }

    }
}
