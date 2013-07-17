using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
using System.Security;

namespace BetterNetwork.ViewModels
{
    [Export(typeof(AppViewModel))]
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

        private bool _sixtyFourBitChecked;
        public bool SixtyFourBitChecked
        {
            get { return _sixtyFourBitChecked; } 
            set { _sixtyFourBitChecked = value; NotifyOfPropertyChange(() => SixtyFourBitChecked); }
        }

        private bool _thirtyTwoBitChecked;
        public bool ThirtyTwoBitChecked
        {
            get { return _thirtyTwoBitChecked; }
            set { _thirtyTwoBitChecked = value; NotifyOfPropertyChange(() => ThirtyTwoBitChecked); }
        }
        #endregion

        private readonly IWindowManager _windowManager;
        [ImportingConstructor]
        public AppViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
            InterfaceProfiles = new ObservableCollection<InterfaceProfile>();
            NetworkProfiles = new ObservableCollection<NetworkProfile>();
            ToDeleteInterfaces = new ObservableCollection<InterfaceProfile>();
            ToDeleteNetworks = new ObservableCollection<NetworkProfile>();

            SixtyFourBitChecked = true;
        }

        #region Network Interfaces Profiles
        public void GetInterfaceProfiles()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);

                var interfaces = registry.OpenSubKey(@"SOFTWARE\Microsoft\WlanSvc\Interfaces\");
                if (interfaces != null)
                {
                    var profiles = interfaces.OpenSubKey(interfaces.GetSubKeyNames().First() + @"\Profiles");

                    foreach (var profile in profiles.GetSubKeyNames())
                    {
                        var metadata = profiles.OpenSubKey(profile + @"\Metadata").GetValue("Channel Hints");
                        var network = ExtractNetworkNames((byte[]) metadata);
                        var path = profiles.Name + "\\" + profile;

                        InterfaceProfiles.Add(new InterfaceProfile {Name = network, RegistryPath = path});
                    }

                    interfaces.Close();
                    profiles.Close();
                }

                registry.Close();
            }
            catch (SecurityException e)
            {
                MessageBox.Show(e.Message + " Please run as administrator.", "Admin rights required");
            }
        }

        public void DeleteInterfaceProfiles(InterfaceProfile profile)
        {
            // TODO: Implement delete subkeytree
            try
            {
                // Delete from registry
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                var path = profile.RegistryPath.Substring(registry.Name.Length + 1);
                registry.DeleteSubKeyTree(path);

                // Then delete from Network Profiles collection so view get updated
                InterfaceProfiles.Remove(profile);
            }
            catch (SecurityException e)
            {
                MessageBox.Show(e.Message + " Please run as administrator.", "Admin rights required");
            }
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
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles
                var profiles = registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles");
                if(profiles != null)
                {
                    foreach (var profile in profiles.GetSubKeyNames())
                    {
                        var name = profiles.OpenSubKey(profile).GetValue("ProfileName");
                        var path = profiles.Name + "\\" + profile;

                        NetworkProfiles.Add(new NetworkProfile {Name = (string)name, RegistryPath = path});
                    }

                    profiles.Close();
                }

                registry.Close();
            }
            catch (SecurityException e)
            {
                MessageBox.Show(e.Message + " Please run as administrator to pull out network lists.", "Admin rights required");
            }
        }

        public void DeleteNetworkProfiles(NetworkProfile profile)
        {
            try
            {
                // Delete from registry
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                var path = profile.RegistryPath.Substring(registry.Name.Length + 1);
                registry.DeleteSubKeyTree(path);

                // Then delete from Network Profiles collection so view get updated
                NetworkProfiles.Remove(profile);
            }
            catch (SecurityException e)
            {
                MessageBox.Show(e.Message + " Please run as administrator.", "Admin rights required");
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

        public void LoadAll()
        {
            // Clear collections so no duplicate
            InterfaceProfiles.Clear();
            NetworkProfiles.Clear();
            ToDeleteInterfaces.Clear();
            ToDeleteNetworks.Clear();

            // Load all interfaces
            GetInterfaceProfiles();
            GetNetworkProfiles();
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

            MessageBox.Show("You will need to restart your system in order to make the changes effective.");
        }

        public void About()
        {
            _windowManager.ShowWindow(new AboutViewModel(_windowManager));
        }

        public void NavigateTo()
        {
            var url =
                @"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=XJCQMH4NUALP8&lc=MY&item_name=Shulhi%20Sapli&item_number=betternetwork&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted";
            Process.Start(new ProcessStartInfo(url));
        }
        #endregion
    }
}
