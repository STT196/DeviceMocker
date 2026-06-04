using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;

namespace DeviceMocker.Services
{
    public class JsonStorageService : IStorageService
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public async Task SaveAsync<T>(string path, T data)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(data, Options);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }

        public async Task<T?> LoadAsync<T>(string path)
        {
            if (!File.Exists(path))
                return default;

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, Options);
        }
    }
}
