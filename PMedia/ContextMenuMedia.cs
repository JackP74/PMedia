using System;
using System.Windows.Forms;

namespace SuperContextMenu
{
    public partial class ContextMenuMedia : UserControl
    {
        public delegate void MediaInfoBtnHandler(object sender, EventArgs e);
        public event MediaInfoBtnHandler OnMediaInfoBtn;

        public delegate void VideoListBtnHandler(object sender, EventArgs e);
        public event VideoListBtnHandler OnVideoListBtn;

        public delegate void NextBtnHandler(object sender, EventArgs e);
        public event NextBtnHandler OnNextBtn;

        public delegate void PreviousBtnHandler(object sender, EventArgs e);
        public event PreviousBtnHandler OnPreviousBtn;

        public delegate void PlayBtnHandler(object sender, EventArgs e);
        public event PlayBtnHandler OnPlayBtn;

        public delegate void StopBtnHandler(object sender, EventArgs e);
        public event StopBtnHandler OnStopBtn;

        public delegate void BackwardBtnHandler(object sender, EventArgs e);
        public event BackwardBtnHandler OnBackwardBtn;

        public delegate void ForwardBtnHandler(object sender, EventArgs e);
        public event ForwardBtnHandler OnForwardBtn;

        public delegate void VolumeUpBtnHandler(object sender, EventArgs e);
        public event VolumeUpBtnHandler OnVolumeUpBtn;

        public delegate void VolumeDownBtnHandler(object sender, EventArgs e);
        public event VolumeDownBtnHandler OnVolumeDownBtn;

        public delegate void MuteBtnHandler(object sender, EventArgs e);
        public event MuteBtnHandler OnMuteBtn;

        public delegate void FullscreenBtnHandler(object sender, EventArgs e);
        public event FullscreenBtnHandler OnFullscreenBtn;

        public delegate void MouseClickBtnHandler(object sender, EventArgs e);
        public event MouseClickBtnHandler OnMouseClickBtn;

        public bool NextActive
        {
            set
            {
                if (BtnNext.InvokeRequired)
                {
                    BtnNext.Invoke(new Action(() => { BtnNext.Enabled = value; }));
                }
                else
                {
                    BtnNext.Enabled = value;
                }
            }
        }

        public bool PreviousActive
        {
            set
            {
                if (BtnPrevious.InvokeRequired)
                {
                    BtnPrevious.Invoke(new Action(() => { BtnPrevious.Enabled = value; }));
                }
                else
                {
                    BtnPrevious.Enabled = value;
                }
                
            }
        }

        public ContextMenuMedia()
        {
            InitializeComponent();

            NextActive = false;
            PreviousActive = false;
        }

        private void BtnMediaInfo_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnMediaInfoBtn == null) return;

            OnMediaInfoBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnVideoList_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnVideoListBtn == null) return;

            OnVideoListBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnNextBtn == null) return;

            OnNextBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnPrevious_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnPreviousBtn == null) return;

            OnPreviousBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnPlayBtn == null) return;

            OnPlayBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e); 
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnStopBtn == null) return;

            OnStopBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnBackward_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnBackwardBtn == null) return;

            OnBackwardBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnForward_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnForwardBtn == null) return;

            OnForwardBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnVolumeUp_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnVolumeUpBtn == null) return;

            OnVolumeUpBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnVolumeDown_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnVolumeDownBtn == null) return;

            OnVolumeDownBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnMute_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnMuteBtn == null) return;

            OnMuteBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }

        private void BtnFullscreen_Click(object sender, EventArgs e)
        {
            // Make sure someone is listening to event
            if (OnFullscreenBtn == null) return;

            OnFullscreenBtn(sender, e);

            //Safe closing
            OnMouseClickBtn(sender, e);
        }
    }
}
