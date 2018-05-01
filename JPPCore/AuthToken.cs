using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace JPP.Core
{
    class AuthToken
    {
        public string MachineID
        {
            get
            {
                if (_MachineID == String.Empty)
                {
                    return (from nic in NetworkInterface.GetAllNetworkInterfaces() where nic.OperationalStatus == OperationalStatus.Up select nic.GetPhysicalAddress().ToString()).FirstOrDefault();
                } else
                {
                    return _MachineID;
                }
            }
            set
            {
                _MachineID = value;
            }
        }

        private string _MachineID;

        public DateTime ValidUntil { get; set; } 
    }    
}
