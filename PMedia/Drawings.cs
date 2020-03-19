using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMedia
{
    static class Drawings
    {
        public static Bitmap GetPlayImage(int size)
        {
            if (size < 15)
                throw new Exception("Size cannot be smaller than 15");

            int offset = 2;
            Color color = Color.FromArgb(50, 131, 168);

            Bitmap finalImage = new Bitmap(size, size);

            Graphics g = Graphics.FromImage(finalImage);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            g.Clear(Color.Transparent);

            GraphicsPath gp = new GraphicsPath();
            gp.AddLine(offset, offset, size - (offset * 2), (size / 2) - offset);
            gp.AddLine(size - (offset * 2), (size / 2) - offset, offset, size - (offset * 2));
            gp.AddLine(offset, size - (offset * 2), offset, offset);

            g.FillPath(new SolidBrush(color), gp);

            g.Save();
            g.Dispose();
            gp.Dispose();

            return finalImage;
        }
    }
}
