using COCOAnnotator.Records.Enums;
using System;
using System.Diagnostics;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace COCOAnnotator.Services {
    public class SettingService {
        #region 설정 관리 내부 메서드
        private YamlStream? _YamlStream;
        private string SettingPath => Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) ?? "", "setting.yml");
        private YamlStream YamlStream {
            get {
                if (_YamlStream is null) {
                    if (File.Exists(SettingPath)) {
                        using StreamReader file = new StreamReader(SettingPath);
                        _YamlStream = new YamlStream();
                        _YamlStream.Load(file);
                        if (_YamlStream.Documents.Count == 0 || !(_YamlStream.Documents[0].RootNode is YamlMappingNode)) {
                            _YamlStream.Documents.Insert(0, new YamlDocument(new YamlMappingNode()));
                        }
                    } else {
                        _YamlStream = new YamlStream { new YamlDocument(new YamlMappingNode()) };
                    }
                }

                return _YamlStream;
            }
        }
        private string GetItem(string Key, string DefaultValue) {
            YamlMappingNode root = (YamlMappingNode)YamlStream.Documents[0].RootNode;
            if (root.Children.TryGetValue(Key, out YamlNode? value1) && value1 is YamlScalarNode value2) {
                return (string?)value2 ?? "";
            } else {
                root.Children[Key] = DefaultValue;
                return DefaultValue;
            }
        }
        private void SetItem(string Key, string Value) {
            YamlMappingNode root = (YamlMappingNode)YamlStream.Documents[0].RootNode;
            bool NeedSave = false;
            if (root.Children.TryGetValue(Key, out YamlNode? currentValue)) {
                if (!currentValue.Equals((YamlNode)Value)) {
                    root.Children[Key] = Value;
                    NeedSave = true;
                }
            } else {
                root.Children[Key] = Value;
                NeedSave = true;
            }
            if (NeedSave) {
                using StreamWriter file = new StreamWriter(SettingPath);
                YamlStream.Save(file);
            }
        }
        #endregion

        public SettingFormats Format {
            get {
                string formatStr = GetItem(nameof(Format), SettingFormats.LTRB.ToString());
                Enum.TryParse(formatStr, out SettingFormats format);
                return format;
            }
            set => SetItem(nameof(Format), value.ToString());
        }
        public SettingColors Color {
            get {
                string colorStr = GetItem(nameof(Color), SettingColors.Fixed.ToString());
                Enum.TryParse(colorStr, out SettingColors color);
                return color;
            }
            set => SetItem(nameof(Color), value.ToString());
        }
    }
}
