using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace ForoCoches_Code_Bot
{
    public partial class Form1 : Form
    {
        bool executed = false; //Anti-refresh
        bool executing = false; //For the timer, to wait browser doing some things

        TextBox[] codes
        {
            get
            {
                return new[] { i1TextBox, i2TextBox, i3TextBox, i4TextBox, i5TextBox };
            }
        }
        Button[] oks
        {
            get
            {
                return new[] { i1OkButton, i2OkButton, i3OkButton, i4OkButton, i5OkButton };
            }
        }
        Button[] copys
        {
            get
            {
                return new[] { i1CopyButton, i2CopyButton, i3CopyButton, i4CopyButton, i5CopyButton };
            }
        }
        ToolStripMenuItem oldWait;
        ToolStripMenuItem[] waits
        {
            get
            {
                return new[] { sToolStripMenuItem, sToolStripMenuItem1, mToolStripMenuItem, mToolStripMenuItem1, mToolStripMenuItem2, mToolStripMenuItem3 };
            }
        }

        DateTime updatedTime = DateTime.MinValue;
        int secondsToUpdate = 60; //1 minute default
        int pastSeconds = 0;

        public Form1()
        {
            InitializeComponent();

            oldWait = mToolStripMenuItem; //Set old to default
        }
        
        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;
            writeToButton("Iniciando...");
            stopButton.Enabled = true;
            
            secondsTimer.Start();

            for (int i = 0; i < waits.Length; i++)
                waits[i].Enabled = false;

            pastSeconds = secondsToUpdate;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = true;
            startButton.Text = "Comenzar";
            stopButton.Enabled = false;
            
            secondsTimer.Stop();

            webBrowser1.Stop();

            updatedTimeLabel.Text = "00/00/0000 00:00";
            updatedAgoTimeLabel.Text = "Hace: 0d, 0h, 0m, 0s";
            updatedTime = DateTime.MinValue;

            pastSeconds = 0;

            webBrowser1.Stop();

            for (int i = 0; i < codes.Length; i++)
            {
                codes[i].Text = "";
                codes[i].BackColor = SystemColors.Control;
                oks[i].Enabled = false;
                copys[i].Enabled = false;
            }

            for (int i = 0; i < waits.Length; i++)
                waits[i].Enabled = true;

            executed = false;
        }

        private void secondsTimer_Tick(object sender, EventArgs e)
        {
            if (executing)
                return;

            if (pastSeconds >= secondsToUpdate)
            {
                pastSeconds = 0;
                executing = true;
                executed = false;
                navigate();
                return;
            }

            pastSeconds++;

            writeToButton("Tiempo de espera: " + (secondsToUpdate - pastSeconds) + "s");

            updatedTime = updatedTime.AddSeconds(1);
            updatedAgoTimeLabel.Text = "Hace: " + ((updatedTime.Year - 1 != 0) ? (updatedTime.Year - 1) + "y, " : "") +
                (updatedTime.Day - 1) + "d, " + updatedTime.Hour + "h, " + updatedTime.Minute + "m, " + updatedTime.Second + "s";
        }

        private void navigate()
        {
            writeToButton("Buscando códigos...");
            stopButton.Enabled = false;
            webBrowser1.Navigate(@"https://www.facebook.com/pg/forocoches/posts/?ref=page_internal");
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (executed)
                return;

            writeToButton("Procesando...");

            updatedAgoTimeLabel.Text = "Hace: --d, --h, --m, --s";
            updatedAgoTimeLabel.Update();
            updatedTimeLabel.Text = "---/---/---- ---:---";
            updatedTimeLabel.Update();

            executed = true;
            bool codeFound = false;

            string[] lines = webBrowser1.DocumentText.Split('\n');
            List<string> tempCodes = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(" sin "))
                {
                    string temp = lines[i];
                    if (temp.StartsWith("<BR>"))
                        temp = temp.Remove(0, 4);
                    int remove = temp.IndexOf("<");
                    temp = temp.Remove(remove, temp.Length - remove);

                    if (temp.StartsWith("Código "))
                        temp = temp.Remove(0, 7);
                    else if (temp.StartsWith("Códigos "))
                        temp = temp.Remove(0, 8);
                    if (temp.EndsWith(" en "))
                        temp = temp.Remove(temp.Length - 4, 4);

                    tempCodes.Add(temp);
                    if (tempCodes.Count >= 5)
                        goto endSearch;
                }
            }
            endSearch:
            if (tempCodes.Count <= 0)
            {
                MessageBox.Show("ERROR:\n\nNo se han encontrado códigos. Revise su conexión", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                stopButton_Click(sender, e);
                return;
            }

            if (tempCodes[0] != i1TextBox.Text)
            {
                for (int i = 0; i < codes.Length; i++)
                {
                    if (tempCodes.Count >= i)
                    {
                        if (codes[i].Text != tempCodes[i] && codes[i].Text != "" && codes[i].Text != "Código no encontrado")
                        {
                            codes[i].BackColor = Color.LightGreen;
                            oks[i].Enabled = true;
                            codeFound = true;
                        }
                        codes[i].Text = tempCodes[i];
                        copys[i].Enabled = true;
                    }
                    else
                    {
                        codes[i].Text = "Código no encontrado";
                        codes[i].BackColor = SystemColors.Control;
                        oks[i].Enabled = false;
                        copys[i].Enabled = false;
                    }
                }
            }

            updatedTime = DateTime.MinValue;
            updatedTimeLabel.Text = DateTime.Now.ToString();

            if (codeFound)
                codeNotifyIcon.ShowBalloonTip(1000);

            stopButton.Enabled = true;
            executing = false;
        }

        private void codeNotifyIcon_Click(object sender, EventArgs e)
        {
            this.BringToFront();
            this.WindowState = FormWindowState.Normal;
        }

        private void codeNotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            try { Clipboard.SetText(codes[0].Text); }
            catch (Exception ex) { MessageBox.Show("ERROR:\n\n" + ex, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            Process.Start(@"https://forocoches.com/codigo");
        }

        private void writeToButton(string text)
        {
            startButton.Text = text;
            startButton.Update();
        }

        private void ayudaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new HelpForm().ShowDialog();
        }

        #region Oks
        private void i1OkButton_Click(object sender, EventArgs e)
        {
            i1OkButton.BackColor = SystemColors.Control;
        }
        private void i2OkButton_Click(object sender, EventArgs e)
        {
            i2OkButton.BackColor = SystemColors.Control;
        }
        private void i3OkButton_Click(object sender, EventArgs e)
        {
            i3OkButton.BackColor = SystemColors.Control;
        }
        private void i4OkButton_Click(object sender, EventArgs e)
        {
            i4OkButton.BackColor = SystemColors.Control;
        }
        private void i5OkButton_Click(object sender, EventArgs e)
        {
            i5OkButton.BackColor = SystemColors.Control;
        }
        #endregion
        #region Copys
        private void i1CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(i1OkButton.Text);
        }
        private void i2CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(i2OkButton.Text);
        }
        private void i3CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(i3OkButton.Text);
        }
        private void i4CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(i4OkButton.Text);
        }
        private void i5CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(i5OkButton.Text);
        }
        #endregion

        #region Opens
        private void faceBookForoCochesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://forocoches.com/codigo");
        }
        private void foroCochescodigoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://www.facebook.com/forocoches/");
        }
        #endregion
        #region Waits
        private void sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setWait(sToolStripMenuItem, 10);
        }
        private void sToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            setWait(sToolStripMenuItem1, 30);
        }
        private void mToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setWait(mToolStripMenuItem, 60);
        }
        private void mToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            setWait(mToolStripMenuItem1, 120);
        }
        private void mToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            setWait(mToolStripMenuItem2, 300);
        }
        private void mToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            setWait(mToolStripMenuItem3, 600);
        }
        private void setWait(ToolStripMenuItem tsmi, int secs)
        {
            tsmi.Checked = true;
            oldWait.Checked = false;
            oldWait = tsmi;
            secondsToUpdate = secs;
        }
        #endregion
    }
}
