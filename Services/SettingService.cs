using LabelAnnotator.Utilities;
using System.Diagnostics;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace LabelAnnotator.Services {
    public class SettingService {
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

        public string Format {
            get => GetItem("Format", SettingNames.FormatLTRB);
            set => SetItem("Format", value);
        }
    }
}
