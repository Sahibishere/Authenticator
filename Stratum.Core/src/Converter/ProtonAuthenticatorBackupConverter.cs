// Copyright (C) 2025 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stratum.Core.Backup;
using Stratum.Core.Entity;
using Stratum.Core.Util;

namespace Stratum.Core.Converter
{
    public class ProtonAuthenticatorBackupConverter : BackupConverter
    {
        public ProtonAuthenticatorBackupConverter(IIconResolver iconResolver) : base(iconResolver)
        {
        }

        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Never;

        public override Task<ConversionResult> ConvertAsync(byte[] data, string password = null)
        {
            var json = Encoding.UTF8.GetString(data);
            var export = JsonConvert.DeserializeObject<ProtonBackup>(json);

            if (export.Version != 1)
            {
                throw new ArgumentException($"Unsupported backup version {export.Version}");
            }

            var authenticators = new List<Authenticator>();
            var failures = new List<ConversionFailure>();

            foreach (var entry in export.Entries)
            {
                Authenticator auth;

                try
                {
                    auth = entry.Content.Convert(IconResolver);
                    auth.Validate();
                }
                catch (Exception e)
                {
                    failures.Add(new ConversionFailure { Description = entry.Content.Name, Error = e.Message });
                    continue;
                }

                authenticators.Add(auth);
            }

            var backup = new Backup.Backup { Authenticators = authenticators };
            var result = new ConversionResult { Failures = failures, Backup = backup };

            return Task.FromResult(result);
        }

        private sealed class ProtonBackup
        {
            [JsonProperty(PropertyName = "version")]
            public int Version { get; set; }

            [JsonProperty(PropertyName = "entries")]
            public List<Entry> Entries { get; set; }
        }

        private sealed class Entry
        {
            [JsonProperty(PropertyName = "content")]
            public Content Content { get; set; }
        }

        private sealed class Content
        {
            [JsonProperty(PropertyName = "uri")]
            public string Uri { get; set; }

            [JsonProperty(PropertyName = "entry_type")]
            public string EntryType { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                switch (EntryType)
                {
                    case "Totp":
                    {
                        var auth = UriParser.ParseStandardUri(Uri, iconResolver).Authenticator;

                        if (auth.Username == null && auth.Issuer != Name ||
                            auth.Username != null && auth.Username != Name)
                        {
                            auth.Username = Name.Truncate(Authenticator.UsernameMaxLength);
                        }

                        return auth;
                    }

                    case "Steam":
                    {
                        var secret = Uri["steam://".Length..];

                        return new Authenticator
                        {
                            Issuer = Name.Truncate(Authenticator.IssuerMaxLength),
                            Username = null,
                            Type = AuthenticatorType.SteamOtp,
                            Algorithm = Authenticator.DefaultAlgorithm,
                            Digits = AuthenticatorType.SteamOtp.GetDefaultDigits(),
                            Period = AuthenticatorType.SteamOtp.GetDefaultPeriod(),
                            Icon = iconResolver.FindServiceKeyByName(Name),
                            Secret = SecretUtil.Normalise(secret, AuthenticatorType.SteamOtp)
                        };
                    }

                    default:
                        throw new ArgumentException($"Entry Type '{EntryType}' not supported");
                }
            }
        }
    }
}