using NLog;
using System;
using System.Management;

namespace OKEGui.Utils
{
    static class WmiUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static int GetTotalPhysicalMemory()
        {
            long capacity = 0;
            try
            {
                foreach (ManagementObject mo1 in new ManagementClass("Win32_PhysicalMemory").GetInstances())
                    capacity += long.Parse(mo1.Properties["Capacity"].Value.ToString());
            }
            catch (Exception ex)
            {
                capacity = -1;
                Logger.Error(ex, "Failed to get total physical memory");
            }
            return (int)(capacity / 1024.0 / 1024);
        }

        public static int GetAvailablePhysicalMemory()
        {
            int capacity = 0;
            try
            {
                foreach (ManagementObject mo1 in new ManagementClass("Win32_OperatingSystem").GetInstances())
                    capacity += int.Parse(mo1.Properties["FreePhysicalMemory"].Value.ToString()) / 1024;
            }
            catch (Exception ex)
            {
                capacity = -1;
                Logger.Error(ex, "Failed to get available physical memory");
            }
            return capacity;
        }
    }
}