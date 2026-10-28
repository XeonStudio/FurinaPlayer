using System;
using System.Text.Json.Serialization;

namespace SonicWave.Audio.Vst3;

public class Vst3PluginInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("vendor")]
    public string Vendor { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Fx";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "-";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("classId")]
    public string ClassId { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "VST3";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "工作正常"; // "工作正常" or "工作不正常"

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
