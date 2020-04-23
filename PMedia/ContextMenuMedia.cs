using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SuperContextMenu
{
    public partial class ContextMenuMedia : UserControl
    {
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

        public ContextMenuMedia()
        {
            InitializeComponent();
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
