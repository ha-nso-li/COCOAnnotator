using COCOAnnotator.Records.Enums;
using System;
using System.IO;
using System.Text.Json;

namespace COCOAnnotator.Services {
    public class SettingService {
        private SettingColors _Color;
        public SettingColors Color {
            get => _Color;
            set {
                if (_Color != value) {
                    _Color = value;
                    NeedSave = true;
                }
            }
        }

        private bool NeedSave = false;
        private const string SettingPath = "setting.json";

        public static SettingService Read() {
            if (File.Exists(SettingPath)) {
                byte[] bytes = File.ReadAllBytes(SettingPath);
                ReadOnlySpan<byte> JsonSpan = new ReadOnlySpan<byte>(bytes);
                return JsonSerializer.Deserialize<SettingService>(JsonSpan);
            } else {
                return new SettingService();
            }
        }

        public void Write() {
            if (!NeedSave) return;
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(this);
            File.WriteAllBytes(SettingPath, bytes);
            NeedSave = false;
        }
    }
}
