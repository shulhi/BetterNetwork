using Caliburn.Micro;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterNetwork.ViewModels
{
    public class AppViewModel : PropertyChangedBase
    {
        public void GetAvailableNetworks()
        {
            var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var interfaces = registry.OpenSubKey(@"SOFTWARE\Microsoft\WlanSvc\Interfaces\");
            var profiles = interfaces.OpenSubKey(interfaces.GetSubKeyNames().First() + @"\Profiles");

            foreach (var profile in profiles.GetSubKeyNames())
            {
                var metadata = profiles.OpenSubKey(profile + @"\Metadata").GetValue("Channel Hints");
                var network = ExtractNetworkNames((byte[]) metadata);
            }
        }
        
        private static string ExtractNetworkNames(byte[] metadata)
        {
            var position = 0;
            for (var i = 4; i < metadata.Length; i++)
            {
                if (metadata[i] == 0)
                {
                    position = i;
                    break;
                }
            }

            return Encoding.ASCII.GetString((byte[])metadata, 4, position - 4);
        }
    }
}
