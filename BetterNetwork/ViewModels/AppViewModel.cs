using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BetterNetwork.Models;
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
        #region Properties
        private ObservableCollection<InterfaceProfile> _interfaceProfiles; 
        public ObservableCollection<InterfaceProfile> InterfaceProfiles
        {
            get { return _interfaceProfiles; } 
            set
            {
                _interfaceProfiles = value;
                NotifyOfPropertyChange(() => InterfaceProfiles);
            }
        }

        private ObservableCollection<InterfaceProfile> ToBeDeletedInterfaces { get; set; }
        #endregion

        public AppViewModel()
        {
            InterfaceProfiles = new ObservableCollection<InterfaceProfile>();
            ToBeDeletedInterfaces = new ObservableCollection<InterfaceProfile>();

            // Load all interfaces
            GetInterfaceProfiles();
        }

        #region Network Interfaces Profiles
        public void GetInterfaceProfiles()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var interfaces = registry.OpenSubKey(@"SOFTWARE\Microsoft\WlanSvc\Interfaces\");
                var profiles = interfaces.OpenSubKey(interfaces.GetSubKeyNames().First() + @"\Profiles");

                foreach (var profile in profiles.GetSubKeyNames())
                {
                    var metadata = profiles.OpenSubKey(profile + @"\Metadata").GetValue("Channel Hints");
                    var network = ExtractNetworkNames((byte[]) metadata);
                    var path = profiles.Name + "\\" + profile;

                    InterfaceProfiles.Add(new InterfaceProfile {Name = network, RegistryPath = path});
                }

                registry.Close();
                interfaces.Close();
                profiles.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void DeleteInterfaceProfiles(InterfaceProfile profile)
        {
            
        }
        
        private static string ExtractNetworkNames(byte[] metadata)
        {
            var position = 0;
            for (var i = 4; i < metadata.Length; i++)
            {
                if (metadata[i] == 0)
                {
                    position = i - 4;
                    break;
                }
            }

            return Encoding.ASCII.GetString((byte[])metadata, 4, position);
        }
        #endregion

        public void GetNetworkProfiles()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles
                var profiles = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles");

                foreach (var profile in profiles.GetSubKeyNames())
                {
                        
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region Events Handler
        public void NetworkChecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            var profile = InterfaceProfiles.First(x => x.Name == (string)item.Content);
            if (!ToBeDeletedInterfaces.Contains(profile))
                ToBeDeletedInterfaces.Add(profile);
        }

        public void NetworkUnchecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            var profile = InterfaceProfiles.First(x => x.Name == (string)item.Content);
            if (ToBeDeletedInterfaces.Contains(profile))
                ToBeDeletedInterfaces.Remove(profile);
        }

        public void Delete()
        {
            foreach (var beDeletedInterface in ToBeDeletedInterfaces)
            {
                DeleteInterfaceProfiles(beDeletedInterface);
            }
        }
        #endregion
    }
}
