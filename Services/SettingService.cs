using COCOAnnotator.Records.Enums;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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

        public static async Task<SettingService> Read() {
            if (File.Exists(SettingPath)) {
                using FileStream fileStream = File.OpenRead(SettingPath);
                byte[] bytes = File.ReadAllBytes(SettingPath);
                return await JsonSerializer.DeserializeAsync<SettingService>(fileStream).ConfigureAwait(false) ?? new SettingService();
            } else {
                return new SettingService();
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
