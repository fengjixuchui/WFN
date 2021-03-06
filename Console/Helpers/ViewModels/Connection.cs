﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using System.Linq;
using System.Collections.Generic;
using System.Net;

namespace Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels
{
    public class Connection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Uses a cache for WMI information to avoid per-process costly queries.
        /// Warning: it has to be reset to null every time a new batch of processes will be handled, since it's not dynamically self-refreshed.
        /// </summary>
        public static Dictionary<int, string[]> LocalOwnerWMICache = null;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Connection(IPHelper.I_OWNER_MODULE ownerMod)
        {
            PID = ownerMod.OwningPid;
            IsNew = true;
            this._localPort = ownerMod.LocalPort.ToString();
            this.CreationTime = ownerMod.CreationTime;
            this._localAddress = ownerMod.LocalAddress;
            ResolveLocalIpToHostnameTask(this._localAddress);
            this._protocol = ownerMod.Protocol;
            this._remoteAddress = ownerMod.RemoteAddress;
            ResolveRemoteIpToHostnameTask(this._remoteAddress);
            this._remotePort = (ownerMod.RemotePort == -1 ? String.Empty : ownerMod.RemotePort.ToString());
            this.LastSeen = DateTime.Now;
            this._state = Enum.GetName(typeof(IPHelper.MIB_TCP_STATE), ownerMod.State);

            try
            {
                // Mainly for non-admin users, could use Process.GetProcessById for admins...
                var r = ProcessHelper.GetProcessOwnerWMI((int)ownerMod.OwningPid, ref LocalOwnerWMICache);
                Path = r[1] ?? "Unknown"; //FIXME: Move to resources!
                ProcName = r[0] ?? "Unknown"; //FIXME: Use something else?
            }
            catch
            {
                ProcName = "[Unknown or closed process]"; //FIXME: Move to resources!
                Path = "Unresolved"; //FIXME: Use something else?
            }

            if (ownerMod.OwnerModule == null)
            {
                if (PID == 0)
                {
                    ProcName = "System";
                    Owner = "System";
                    Path = "System";
                    Icon = IconHelper.GetIcon("System", true);
                }
                else
                {
                    Owner = "Unknown";
                    Icon = IconHelper.GetIcon("?error", true);
                }
            }
            else
            {
                Icon = ownerMod.OwnerModule.Icon;
                Owner = ownerMod.OwnerModule.ModuleName;
            }

            GroupKey = String.Format("{0} ({1}) - [{2}]", ProcName, Path, PID);
        }

        internal void ResolveRemoteIpToHostnameTask(string ip)
        {
            async void doResolve()
            {
                CachedIPHostEntry entry = await DnsResolver.ResolveIpAddress(ip).ConfigureAwait(true);
                RemoteHostName = entry.DisplayText;
            };
            doResolve();
        }
        internal void ResolveLocalIpToHostnameTask(string ip)
        {
            async void doResolve()
            {
                CachedIPHostEntry entry = await DnsResolver.ResolveIpAddress(ip).ConfigureAwait(true);
                LocalHostName = entry.DisplayText;
            };
            doResolve();
        }

        private bool _isAccessDenied;
        public bool IsAccessDenied
        {
            get { return _isAccessDenied; }
            set { _isAccessDenied = value; NotifyPropertyChanged(nameof(IsAccessDenied)); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; NotifyPropertyChanged(nameof(IsSelected)); }
        }

        private bool _isDead;
        public bool IsDead
        {
            get { return _isDead; }
            set { _isDead = value; NotifyPropertyChanged(nameof(IsDead)); }
        }

        private string _lastError;
        public string LastError
        {
            get { return _lastError; }
            set { _lastError = value; NotifyPropertyChanged(nameof(LastError)); }
        }

        public string GroupKey { get; private set; }
        public BitmapSource Icon { get; private set; }
        public uint PID { get; private set; }
        public string ProcName { get; private set; }
        public string Path { get; private set; }
        //public IEnumerable<FirewallHelper.Rule> FirewallRule { get { return FirewallHelper.GetMatchingRules(Path, Protocol, RemoteAddress, RemotePort, LocalPort, (Owner != ProcName ? new[] { Owner } : null), false).ToList(); } }

        internal void UpdateValues(IPHelper.I_OWNER_MODULE b)
        {
            //lvi.LocalAddress = b.LocalAddress;
            //lvi.Protocol = b.Protocol;
            if (this.RemoteAddress != b.RemoteAddress)
            {
                ResolveRemoteIpToHostnameTask(this._remoteAddress); 
            }

            var newPort = (b.RemotePort == -1 ? String.Empty : b.RemotePort.ToString());
            if (this.RemotePort != newPort)
            {
                this.RemotePort = newPort;
            }

            var newState = Enum.GetName(typeof(IPHelper.MIB_TCP_STATE), b.State);
            if (this.State != newState)
            {
                this.State = newState;
            }

            this.LastSeen = DateTime.Now;
        }

        private string _protocol;
        public string Protocol
        {
            get { return _protocol; }
            set { _protocol = value; NotifyPropertyChanged(nameof(Protocol)); }
        }

        private string _state;
        public string State
        {
            get { return _state; }
            set { _state = value; NotifyPropertyChanged(nameof(State)); }
        }

        private string _localAddress;
        public string LocalAddress
        {
            get { return _localAddress; }
            set { _localAddress = value; NotifyPropertyChanged(nameof(LocalAddress)); }
        }

        private string _localHostName;
        public string LocalHostName
        {
            get { return _localHostName; }
            set { _localHostName = value; NotifyPropertyChanged(nameof(LocalHostName)); }
        }


        private string _localPort;
        public string LocalPort
        {
            get { return _localPort; }
            set { _localPort = value; NotifyPropertyChanged(nameof(LocalPort)); }
        }

        private string _remoteAddress;
        public string RemoteAddress
        {
            get { return _remoteAddress; }
            set { _remoteAddress = value;
                NotifyPropertyChanged(nameof(RemoteAddress)); 
            }
        }

        private string _remoteHostName;
        public string RemoteHostName
        {
            get { return _remoteHostName; }
            set { _remoteHostName = value; NotifyPropertyChanged(nameof(RemoteHostName)); }
        }

        private string _remotePort;
        public string RemotePort
        {
            get { return _remotePort; }
            set { _remotePort = value; NotifyPropertyChanged(nameof(RemotePort)); }
        }

        public string Owner { get; private set; }
        public DateTime? CreationTime { get; set; }

        public DateTime LastSeen { get; set; }

        private bool _isDying;
        public bool IsDying
        {
            get { return _isDying; }
            set { _isDying = value; NotifyPropertyChanged(nameof(IsDying)); }
        }

        private bool _isNew;
        public bool IsNew
        {
            get { return _isNew; }
            set { _isNew = value; NotifyPropertyChanged(nameof(IsNew)); }
        }
    }
}
