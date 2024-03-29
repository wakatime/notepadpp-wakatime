﻿using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly ConfigFile _configFile;
        private readonly Logger _logger;

        public SettingsForm(ConfigFile configFile, Logger logger)
        {
            _configFile = configFile;
            _logger = logger;

            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = _configFile.GetSetting("api_key");
                txtProxy.Text = _configFile.GetSetting("proxy");
                chkDebugMode.Checked = _configFile.GetSettingAsBoolean("debug");
            }
            catch (Exception ex)
            {
                _logger.Error("Error when loading form SettingsForm:", ex);

                MessageBox.Show(ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                var matched = Regex.IsMatch(txtAPIKey.Text.Trim(), "(?im)^(waka_)?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}$");

                if (matched)
                {
                    _configFile.SaveSetting("settings", "api_key", txtAPIKey.Text.Trim());
                    _configFile.SaveSetting("settings", "proxy", txtProxy.Text.Trim());
                    _configFile.SaveSetting("settings", "debug", chkDebugMode.Checked.ToString().ToLower());
                }
                else
                {
                    MessageBox.Show(@"Please enter valid Api Key.");

                    DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error when saving data from SettingsForm:", ex);

                MessageBox.Show(ex.Message);
            }
        }
    }
}
