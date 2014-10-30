// This file is part of AlarmWorkflow.
// 
// AlarmWorkflow is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// AlarmWorkflow is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AlarmWorkflow.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using AlarmWorkflow.BackendService.SettingsContracts;
using AlarmWorkflow.Shared;
using AlarmWorkflow.Shared.Core;
using AlarmWorkflow.Shared.Settings;
using AlarmWorkflow.Shared.Specialized;

namespace AlarmWorkflow.AlarmSource.Fax
{
    internal sealed class FaxConfiguration : DisposableObject
    {
        #region Constants

        private const string Identifier = "FaxAlarmSource";

        #endregion

        #region Fields

        private ISettingsServiceInternal _settings;

        #endregion

        #region Properties

        internal event EventHandler<ConfigChangedEventArgs> ConfigurationChanged;

        internal string FaxPath
        {
            get { return _settings.GetSetting(FaxSettingKeys.FaxPath).GetValue<string>(); }
        }

        internal string ArchivePath
        {
            get { return _settings.GetSetting(FaxSettingKeys.ArchivePath).GetValue<string>(); }
        }

        internal string AnalysisPath
        {
            get { return _settings.GetSetting(FaxSettingKeys.AnalysisPath).GetValue<string>(); }
        }

        internal string OCRSoftwarePath
        {
            get { return _settings.GetSetting(FaxSettingKeys.OcrPath).GetValue<string>(); }
        }

        internal string AlarmFaxParserAlias
        {
            get { return _settings.GetSetting(FaxSettingKeys.AlarmFaxParserAlias).GetValue<string>(); }
        }

        internal IEnumerable<string> FaxBlacklist
        {
            get { return GetSplit(_settings.GetSetting(FaxSettingKeys.FaxBlacklist).GetValue<string>()); }
        }

        internal IEnumerable<string> FaxWhitelist
        {
            get { return GetSplit(_settings.GetSetting(FaxSettingKeys.FaxWhitelist).GetValue<string>()); }
        }

        internal ReplaceDictionary ReplaceDictionary
        {
            get { return _settings.GetSetting(SettingKeys.ReplaceDictionary).GetValue<ReplaceDictionary>(); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FaxConfiguration"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public FaxConfiguration(IServiceProvider serviceProvider)
        {
            _settings = serviceProvider.GetService<ISettingsServiceInternal>();
            _settings.SettingChanged += _settings_SettingChanged;
        }

        #endregion

        #region Event-Handler

        void _settings_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            IEnumerable<SettingKey> keys = e.Keys.ToList();
            if (keys.All(x => x.Identifier != Identifier) || ConfigurationChanged == null)
            {
                return;
            }

            keys = keys.Where(x => x.Identifier == Identifier);
            List<string> changedKeys = new List<string>();

            foreach (SettingKey key in keys)
            {
                switch (key.Name)
                {
                    case "AlarmfaxParser":
                        changedKeys.Add("AlarmfaxParser");
                        break;
                    case "OCR.Path":
                        changedKeys.Add("OCR.Path");
                        break;
                    case "FaxPath":
                    case "ArchivePath":
                    case "AnalysisPath":
                        if (!changedKeys.Contains("FaxPaths"))
                        {
                            changedKeys.Add("FaxPaths");
                        }
                        break;
                }
            }

            var copy = ConfigurationChanged;
            if (copy != null)
            {
                ConfigChangedEventArgs args = new ConfigChangedEventArgs(changedKeys);
                copy(this, args);
            }

        }

        #endregion

        #region Methods

        private static string[] GetSplit(string input)
        {
            if (input == null)
            {
                return new string[0];
            }

            return input.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected override void DisposeCore()
        {
            _settings.SettingChanged -= _settings_SettingChanged;
            _settings = null;
        }

        #endregion

    }
}