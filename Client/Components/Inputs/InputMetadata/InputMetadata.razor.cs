using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;

namespace FileFlows.Client.Components.Inputs;

public partial class InputMetadata : Input<Dictionary<string, object>>
{
    private readonly List<Metadata> metadata = new ();
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Value?.Any() != true)
            return;

        var md = new Dictionary<string, Metadata>();
        var mdGeneral = new Metadata() { Name = "General" };
        md.Add("General", mdGeneral);
        var rgxVAS = new Regex(@"^(Video|Audio|Subtitle) ([\d]+)?");
        bool hasAudio2 = Value.Keys.Any(x => x.StartsWith("Audio 2"));
        bool hasSub2 = Value.Keys.Any(x => x.StartsWith("Subtitle 2"));
        foreach (var kv in Value)
        {
            if (kv.Value == null)
                continue;
            var match = rgxVAS.Match(kv.Key);
            var strValue = kv.Key.Contains("Bitrate") ? FormatBitrate(kv.Value) : kv.Value.ToString();
            if (match.Success == false)
            {
                mdGeneral.Values.Add(kv.Key, strValue);
                continue;
            }

            string key = match.Value.Trim();
            if (key == "Audio" && hasAudio2)
                key = "Audio 1";
            else if (key == "Subtitle" && hasSub2)
                key = "Subtitle 1";
            
            string subkey = kv.Key[(match.Value.Length)..].Trim();
            if (md.ContainsKey(key) == false)
                md.Add(key, new () { Name = key });
            md[key].Values.Add(subkey, strValue);
        }
        metadata.Clear();
        metadata.AddRange(md.Values);
    }

    private string FormatBitrate(object o)
    {
        if (o == null)
            return string.Empty;
        if (o is JsonElement je)
        {
            var bitrate = je.GetDouble();
            if (bitrate > 1_000_000)
                return Math.Round(bitrate / 1_000_000, 1) + " MBps" + " / " + Math.Round(bitrate / (1024 * 1024), 1) + " MiBps";
            if(bitrate > 1_000)
                return Math.Round(bitrate / 1_000, 1) + " KBps" + " / " + Math.Round(bitrate / 1_024, 1) + " KiBps";
            return bitrate + " bps";
        }

        return o.GetType().FullName;
    }

    class Metadata
    {
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; set; } = new ();
    }
}