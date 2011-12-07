using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace CutFile
{
    public partial class Form1 : Form
    {
        private string _fileName = "";
        private long _fileLength = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonInputBrowse_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = textBoxInput.Text;

            var result = dialog.ShowDialog(this);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                bool setOutput = ((textBoxOutput.Text.Length == 0) || (textBoxOutput.Text == GetCutFilename(textBoxInput.Text)));

                textBoxInput.Text = dialog.FileName;
                if (setOutput)
                    textBoxOutput.Text = GetCutFilename(textBoxInput.Text);

                LoadFile(dialog.FileName);
            }
        }
        private void textBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                bool setOutput = ((textBoxOutput.Text.Length == 0) || (textBoxOutput.Text == GetCutFilename(_fileName)));

                if (setOutput)
                    textBoxOutput.Text = GetCutFilename(textBoxInput.Text);

                LoadFile(textBoxInput.Text);
            }
        }

        private void buttonOutputBrowse_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = textBoxOutput.Text;

            var result = dialog.ShowDialog(this);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textBoxOutput.Text = dialog.FileName;
                LoadFile(dialog.FileName);
            }
        }

        private string GetCutFilename(string fileName)
        {
            return Path.GetDirectoryName(fileName) + @"\" + Path.GetFileNameWithoutExtension(fileName) + "_cut" + Path.GetExtension(fileName);
        }

        public string GetNiceSize(long size)
        {
            double curr = 1.0;
            long next = 1024;

            if (size < next)
                return size + " bytes";

            curr = next;
            next *= 1024;
            if (size < next)
                return (size / curr).ToString("0.000") + " KB";

            curr = next;
            next *= 1024;
            if (size < next)
                return (size / curr).ToString("0.000") + " MB";

            curr = next;
            next *= 1024;
            if (size < next)
                return (size / curr).ToString("0.000") + " GB";

            curr = next;
            next *= 1024;
            if (size < next)
                return (size / curr).ToString("0.000") + " TB";

            return (size / curr).ToString("0.000") + " TB";
        }

        private void LoadFile(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            _fileName = fileName;
            _fileLength = fileInfo.Length;

            numericUpDownStart.Maximum = _fileLength;
            numericUpDownEnd.Maximum = _fileLength;
            numericUpDownStart.Value = 0;
            numericUpDownEnd.Value = _fileLength;
            trackBarStart.Value = 0;
            trackBarEnd.Value = trackBarEnd.Maximum;

            labelInputSize.Text = GetNiceSize(_fileLength);
            labelInputSize.Visible = true;
            labelOutputSize.Visible = true;
        }

        private void trackBarStart_Scroll(object sender, EventArgs e)
        {
            numericUpDownStart.Value = (long)(_fileLength * (trackBarStart.Value / 1000.0));
        }

        private void trackBarEnd_Scroll(object sender, EventArgs e)
        {
            numericUpDownEnd.Value = (long)(_fileLength * (trackBarEnd.Value / 1000.0));
        }

        private void numericUpDownStart_ValueChanged(object sender, EventArgs e)
        {
            trackBarStart.Value = (int)((numericUpDownStart.Value / _fileLength) * 1000);
            if (numericUpDownEnd.Value < numericUpDownStart.Value)
                numericUpDownEnd.Value = numericUpDownStart.Value;
            UpdateSize();
        }

        private void numericUpDownEnd_ValueChanged(object sender, EventArgs e)
        {
            trackBarEnd.Value = (int)((numericUpDownEnd.Value / _fileLength) * 1000);
            if (numericUpDownEnd.Value < numericUpDownStart.Value)
                numericUpDownStart.Value = numericUpDownEnd.Value;
            UpdateSize();
        }

        private void UpdateSize()
        {
            labelOutputSize.Text = GetNiceSize((long)(numericUpDownEnd.Value - numericUpDownStart.Value));
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            long startPos = (long)numericUpDownStart.Value;
            long endPos = (long)numericUpDownEnd.Value;

            using (var input = File.OpenRead(_fileName))
            {
                input.Seek(startPos, SeekOrigin.Begin);

                using (var output = File.OpenWrite(textBoxOutput.Text))
                {
                    byte[] buffer = new byte[65536];
                    long bytesLeft = endPos - startPos;

                    while (bytesLeft > 0)
                    {
                        int bytesToRead = buffer.Length;
                        if (bytesLeft < buffer.Length)
                            bytesToRead = (int)bytesLeft;
                        int bytesRead = input.Read(buffer, 0, bytesToRead);
                        if (bytesRead <= 0)
                            break;
                        output.Write(buffer, 0, bytesRead);
                        bytesLeft -= bytesRead;
                    }
                }
            }

            this.Enabled = true;
            buttonClose.Focus();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
