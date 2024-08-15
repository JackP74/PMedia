using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MessageCustomHandler;

namespace PMedia;

class FormattedSlider : Slider
{
    private ToolTip _autoToolTip;

    static FormattedSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FormattedSlider),
            new FrameworkPropertyMetadata(typeof(Slider)));
    }

    public string AutoToolTipFormat { get; set; }

    protected override void OnThumbDragStarted(DragStartedEventArgs e)
    {
        base.OnThumbDragStarted(e);
        this.FormatAutoToolTipContent();
    }

    protected override void OnThumbDragDelta(DragDeltaEventArgs e)
    {
        base.OnThumbDragDelta(e);
        this.FormatAutoToolTipContent();
    }

    private void FormatAutoToolTipContent()
    {
        if (!string.IsNullOrEmpty(this.AutoToolTipFormat))
        {
            string Content = this.AutoToolTip.Content.ToString().Replace(",", "");

            try
            {
                this.AutoToolTip.Content = TimeSpan.FromSeconds(Convert.ToDouble(Content)).ToString();
            } 
            catch (Exception ex)
            {
                CMBox.Show("Error in slider", Content, MessageCustomHandler.Style.Error, Buttons.OK, ex.ToString());
            }
        }
    }

    private ToolTip AutoToolTip
    {
        get
        {
            if(_autoToolTip == null)
            {
                FieldInfo field = typeof(Slider).GetField("_autoToolTip", BindingFlags.NonPublic | BindingFlags.Instance);
                _autoToolTip = field.GetValue(this) as ToolTip;
            }
            return _autoToolTip;
        }
    }

}
