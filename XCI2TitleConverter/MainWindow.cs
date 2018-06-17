﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static XCI2TitleConverter.Utils;

namespace XCI2TitleConverter
{
    public partial class MainWindow : Form
    {
        private string[] xciList = new string[] { };
        private string pathXCIDir = "";
        private string pathOutput = "";
        private string pathHactool = "";
        private string pathKeys = "";
        private string targetTitleId = "";

        public MainWindow()
        {
            InitializeComponent();
            this.pathXCIDir = Properties.Settings.Default.pathXCIDir;
            this.pathOutput = Properties.Settings.Default.pathOutput;
            this.pathHactool = Properties.Settings.Default.pathHactool;
            this.pathKeys = Properties.Settings.Default.pathKeys;
            this.updateFormValues();
            this.readXCIDirectory();
        }

        private void updateFormValues()
        {
            this.txtXCIDir.Text = this.pathXCIDir;
            this.txtOutput.Text = this.pathOutput;
            this.txtHactool.Text = this.pathHactool;
            this.txtKeys.Text = this.pathKeys;
            this.txtTitleId.Text = this.targetTitleId;
        }

        private void dirSearch(DirectoryInfo sDir)
        {
            foreach (DirectoryInfo subDir in sDir.GetDirectories())
            {
                dirSearch(subDir);
            }

            
            FileInfo[] Files = sDir.GetFiles("*.xci");
            foreach (FileInfo file in Files)
            {
                ComboboxItem item = new ComboboxItem();
                item.Text = sDir.Name+"/"+file.Name;
                this.cmbXCIFile.Items.Add(item);
            }
        }

        private void readXCIDirectory()
        {
            if (this.pathXCIDir == null || this.pathXCIDir == "") return;

            DirectoryInfo d = new DirectoryInfo(this.pathXCIDir);

            this.cmbXCIFile.Items.Clear();
            this.cmbXCIFile.SelectedItem = -1;
            this.cmbXCIFile.Text = "";
            dirSearch(d);

        }

