using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BetterNetwork.Models;
using Caliburn.Micro;
using Microsoft.Win32;
using System.Linq;
using System.Text;
using System.Security;
using System;

namespace BetterNetwork.ViewModels
{
    [Export(typeof(AppViewModel))]
    public partial class AppViewModel : PropertyChangedBase
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

        private ObservableCollection<InterfaceProfile> _toDeleteInterfaces;
        public ObservableCollection<InterfaceProfile> ToDeleteInterfaces
        {
            get { return _toDeleteInterfaces; }
            set 
            {
                _toDeleteInterfaces = value; 
                NotifyOfPropertyChange(()=> ToDeleteInterfaces);
            }

        }

        private ObservableCollection<NetworkProfile> _toDeleteNetworks; 
        public ObservableCollection<NetworkProfile> ToDeleteNetworks
        {
            get { return _toDeleteNetworks; }
            set 
            { 
                _toDeleteNetworks = value; 
                NotifyOfPropertyChange(() => ToDeleteNetworks); 
            }
        }

        public bool CanDelete
        {
            get { return ToDeleteNetworks.Count != 0 || ToDeleteInterfaces.Count != 0; }
        }

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

            ToDeleteInterfaces.CollectionChanged += (sender, args) => NotifyOfPropertyChange(() => CanDelete);
            ToDeleteNetworks.CollectionChanged += (sender, args) => NotifyOfPropertyChange(() => CanDelete);
        }

        #region Interfaces Profiles
        public void GetInterfaces()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                var interfaces = registry.OpenSubKey(@"SOFTWARE\Microsoft\WlanSvc\Interfaces\");

                if (interfaces == null) return;

                foreach (var subkey in interfaces.GetSubKeyNames())
                {
                    var profile = interfaces.OpenSubKey(subkey);
                    if (profile == null) continue;

                    var items = (string[]) profile.GetValue("ProfileList");
                    if (items == null) continue;
                    
                    foreach (var interfaceProfile in items.Select(item => GetInterfaceMetadata(subkey, item)).Where(interfaceProfile => interfaceProfile != null))
                    {
                        InterfaceProfiles.Add(interfaceProfile);
                    }

                    profile.Close();
                }

                registry.Close();
                interfaces.Close();
            }
            catch (Exception ex)
            {
                if (ex is SecurityException || ex is UnauthorizedAccessException)
                    MessageBox.Show(ex.Message + " Please run as administrator.", "Admin rights required");
                if (ex is ArgumentException || ex is NullReferenceException)
                    MessageBox.Show(ex.Message, "Could not find registry key");
            }
        }

        private InterfaceProfile GetInterfaceMetadata(string interfaceGuid, string profileGuid)
        {
            var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
            var metadata = registry.OpenSubKey(@"SOFTWARE\Microsoft\WlanSvc\Interfaces\" + interfaceGuid + @"\Profiles\" + profileGuid + @"\Metadata");
            
            if (metadata != null)
            {
                var channel = (byte[])metadata.GetValue("Channel Hints");
                if (channel != null)
                {
                    var name = ExtractNetworkNames(channel);

                    return new InterfaceProfile
                        {
                            Name = name, 
                            RegistryPath = metadata.Name.Substring(0, metadata.Name.Length - @"\Metadata".Length),
                            InterfaceGuid = interfaceGuid,
                            ProfileGuid = profileGuid
                        };
                }
            }

            return null;
        }

        public void DeleteInterfaceProfiles(InterfaceProfile profile)
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                registry.DeleteSubKeyTree(profile.RegistryPath.Substring(registry.Name.Length+1), false);

                registry.Close();
            }
            catch (Exception ex)
            {
                if (ex is SecurityException || ex is UnauthorizedAccessException)
                    MessageBox.Show(ex.Message + " Please run as administrator.", "Admin rights required");
                if (ex is ArgumentException || ex is NullReferenceException)
                    MessageBox.Show(ex.Message, "Could not find registry key");
            }
        }

        private void UpdateInterfaceProfileListKey()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                var grouped = InterfaceProfiles.GroupBy(i => i.InterfaceGuid, p => p.ProfileGuid,
                                                        (key, g) => new { InterfaceGuid = key, ProfileGuids = g.ToList() });
                foreach (var g in grouped)
                {
                    var profile = registry.OpenSubKey(@"SOFTWARE\Microsoft\WlanSvc\Interfaces\" + g.InterfaceGuid, true);
                    if(profile != null)
                        profile.SetValue("ProfileList", g.ProfileGuids.ToArray());
                }

                registry.Close();
            }
            catch (Exception ex)
            {
                if (ex is SecurityException || ex is UnauthorizedAccessException)
                    MessageBox.Show(ex.Message + " Please run as administrator.", "Admin rights required");
                if (ex is ArgumentException || ex is NullReferenceException)
                    MessageBox.Show(ex.Message, "Could not find registry key");
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

        #region Network List
        public void GetNetworks()
        {
            try
            {
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                var unmanagedKeys =
                    registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Signatures\Unmanaged");

                if (unmanagedKeys != null)
                {
                    foreach (var unmanaged in unmanagedKeys.GetSubKeyNames())
                    {
                        var network = GetNetworkMetadata(unmanaged);

                        if(network != null)
                            NetworkProfiles.Add(network);
                    }
                    unmanagedKeys.Close();
                }
            }
            catch (Exception ex)
            {
                if (ex is SecurityException || ex is UnauthorizedAccessException)
                    MessageBox.Show(ex.Message + " Please run as administrator.", "Admin rights required");
                if (ex is ArgumentException || ex is NullReferenceException)
                    MessageBox.Show(ex.Message, "Could not find registry key");
            }
        }

        private NetworkProfile GetNetworkMetadata(string unmanaged)
        {
            var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
            var unmanagedSubKeys =
                registry.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Signatures\Unmanaged\" + unmanaged);

            if (unmanagedSubKeys != null)
            {
                var desc = unmanagedSubKeys.GetValue("Description");
                var guid = unmanagedSubKeys.GetValue("ProfileGuid");
                var signaturePath = unmanagedSubKeys.Name;
                var profilepath = registry.Name + @"\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\" + guid;

                var network = new NetworkProfile()
                {
                    Name = (string)desc,
                    ProfileGuid = (string)guid,
                    SignatureRegistryPath = signaturePath,
                    ProfileRegistryPath = profilepath,
                    ManageType = "Unmanaged"
                };

                unmanagedSubKeys.Close();

                return network;
            }

            return null;
        }

        public void DeleteNetworkProfiles(NetworkProfile profile)
        {
            try
            {
                // Delete from registry
                var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, SixtyFourBitChecked ? RegistryView.Registry64 : RegistryView.Registry32);
                registry.DeleteSubKeyTree(profile.ProfileRegistryPath.Substring(registry.Name.Length + 1), false);
                registry.DeleteSubKeyTree(profile.SignatureRegistryPath.Substring(registry.Name.Length + 1), false);
            }
            catch (Exception ex)
            {
                if (ex is SecurityException || ex is UnauthorizedAccessException)
                    MessageBox.Show(ex.Message + " Please run as administrator.", "Admin rights required");
                if (ex is ArgumentException || ex is NullReferenceException)
                    MessageBox.Show(ex.Message, "Could not find registry key");
            }
        }
        #endregion

        #region Events Handler
        public void InterfaceChecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            if (item == null) return;
            var profile = item.DataContext as InterfaceProfile;
            if (profile != null && !ToDeleteInterfaces.Contains(profile))
            {
                ToDeleteInterfaces.Add(profile);
            }
        }

        public void InterfaceUnchecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            if (item == null) return;
            var profile = item.DataContext as InterfaceProfile;
            if (profile != null && ToDeleteInterfaces.Contains(profile))
            {
                ToDeleteInterfaces.Remove(profile);
            }
        }

        public void NetworkChecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            if (item == null) return;
            var profile = item.DataContext as NetworkProfile;
            if (profile != null && !ToDeleteNetworks.Contains(profile))
            {
                ToDeleteNetworks.Add(profile);
            }
        }

        public void NetworkUnchecked(RoutedEventArgs e)
        {
            var item = e.Source as CheckBox;

            if (item == null) return;
            var profile = item.DataContext as NetworkProfile;
            if (profile != null && ToDeleteNetworks.Contains(profile))
            {
                ToDeleteNetworks.Remove(profile);
            }
        }
        #endregion

        #region Commands
        public void LoadAll()
        {
            // Clear collections so no duplicate
            InterfaceProfiles.Clear();
            NetworkProfiles.Clear();
            ToDeleteInterfaces.Clear();
            ToDeleteNetworks.Clear();

            // Load all interfaces
            GetInterfaces();
            GetNetworks();
        }

        public void Delete()
        {
            if (ToDeleteInterfaces.Count != 0)
            {
                foreach (var interfaceProfile in ToDeleteInterfaces)
                {
                    DeleteInterfaceProfiles(interfaceProfile);
                    InterfaceProfiles.Remove(interfaceProfile);
                }
            }

            UpdateInterfaceProfileListKey();

            if (ToDeleteNetworks.Count != 0)
            {
                foreach (var networkProfile in ToDeleteNetworks)
                {
                    DeleteNetworkProfiles(networkProfile);
                    NetworkProfiles.Remove(networkProfile);
                }
            }

            // Clear trackers
            ToDeleteInterfaces.Clear();
            ToDeleteNetworks.Clear();
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
