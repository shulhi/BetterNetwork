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

        private ObservableCollection<NetworkProfile> _networkProfiles;
        public ObservableCollection<NetworkProfile>  NetworkProfiles
        {
            get { return _networkProfiles; }
            set
            {
                _networkProfiles = value; 
                NotifyOfPropertyChange(() => NetworkProfiles);
            }
        }

        private ObservableCollection<InterfaceProfile> ToDeleteInterfaces { get; set; }
        private ObservableCollection<NetworkProfile> ToDeleteNetworks { get; set; } 
        #endregion

        public AppViewModel()
        {
            InterfaceProfiles = new ObservableCollection<InterfaceProfile>();
            NetworkProfiles = new ObservableCollection<NetworkProfile>();
            ToDeleteInterfaces = new ObservableCollection<InterfaceProfile>();
            ToDeleteNetworks = new ObservableCollection<NetworkProfile>();

            // Load all interfaces
            GetInterfaceProfiles();
            GetNetworkProfiles();
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

        #region Network List Profiles
        public void GetNetworkProfiles()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles
                var profiles = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles");

                foreach (var profile in profiles.GetSubKeyNames())
                {
                    var name = profiles.OpenSubKey(profile).GetValue("ProfileName");
                    var path = profiles.Name + "\\" + profile;

                    NetworkProfiles.Add(new NetworkProfile {Name = (string)name, RegistryPath = path});
                }

                registry.Close();
                profiles.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void DeleteNetworkProfiles(NetworkProfile profile)
        {
            try
            {
                // Delete from registry
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var path = profile.RegistryPath.Substring(registry.Name.Length + 1);
                registry.DeleteSubKeyTree(path);

                // Then delete from Network Profiles collection so view get updated
                NetworkProfiles.Remove(profile);
            }
            catch (Exception e)
            {
                
            }
        }
        #endregion

        #region Events Handler
        public void InterfaceChecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            var profile = InterfaceProfiles.First(x => x.Name == (string)item.Content);
            if (!ToDeleteInterfaces.Contains(profile))
                ToDeleteInterfaces.Add(profile);
        }

        public void InterfaceUnchecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            var profile = InterfaceProfiles.First(x => x.Name == (string)item.Content);
            if (ToDeleteInterfaces.Contains(profile))
                ToDeleteInterfaces.Remove(profile);
        }

        public void NetworkChecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            var profile = NetworkProfiles.First(x => x.Name == (string)item.Content);
            if (!ToDeleteNetworks.Contains(profile))
                ToDeleteNetworks.Add(profile);
        }

        public void NetworkUnchecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            var profile = NetworkProfiles.First(x => x.Name == (string)item.Content);
            if (ToDeleteNetworks.Contains(profile))
                ToDeleteNetworks.Remove(profile);
        }

        public void Delete()
        {
            if (ToDeleteInterfaces.Count != 0)
            {
                foreach (var interfaceProfile in InterfaceProfiles)
                {
                    DeleteInterfaceProfiles(interfaceProfile);
                }
            }

            if (ToDeleteNetworks.Count != 0)
            {
                foreach (var networkProfile in ToDeleteNetworks)
                {
                    DeleteNetworkProfiles(networkProfile);
                }
            }
        }
        #endregion
    }
}
