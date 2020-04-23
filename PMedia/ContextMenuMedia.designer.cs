namespace SuperContextMenu
{
    partial class ContextMenuMedia
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnForward = new System.Windows.Forms.Button();
            this.btnBackward = new System.Windows.Forms.Button();
            this.btnSeparator01 = new System.Windows.Forms.Button();
            this.btnVolumeUp = new System.Windows.Forms.Button();
            this.btnVolumeDown = new System.Windows.Forms.Button();
            this.btnMute = new System.Windows.Forms.Button();
            this.btnSeparator02 = new System.Windows.Forms.Button();
            this.btnFullscreen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnPlay
            // 
            this.btnPlay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPlay.FlatAppearance.BorderSize = 0;
            this.btnPlay.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnPlay.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlay.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnPlay.Location = new System.Drawing.Point(1, 1);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(110, 23);
            this.btnPlay.TabIndex = 0;
            this.btnPlay.Text = "Play/Pause";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.BtnPlay_Click);
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnStop.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnStop.Location = new System.Drawing.Point(1, 23);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(110, 23);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.BtnStop_Click);
            // 
            // btnForward
            // 
            this.btnForward.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnForward.FlatAppearance.BorderSize = 0;
            this.btnForward.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnForward.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnForward.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnForward.Location = new System.Drawing.Point(1, 67);
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(110, 23);
            this.btnForward.TabIndex = 2;
            this.btnForward.Text = "Forward";
            this.btnForward.UseVisualStyleBackColor = true;
            this.btnForward.Click += new System.EventHandler(this.BtnForward_Click);
            // 
            // btnBackward
            // 
            this.btnBackward.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBackward.FlatAppearance.BorderSize = 0;
            this.btnBackward.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnBackward.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnBackward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackward.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnBackward.Location = new System.Drawing.Point(1, 45);
            this.btnBackward.Name = "btnBackward";
            this.btnBackward.Size = new System.Drawing.Size(110, 23);
            this.btnBackward.TabIndex = 3;
            this.btnBackward.Text = "Backward";
            this.btnBackward.UseVisualStyleBackColor = true;
            this.btnBackward.Click += new System.EventHandler(this.BtnBackward_Click);
            // 
            // btnSeparator01
            // 
            this.btnSeparator01.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSeparator01.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29)))));
            this.btnSeparator01.Enabled = false;
            this.btnSeparator01.FlatAppearance.BorderSize = 0;
            this.btnSeparator01.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSeparator01.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnSeparator01.Location = new System.Drawing.Point(1, 90);
            this.btnSeparator01.Margin = new System.Windows.Forms.Padding(0);
            this.btnSeparator01.Name = "btnSeparator01";
            this.btnSeparator01.Size = new System.Drawing.Size(110, 10);
            this.btnSeparator01.TabIndex = 4;
            this.btnSeparator01.UseVisualStyleBackColor = false;
            // 
            // btnVolumeUp
            // 
            this.btnVolumeUp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVolumeUp.FlatAppearance.BorderSize = 0;
            this.btnVolumeUp.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnVolumeUp.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnVolumeUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVolumeUp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnVolumeUp.Location = new System.Drawing.Point(1, 92);
            this.btnVolumeUp.Name = "btnVolumeUp";
            this.btnVolumeUp.Size = new System.Drawing.Size(110, 23);
            this.btnVolumeUp.TabIndex = 5;
            this.btnVolumeUp.Text = "Volume Up";
            this.btnVolumeUp.UseVisualStyleBackColor = true;
            this.btnVolumeUp.Click += new System.EventHandler(this.BtnVolumeUp_Click);
            // 
            // btnVolumeDown
            // 
            this.btnVolumeDown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnVolumeDown.FlatAppearance.BorderSize = 0;
            this.btnVolumeDown.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnVolumeDown.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnVolumeDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVolumeDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnVolumeDown.Location = new System.Drawing.Point(1, 114);
            this.btnVolumeDown.Name = "btnVolumeDown";
            this.btnVolumeDown.Size = new System.Drawing.Size(110, 23);
            this.btnVolumeDown.TabIndex = 6;
            this.btnVolumeDown.Text = "Volume Down";
            this.btnVolumeDown.UseVisualStyleBackColor = true;
            this.btnVolumeDown.Click += new System.EventHandler(this.BtnVolumeDown_Click);
            // 
            // btnMute
            // 
            this.btnMute.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMute.FlatAppearance.BorderSize = 0;
            this.btnMute.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnMute.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnMute.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMute.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnMute.Location = new System.Drawing.Point(1, 136);
            this.btnMute.Name = "btnMute";
            this.btnMute.Size = new System.Drawing.Size(110, 23);
            this.btnMute.TabIndex = 7;
            this.btnMute.Text = "Mute";
            this.btnMute.UseVisualStyleBackColor = true;
            this.btnMute.Click += new System.EventHandler(this.BtnMute_Click);
            // 
            // btnSeparator02
            // 
            this.btnSeparator02.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSeparator02.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29)))));
            this.btnSeparator02.Enabled = false;
            this.btnSeparator02.FlatAppearance.BorderSize = 0;
            this.btnSeparator02.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSeparator02.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnSeparator02.Location = new System.Drawing.Point(-1, 159);
            this.btnSeparator02.Margin = new System.Windows.Forms.Padding(0);
            this.btnSeparator02.Name = "btnSeparator02";
            this.btnSeparator02.Size = new System.Drawing.Size(112, 10);
            this.btnSeparator02.TabIndex = 8;
            this.btnSeparator02.UseVisualStyleBackColor = false;
            // 
            // btnFullscreen
            // 
            this.btnFullscreen.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFullscreen.FlatAppearance.BorderSize = 0;
            this.btnFullscreen.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnFullscreen.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(37)))));
            this.btnFullscreen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFullscreen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(173)))), ((int)(((byte)(254)))));
            this.btnFullscreen.Location = new System.Drawing.Point(0, 161);
            this.btnFullscreen.Name = "btnFullscreen";
            this.btnFullscreen.Size = new System.Drawing.Size(111, 23);
            this.btnFullscreen.TabIndex = 9;
            this.btnFullscreen.Text = "Fullscreen";
            this.btnFullscreen.UseVisualStyleBackColor = true;
            this.btnFullscreen.Click += new System.EventHandler(this.BtnFullscreen_Click);
            // 
            // ContextMenuMedia
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.btnFullscreen);
            this.Controls.Add(this.btnSeparator02);
            this.Controls.Add(this.btnMute);
            this.Controls.Add(this.btnVolumeDown);
            this.Controls.Add(this.btnVolumeUp);
            this.Controls.Add(this.btnSeparator01);
            this.Controls.Add(this.btnBackward);
            this.Controls.Add(this.btnForward);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPlay);
            this.Name = "ContextMenuMedia";
            this.Size = new System.Drawing.Size(113, 185);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnForward;
        private System.Windows.Forms.Button btnBackward;
        private System.Windows.Forms.Button btnSeparator01;
        private System.Windows.Forms.Button btnVolumeUp;
        private System.Windows.Forms.Button btnVolumeDown;
        private System.Windows.Forms.Button btnMute;
        private System.Windows.Forms.Button btnSeparator02;
        private System.Windows.Forms.Button btnFullscreen;
    }
}
