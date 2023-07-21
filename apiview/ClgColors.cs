using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using symlib;

namespace cppsymview
{
    internal class ClgColors
    {
        static Dictionary<CXCursorKind, Color> cursorColors = null;
        public static Dictionary<CXCursorKind, Color> CursorColor
        {
            get
            {
                if (cursorColors == null)
                {

                    List<Color> colors = new List<Color>();
                    int numsegs = 12;

                    for (int j = 0; j < 3; ++j)
                    {
                        float l = (1 - j) * 0.25f + 0.5f;
                        for (int k = 0; k < 3; ++k)
                        {
                            float s = 0.33f + k * 0.33f;
                            for (int i = 0; i < numsegs; ++i)
                            {
                                Cnv.HSL hsl = new Cnv.HSL((float)i / (float)numsegs, s, l);
                                Cnv.RGB rgb = Cnv.HSLToRGB(hsl);
                                colors.Add(Color.FromArgb(255, rgb.R, rgb.G, rgb.B));
                            }
                        }
                    }

                    cursorColors = new Dictionary<CXCursorKind, Color>();
                    int idx = 0;
                    foreach (var cursorKind in ClangTypes.CursorKindsMRU)
                    {
                        cursorColors.Add(cursorKind, colors[idx]);
                        idx++;
                    }
                }

                return cursorColors;
            }
        }

    }
    public class CursorKindToBrushConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Brush))
            {
                CXCursorKind ck = (CXCursorKind)value;
                return new SolidColorBrush(ClgColors.CursorColor[ck]);
            }
            throw new InvalidOperationException("Converter can only convert to value of type Visibility.");
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("Converter cannot convert back.");
        }
    }
}
