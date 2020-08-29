using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


namespace SuperContextMenu
{
    public partial class PoperContainer : ToolStripDropDown
    {
        private Control m_popedContainer;
        private ToolStripControlHost m_host;
        private bool m_fade = true;

        private const int frames = 5;
        private const int totalduration = 100;
        private const int frameduration = totalduration / frames;

        public delegate void OnClseHandler(object sender, EventArgs e);
        public event OnClseHandler OnClose;

        public PoperContainer(Control popedControl)
        {
            InitializeComponent();
            this.m_popedContainer = popedControl ?? throw new ArgumentNullException("content");

            this.m_fade = SystemInformation.IsMenuAnimationEnabled && SystemInformation.IsMenuFadeEnabled;

            this.m_host = new ToolStripControlHost(popedControl);
            m_host.AutoSize = false;//make it take the same room as the poped control
       
            Padding = Margin = m_host.Padding = m_host.Margin = Padding.Empty;
            
            popedControl.Location = Point.Empty;
            
            this.Items.Add(m_host);
            
            popedControl.Disposed += delegate(object sender, EventArgs e)
            {
                popedControl = null;
                Dispose(true);// this popup container will be disposed immediately after disposion of the contained control
            };
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {//prevent alt from closing it and allow alt+menumonic to work
            if ((keyData & Keys.Alt) == Keys.Alt)
                return false;

            return base.ProcessDialogKey(keyData);
        }

        public void ShowContext(Control control, Point p)
        {
            this.Show(control, new Rectangle(p, new Size(0, 0)));
        }

        private void Show(Control control, Rectangle area)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            Point location = control.PointToScreen(new Point(area.Left, area.Top + area.Height));
            
            Rectangle screen = Screen.FromControl(control).WorkingArea;
            
            if (location.X + Size.Width > (screen.Left + screen.Width))
                location.X = (screen.Left + screen.Width) - Size.Width;
            
            if (location.Y + Size.Height > (screen.Top + screen.Height))
                location.Y -= Size.Height + area.Height;
                    
            location = control.PointToClient(location);
            
            Show(control, location, ToolStripDropDownDirection.Default);
        }

        public void HideContext()
        {
            SetVisibleCore(false);
        }

        protected override void SetVisibleCore(bool visible)
        {
            double opacity = Opacity;
            if (visible && m_fade) Opacity = 0;
            base.SetVisibleCore(visible);
            if (!visible || !m_fade) return;
            for (int i = 1; i <= frames; i++)
            {
                if (i > 1)
                {
                    System.Threading.Thread.Sleep(frameduration);
                }
                Opacity = opacity * (double)i / (double)frames;
            }
            Opacity = opacity;
        }

        protected override void OnClosed(ToolStripDropDownClosedEventArgs e)
        {
            // Make sure someone is listening to event
            if (OnClose == null) return;

            OnClose(this, e);

            base.OnClosed(e);
        }

        protected override void OnOpening(CancelEventArgs e)
        {
            if (m_popedContainer.IsDisposed || m_popedContainer.Disposing)
            {
                e.Cancel = true;
                return;
            }
            base.OnOpening(e);
        }

        protected override void OnOpened(EventArgs e)
        {
            m_popedContainer.Focus();
            
            base.OnOpened(e);
        }
    }
}
