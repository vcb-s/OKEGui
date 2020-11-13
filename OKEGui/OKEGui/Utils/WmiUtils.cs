using System;
using System.Management;

namespace OKEGui.Utils
{
    static class WmiUtils
    {
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
                Console.WriteLine(ex.Message);
            }
            return (int)(capacity / 1024.0 / 1024);
        }
        public static int GetAvailablePhysicalMemory()
        {
            int capacity = 0;
            try
            {
                foreach (ManagementObject mo1 in new ManagementClass("Win32_PerfFormattedData_PerfOS_Memory").GetInstances())
                    capacity += int.Parse(mo1.Properties["AvailableMBytes"].Value.ToString());
            }
            catch (Exception ex)
            {
                capacity = -1;
                Console.WriteLine(ex.Message);
            }
            return capacity;
        }
    }
}
