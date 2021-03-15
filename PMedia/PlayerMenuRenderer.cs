using System.Drawing;
using System.Windows.Forms;

namespace PMedia
{
    class PlayerMenuRenderer : ToolStripProfessionalRenderer
    {
        public PlayerMenuRenderer() : base(new PlayerMenuColors()) { }
    }

    class PlayerMenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(37, 37, 37); }
        }

        public override Color MenuItemBorder
        {
            get { return Color.FromArgb(37, 37, 37); }
        }

        public override Color ToolStripDropDownBackground
        {
            get { return Color.FromArgb(51, 51, 51); }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Color.FromArgb(51, 51, 51); }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Color.FromArgb(51, 51, 51); }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Color.FromArgb(51, 51, 51); }
        }

        public override Color SeparatorLight
        {
            get { return Color.FromArgb(37, 37, 37); }
        }
        
        public override Color SeparatorDark
        {
            get { return Color.FromArgb(20, 20, 20); }
        }

    }
}
