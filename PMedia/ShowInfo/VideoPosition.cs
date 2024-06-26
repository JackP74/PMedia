﻿using MessageCustomHandler;
using System;
using System.IO;
using System.Text;

namespace PMedia;

class VideoPosition
{
    private readonly string path;
    private string name;
    private int duration;

    public bool ErrorSaving = false;

    public VideoPosition(string Path)
    {
        if (!Directory.Exists(Path))
        {
            try
            {
                Directory.CreateDirectory(Path);
                
            }
            catch (Exception ex)
            {
                CMBox.Show("Error", "Couldn't set video position directory, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
                return;
            }
        }

        this.path = Path;
    }

    public void SetNewFile(string FilePath, int Duration)
    {
        this.name = $"{FilePath}-{Duration}";
        this.duration = Duration;
    }

    public void ClearName()
    {
        this.name = string.Empty;
        this.duration = 0;
    }

    public long GetPosition()
    {
        try
        {
            string filePath = Path.Combine(this.path, this.name + ".ini");
            if (!File.Exists(filePath))
                return 0;

            string position = "0";

            if (duration > 180)
                position = File.ReadAllText(filePath, Encoding.ASCII);

            int finalPosition = position.ToInt32();

            if ((duration - finalPosition) < 180)
            {
                if (duration > 180)
                {
                    finalPosition = duration - 180;
                }
                else
                {
                    finalPosition = 0;
                }
            }

            return finalPosition * 1000;
        }
        catch (Exception ex)
        {
            CMBox.Show("Error", "Couldn't get video position, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
            return 0;
        }
    }

    public void SavePosition(int Position)
    {
        try
        {
            if (this.name == null || this.name.Length == 0)
                return;

            string filePath = Path.Combine(this.path, this.name + ".ini");
            File.WriteAllText(filePath, Position.ToString(), Encoding.ASCII);
        }
        catch (Exception ex)
        {
            CMBox.Show("Error", "Couldn't save video position, Error: " + ex.Message, Style.Error, Buttons.OK, ex.ToString());
            ErrorSaving = true;
            return;
        }
    }
}