        private void btnXCIDir_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    this.pathXCIDir = folderBrowserDialog.SelectedPath;
                    this.updateFormValues();
                    Properties.Settings.Default["pathXCIDir"] = folderBrowserDialog.SelectedPath;
                    Properties.Settings.Default.Save();
                    this.readXCIDirectory();
                }
            }
        }

        private void btnOutput_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    this.pathOutput = folderBrowserDialog.SelectedPath;
                    this.updateFormValues();
                    Properties.Settings.Default["pathOutput"] = folderBrowserDialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void btnHactool_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Hactool binary|hactool.exe";
            fileDialog.Title = "Select a hacktool.exe";
         
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                this.pathHactool = fileDialog.FileName;
                this.updateFormValues();
                Properties.Settings.Default["pathHactool"] = fileDialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void btnKeys_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Keyset|*.txt";
            openFileDialog1.Title = "Select a hacktool.exe";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.pathKeys = openFileDialog1.FileName;
                this.updateFormValues();
                Properties.Settings.Default["pathKeys"] = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            this.ActiveControl = label1;
            foreach (KeyValuePair<string, string> title in Constants.TARGET_TITLES)
            {
                ComboboxItem item = new ComboboxItem();
                item.Text = title.Value;
                item.Value = title.Key;
                this.cmbTarget.Items.Add(item);
            }
        }

        private void cmbTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.targetTitleId = ((ComboboxItem)cmbTarget.SelectedItem).Value.ToString();
            this.updateFormValues();
        }

        private string getLargestFileInPath(string path)
        {
            return new DirectoryInfo(path)
                .EnumerateFiles()
                .OrderByDescending(f => f.Length)
                .FirstOrDefault()
                .FullName;
        }

        private bool areParamsValid()
        {
            if (this.pathXCIDir == "")
            {
                MessageBox.Show("Missing XCI path", null, MessageBoxButtons.OK);
                return false;
            }

            if ( this.pathOutput == "")
            {
                MessageBox.Show("Missing output path", null, MessageBoxButtons.OK);
                return false;
            }

            if (this.pathHactool == "")
            {
                MessageBox.Show("Missing hactool filepath", null, MessageBoxButtons.OK);
                return false;
            }

            if (this.pathKeys == "")
            {
                MessageBox.Show("Missing keys filepath", null, MessageBoxButtons.OK);
                return false;
            }

            if (this.txtTitleId.Text == "")
            {
                MessageBox.Show("Missing title id value", null, MessageBoxButtons.OK);
                return false;
            }

            if (!(new Regex(@"^[a-fA-F0-9]{16}$")).Match(this.txtTitleId.Text).Success)
            {
                MessageBox.Show("Wrong title id format", null, MessageBoxButtons.OK);
                return false;
            }

            
            if ((ComboboxItem)cmbXCIFile.SelectedItem == null)
            {
                MessageBox.Show("Missing xci file value", null, MessageBoxButtons.OK);
                return false;
            }

            string xciFile = ((ComboboxItem)cmbXCIFile.SelectedItem).Text.ToString();
            if (xciFile == "")
            {
                MessageBox.Show("Missing xci file value", null, MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!this.areParamsValid()) return;

            try
            {
                this.startProcess();
            }
            catch (Exception excep)
            {
                Console.Write(excep);
                MessageBox.Show(excep.Message + excep.StackTrace, null, MessageBoxButtons.OK);
                return;
            }
        }

        private void lnklblTitleList_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Constants.TITLE_LIST_URL);
        }

        private void startProcess()
        {
            string targetTitleId = this.txtTitleId.Text.ToUpper();

            string xciFile = ((ComboboxItem)cmbXCIFile.SelectedItem).Text.ToString();
            string tilePath = Path.Combine(this.pathOutput, targetTitleId);
            string securePath = Path.Combine(tilePath, "secure");
            string xciFilePath = Path.Combine(this.pathXCIDir, xciFile);
            string romfsFile = Path.Combine(tilePath, "romfs.bin");
            string exefsPath = Path.Combine(tilePath, "exefs");

            if (Directory.Exists(tilePath))
            {
                Directory.Delete(tilePath, true);
            }

            Directory.CreateDirectory(tilePath);
            DirectoryInfo secureDirectory = Directory.CreateDirectory(securePath);

            // Decrypt XCI
            string decryptXCIargs = String.Format("--intype=xci --securedir=\"{0}\" \"{1}\"", securePath, xciFilePath);
            using (Process process = Process.Start(this.pathHactool, decryptXCIargs)) process.WaitForExit();

            // Decrypt NCA
            string largestNCAFile = this.getLargestFileInPath(securePath);
            string decryptNCAargs = String.Format("--keyset=\"{0}\" --romfs=\"{1}\" --exefsdir=\"{2}\" \"{3}\"", this.pathKeys, romfsFile, exefsPath, largestNCAFile);
            using (Process process = Process.Start(this.pathHactool, decryptNCAargs)) process.WaitForExit();

            secureDirectory.Delete(true);

            // Save empty file with decrypted XCI name
            File.Create(Path.Combine(tilePath, xciFile)).Close();

            if (!Directory.Exists(exefsPath))
            {
                throw new Exception("Unable to decrypt NCA file, check your keyset! You should remove created files.");
            }

            ulong targetTitleIdULong = (ulong)Convert.ToUInt64(targetTitleId, 16);
            string npdmFilePath = Path.Combine(exefsPath, "main.npdm");
            byte[] npdmBytes = File.ReadAllBytes(npdmFilePath);
            byte[] patchedNpdmBytes = getPatchedNpdmBytes(npdmBytes, targetTitleIdULong);

            File.Delete(npdmFilePath);
            File.WriteAllBytes(npdmFilePath, patchedNpdmBytes);

            MessageBox.Show("Success!", this.Text, MessageBoxButtons.OK);
        }
    }
}
