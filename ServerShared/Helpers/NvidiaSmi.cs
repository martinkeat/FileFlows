using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using FileFlows.ServerShared.Models.Schemas;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Util for parsing nvidia-smi output
/// </summary>
public class NvidiaSmi
{
    /// <summary>
    /// Gets the data from nvidia-smi
    /// </summary>
    /// <returns>A list of GPUs in the system</returns>
    public NvidiaGpu[] GetData()
    {
        try
        {
            string output = GetOutput();
            if (string.IsNullOrEmpty(output))
                return new NvidiaGpu[] { };

            var method = typeof(XmlSerializer).GetMethod("set_Mode",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if(method == null)
                return new NvidiaGpu[] { };
            method.Invoke(null, new object[] { 1 });

            XmlReader reader = new XmlTextReader(new StringReader(output));
            XmlSerializer serializer = new XmlSerializer(typeof(nvidia_smi_log));
            if (serializer.CanDeserialize(reader))
            {
                var info = serializer.Deserialize(reader) as nvidia_smi_log;
                if (info == null)
                    return new NvidiaGpu[] { };
                List<NvidiaGpu> gpus = new List<NvidiaGpu>();
                foreach (var agpu in info.gpu)
                {
                    var gpu = new NvidiaGpu()
                    {
                        Architecture = agpu.product_architecture,
                        Brand = agpu.product_brand,
                        Name = agpu.product_name,
                        FanSpeedPercent = ParsePercent(agpu.fan_speed)
                    };
                    if (agpu.temperature?.Any() == true &&
                        int.TryParse(Regex.Match(agpu.temperature[0].gpu_temp, @"[\d]+").Value, out int temp))
                        gpu.GpuTemperature = temp;
                    if (agpu.fb_memory_usage?.Any() == true)
                    {
                        string memTotal = agpu.fb_memory_usage[0].total;
                        if (memTotal.EndsWith("MiB"))
                            gpu.MemoryTotalMib = int.Parse(Regex.Match(memTotal, @"[\d]+").Value);
                        string memUsed = agpu.fb_memory_usage[0].used;
                        if (memUsed.EndsWith("MiB"))
                            gpu.MemoryUsedMib = int.Parse(Regex.Match(memUsed, @"[\d]+").Value);
                    }

                    if (agpu.utilization?.Any() == true)
                    {
                        gpu.UtilizationDecoderPercent = ParsePercent(agpu.utilization[0].decoder_util);
                        gpu.UtilizationEncoderPercent = ParsePercent(agpu.utilization[0].encoder_util);
                        gpu.UtilizationMemoryPercent = ParsePercent(agpu.utilization[0].memory_util);
                    }

                    if (agpu?.processes?.Any() == true)
                    {
                        gpu.Processes = new();
                        foreach (var p in agpu.processes)
                        {
                            gpu.Processes.Add(new()
                            {
                                Memory = ParseMib(p.used_memory),
                                ProcessName = p.process_name
                            });
                        }
                    }

                    gpus.Add(gpu);
                }

                return gpus.ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed calling nvidia-smi: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
        return new NvidiaGpu[] { };
    }


    private int ParsePercent(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
        if (Regex.IsMatch(value, @"[\d]+ %"))
            return int.Parse(Regex.Match(value, @"[\d]+").Value);
        return 0;
    }

    private int ParseMib(string value)
    {
        var rgx = Regex.Match(value, @"[\d]+");
        if (rgx.Success == false)
            return 0;
        if (int.TryParse(rgx.Value, out int mib))
            return mib;
        return 0;
    }

    private static string GetOutput()
    {
        #if(DEBUG)
        return sample;
        #else
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo();
            p.StartInfo.FileName = "nvidia-smi";
            p.StartInfo.ArgumentList.Add("-x");
            p.StartInfo.ArgumentList.Add("-q");
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output.Trim();
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed getting nvidia-smi information: " + ex.Message);
            return string.Empty;
        }
        #endif
    }

    private const string sample = @"<?xml version=""1.0"" ?>
<!DOCTYPE nvidia_smi_log SYSTEM ""nvsmi_device_v11.dtd"">
<nvidia_smi_log>
        <timestamp>Tue Nov  1 19:58:54 2022</timestamp>
        <driver_version>515.65.01</driver_version>
        <cuda_version>11.7</cuda_version>
        <attached_gpus>1</attached_gpus>
        <gpu id=""00000000:01:00.0"">
                <product_name>Quadro P620</product_name>
                <product_brand>Quadro</product_brand>
                <product_architecture>Pascal</product_architecture>
                <serial>0422318035396</serial>
                <uuid>GPU-31296037-cc98-cba3-11c0-26666ebb762c</uuid>
                <minor_number>0</minor_number>
                <vbios_version>86.07.51.00.04</vbios_version>
                <multigpu_board>No</multigpu_board>
                <board_id>0x100</board_id>
                <gpu_part_number>900-5G212-2540-000</gpu_part_number>
                <gpu_module_id>0</gpu_module_id>
                <fan_speed>36 %</fan_speed>
                <performance_state>P0</performance_state>
                <fb_memory_usage>
                        <total>2048 MiB</total>
                        <reserved>47 MiB</reserved>
                        <used>267 MiB</used>
                        <free>1732 MiB</free>
                </fb_memory_usage>
                <bar1_memory_usage>
                        <total>256 MiB</total>
                        <used>4 MiB</used>
                        <free>252 MiB</free>
                </bar1_memory_usage>
                <utilization>
                        <gpu_util>22 %</gpu_util>
                        <memory_util>15 %</memory_util>
                        <encoder_util>100 %</encoder_util>
                        <decoder_util>0 %</decoder_util>
                </utilization>
                <encoder_stats>
                        <session_count>0</session_count>
                        <average_fps>0</average_fps>
                        <average_latency>0</average_latency>
                </encoder_stats>
                <fbc_stats>
                        <session_count>0</session_count>
                        <average_fps>0</average_fps>
                        <average_latency>0</average_latency>
                </fbc_stats>
                <temperature>
                        <gpu_temp>33 C</gpu_temp>
                        <gpu_temp_max_threshold>103 C</gpu_temp_max_threshold>
                        <gpu_temp_slow_threshold>100 C</gpu_temp_slow_threshold>
                        <gpu_temp_max_gpu_threshold>N/A</gpu_temp_max_gpu_threshold>
                        <gpu_target_temperature>83 C</gpu_target_temperature>
                        <memory_temp>N/A</memory_temp>
                        <gpu_temp_max_mem_threshold>N/A</gpu_temp_max_mem_threshold>
                </temperature>
                <supported_gpu_target_temp>
                        <gpu_target_temp_min>65 C</gpu_target_temp_min>
                        <gpu_target_temp_max>97 C</gpu_target_temp_max>
                </supported_gpu_target_temp>
                <power_readings>
                        <power_state>P0</power_state>
                        <power_management>N/A</power_management>
                        <power_draw>N/A</power_draw>
                        <power_limit>N/A</power_limit>
                        <default_power_limit>N/A</default_power_limit>
                        <enforced_power_limit>N/A</enforced_power_limit>
                        <min_power_limit>N/A</min_power_limit>
                        <max_power_limit>N/A</max_power_limit>
                </power_readings>
                <processes>
                        <process_info>
                                <gpu_instance_id>N/A</gpu_instance_id>
                                <compute_instance_id>N/A</compute_instance_id>
                                <pid>41394</pid>
                                <type>C</type>
                                <process_name></process_name>
                                <used_memory>56 MiB</used_memory>
                        </process_info>
                        <process_info>
                                <gpu_instance_id>N/A</gpu_instance_id>
                                <compute_instance_id>N/A</compute_instance_id>
                                <pid>41782</pid>
                                <type>C</type>
                                <process_name></process_name>
                                <used_memory>207 MiB</used_memory>
                        </process_info>
                </processes>
        </gpu>
        <gpu id=""00000000:02:00.0"">
                <product_name>Quadro P620</product_name>
                <product_brand>Quadro</product_brand>
                <product_architecture>Pascal</product_architecture>
                <serial>0422318035396</serial>
                <uuid>GPU-31296037-cc98-cba3-11c0-26666ebb762c</uuid>
                <minor_number>0</minor_number>
                <vbios_version>86.07.51.00.04</vbios_version>
                <multigpu_board>No</multigpu_board>
                <board_id>0x100</board_id>
                <gpu_part_number>900-5G212-2540-000</gpu_part_number>
                <gpu_module_id>0</gpu_module_id>
                <fan_speed>36 %</fan_speed>
                <performance_state>P0</performance_state>
                <fb_memory_usage>
                        <total>2048 MiB</total>
                        <reserved>47 MiB</reserved>
                        <used>267 MiB</used>
                        <free>1732 MiB</free>
                </fb_memory_usage>
                <bar1_memory_usage>
                        <total>256 MiB</total>
                        <used>4 MiB</used>
                        <free>252 MiB</free>
                </bar1_memory_usage>
                <utilization>
                        <gpu_util>22 %</gpu_util>
                        <memory_util>15 %</memory_util>
                        <encoder_util>100 %</encoder_util>
                        <decoder_util>0 %</decoder_util>
                </utilization>
                <encoder_stats>
                        <session_count>0</session_count>
                        <average_fps>0</average_fps>
                        <average_latency>0</average_latency>
                </encoder_stats>
                <fbc_stats>
                        <session_count>0</session_count>
                        <average_fps>0</average_fps>
                        <average_latency>0</average_latency>
                </fbc_stats>
                <temperature>
                        <gpu_temp>83 C</gpu_temp>
                        <gpu_temp_max_threshold>103 C</gpu_temp_max_threshold>
                        <gpu_temp_slow_threshold>100 C</gpu_temp_slow_threshold>
                        <gpu_temp_max_gpu_threshold>N/A</gpu_temp_max_gpu_threshold>
                        <gpu_target_temperature>83 C</gpu_target_temperature>
                        <memory_temp>N/A</memory_temp>
                        <gpu_temp_max_mem_threshold>N/A</gpu_temp_max_mem_threshold>
                </temperature>
                <supported_gpu_target_temp>
                        <gpu_target_temp_min>65 C</gpu_target_temp_min>
                        <gpu_target_temp_max>97 C</gpu_target_temp_max>
                </supported_gpu_target_temp>
                <power_readings>
                        <power_state>P0</power_state>
                        <power_management>N/A</power_management>
                        <power_draw>N/A</power_draw>
                        <power_limit>N/A</power_limit>
                        <default_power_limit>N/A</default_power_limit>
                        <enforced_power_limit>N/A</enforced_power_limit>
                        <min_power_limit>N/A</min_power_limit>
                        <max_power_limit>N/A</max_power_limit>
                </power_readings>
                <processes>
                        <process_info>
                                <gpu_instance_id>N/A</gpu_instance_id>
                                <compute_instance_id>N/A</compute_instance_id>
                                <pid>41394</pid>
                                <type>C</type>
                                <process_name></process_name>
                                <used_memory>56 MiB</used_memory>
                        </process_info>
                        <process_info>
                                <gpu_instance_id>N/A</gpu_instance_id>
                                <compute_instance_id>N/A</compute_instance_id>
                                <pid>41782</pid>
                                <type>C</type>
                                <process_name></process_name>
                                <used_memory>207 MiB</used_memory>
                        </process_info>
                </processes>
        </gpu>
</nvidia_smi_log>";
}

/// <summary>
/// A NVIDIA GPU
/// </summary>
public class NvidiaGpu
{
    /// <summary>
    /// Gets or sets the GPUs name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the GPUs brand
    /// </summary>B
    public string Brand { get; set; }
    /// <summary>
    /// Gets or sets the GPUs architecture
    /// </summary>
    public string Architecture { get; set; }
    /// <summary>
    /// Gets or sets the current fan speed on the GPU
    /// </summary>
    public int FanSpeedPercent { get; set; }
    /// <summary>
    /// Gets or sets the used memory on the GPU
    /// </summary>
    public int MemoryUsedMib { get; set; }
    /// <summary>
    /// Gets or sets the total memory on the GPU
    /// </summary>
    public int MemoryTotalMib { get; set; }
    
    /// <summary>
    /// Gets or sets the decoder utilization percentage
    /// </summary>
    public int UtilizationDecoderPercent { get; set; }
    /// <summary>
    /// Gets or sets the encoder utilization percentage
    /// </summary>
    public int UtilizationEncoderPercent { get; set; }
    /// <summary>
    /// Gets or sets the memory utilization percentage
    /// </summary>
    public int UtilizationMemoryPercent { get; set; }
    
    /// <summary>
    /// Gets or sets the GPUs temperature in celsius
    /// </summary>
    public int GpuTemperature { get; set; }

    /// <summary>
    /// Gets or sets the processes running on the GPU
    /// </summary>
    public List<NvidiaGpuProcess> Processes { get; set; }
}

/// <summary>
/// A process using the nvidia gpu
/// </summary>
public class NvidiaGpuProcess
{
    /// <summary>
    /// Gets or sets the process name
    /// </summary>
    public string ProcessName { get; set; }
    /// <summary>
    /// Gets or sets the amount of memory being used
    /// </summary>
    public int Memory { get; set; }
}