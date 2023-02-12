using System.Drawing;
using KikoleSite.MetsTesTennis.Enums;

namespace KikoleSite.MetsTesTennis.Views
{
    public static class ViewsExtensions
    {
        public static string ToColor(this Surfaces? surface, bool indoor)
        {
            Color color;
            if (surface == Surfaces.Carpet || indoor)
                color = Color.DimGray;
            else if (surface == Surfaces.Grass)
                color = Color.Green;
            else if (surface == Surfaces.Clay)
                color = Color.SandyBrown;
            else if (surface == Surfaces.Hard)
                color = Color.Blue;
            else
                color = Color.White;

            return ColorTranslator.ToHtml(color);
        }
    }
}
