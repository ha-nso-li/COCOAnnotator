using COCOAnnotator.Records.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace COCOAnnotator.Services {
    public class SettingService {
        private SettingColors _Color;
        public SettingColors Color {
            get => _Color;
            set {
                NeedSave |= _Color != value;
                _Color = value;
            }
        }

        private string _SupportedFormats;
        public string SupportedFormats {
            get => _SupportedFormats;
            set {
                NeedSave |= !value.Equals(_SupportedFormats, StringComparison.OrdinalIgnoreCase);
                _SupportedFormats = value;
            }
        }

        public ISet<string> SetSupportedFormats => new SortedSet<string>(SupportedFormats.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);

        public SettingService() {
            _Color = SettingColors.Fixed;
            _SupportedFormats = ".jpg;.jpeg;.png;.tif;.webp;.avif";
            NeedSave = false;
        }

        private bool NeedSave;
        private const string SettingPath = "setting.json";

        public static async Task<SettingService> Read() {
            if (File.Exists(SettingPath)) {
                using FileStream fileStream = File.OpenRead(SettingPath);
                return await JsonSerializer.DeserializeAsync<SettingService>(fileStream).ConfigureAwait(false) ?? new();
            } else {
                return new();
            }
        }

        public async Task Write() {
            if (!NeedSave) return;
            NeedSave = false;
            using FileStream fileStream = File.Create(SettingPath);
            await JsonSerializer.SerializeAsync(fileStream, this).ConfigureAwait(false);
        }
    }
}
