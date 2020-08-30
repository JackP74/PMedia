using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PMedia
{
    class PlayerContextMenuItems
    {
        private ContextMenuStrip CurrentContext;
        private delegate void SafePlayBtnTxt(string newTxt);

        public ToolStripItem PlayBtn;

        public PlayerContextMenuItems(ContextMenuStrip currentMenu)
        {
            this.CurrentContext = currentMenu;
            PlayBtn = new ToolStripButton("Play");
        }

        public void SetPlayBtnTxt(string newTxt)
        {
            if (CurrentContext.InvokeRequired)
            {
                var d = new SafePlayBtnTxt(SetPlayBtnTxt);
                CurrentContext.Invoke(d, new object[] { newTxt });
            }
            else
            {
                PlayBtn.Text = newTxt;
            }
        }
    }
}
