using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Docker.Secrets.Extensions.Configuration.Util;
using Microsoft.Extensions.Configuration;

namespace Docker.Secrets.Extensions.Configuration
{
    /// <summary>
    /// An docker secrets based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class DockerSecretsConfigurationProvider : ConfigurationProvider
    {
        DockerSecretsConfigurationSource Source { get; }

        private readonly IDictionary<string, Action<Stream, string>> _knownFileTypeProviders;
        
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="source">The settings.</param>
        public DockerSecretsConfigurationProvider(DockerSecretsConfigurationSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _knownFileTypeProviders = new Dictionary<string, Action<Stream, string>>
            {
                {"json", ReadJson}
            };
        }

        private void ReadJson(Stream stream, string baseKey)
        {
            var newData = JsonConfigurationFileParser.Parse(stream).ToDictionary(
                kv => $"{baseKey}{ConfigurationPath.KeyDelimiter}{kv.Key}",
                kv => kv.Value);

            foreach (var kv in newData)
            {
                Data[kv.Key] = kv.Value;
            }
        }

        private void ReadDefault(Stream stream, string key)
        {
            using (var streamReader = new StreamReader(stream))
            {
                var content = streamReader.ReadToEnd();
                Data.Add(key, content);
            }
        }

        private static string NormalizeKey(string key)
        {
            return key.Replace("__", ConfigurationPath.KeyDelimiter);
        }

        /// <summary>
        /// Loads the docker secrets.
        /// </summary>
        public override void Load()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var fileProvider = Source.FileProvider.Value;
            if (fileProvider == null) return;
            
            var secretsDir = fileProvider.GetDirectoryContents("/");

            if (!secretsDir.Exists && !Source.Optional)
            {
                throw new DirectoryNotFoundException("DockerSecrets directory doesn't exist and is not optional.");
            }
            
            var files = secretsDir.Where(file =>
                !file.IsDirectory &&
                (Source.IgnoreCondition == null ||
                 !Source.IgnoreCondition(file.Name)));

            foreach (var file in files)
            {
                var baseKey = NormalizeKey(file.GetFileName());
                using (var stream = file.CreateReadStream())
                {
                    if (_knownFileTypeProviders.TryGetValue(file.GetExtension(), out var action))
                    {
                        action(stream, baseKey);
                    }
                    else
                    {
                        ReadDefault(stream, baseKey);
                    }
                }
                
            }
        }
    }
    
    
}
