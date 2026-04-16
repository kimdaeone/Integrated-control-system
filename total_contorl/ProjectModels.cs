using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GmnPlayer.Models
{
    public class ProjectConfig
    {
        [Category("General")]
        [DisplayName("Project ID")]
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = "default_project";

        [Category("Appearance")]
        [DisplayName("Resolution Width")]
        [JsonPropertyName("resolutionWidth")]
        public int ResolutionWidth { get; set; } = 1920;

        [Category("Appearance")]
        [DisplayName("Resolution Height")]
        [JsonPropertyName("resolutionHeight")]
        public int ResolutionHeight { get; set; } = 1080;

        [Category("Appearance")]
        [DisplayName("Background Color Hex")]
        [JsonPropertyName("backgroundColor")]
        public string BackgroundColor { get; set; } = "#191919";

        [Browsable(false)]
        [JsonPropertyName("deviceNodes")]
        public List<DeviceNode> DeviceNodes { get; set; } = new List<DeviceNode>();

        [JsonPropertyName("widgets")]
        public List<WidgetModel> Widgets { get; set; } = new List<WidgetModel>();
        
        [Category("Appearance")]
        [DisplayName("Background Image Path")]
        [Editor(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [JsonPropertyName("backgroundImagePath")]
        public string BackgroundImagePath { get; set; } = string.Empty;

        [JsonPropertyName("presets")]
        public List<PresetState> Presets { get; set; } = new List<PresetState>();
    }

    public enum WidgetType
    {
        Button,
        GridCanvas,
        SourceList,
        LayoutChanger,
        PresetMinimap
    }

    public class SourceItemNode
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;
    }

    public class DeviceNode
    {
        [Category("Identity")]
        [DisplayName("Device ID")]
        [Description("Unique identifier for the generic device mapping.")]
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "unknown_device";

        [Category("Network")]
        [DisplayName("IP Address")]
        [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Invalid IP Address format.")]
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = "127.0.0.1";

        private int _port = 8080;
        
        [Category("Network")]
        [DisplayName("Port Number")]
        [Range(0, 65535, ErrorMessage = "Port must be bounded between 0 and 65535")]
        [JsonPropertyName("port")]
        public int Port
        {
            get => _port;
            set => _port = (value > 0 && value <= 65535) ? value : 8080;
        }

        [Category("Hardware")]
        [DisplayName("Protocol Strategy")]
        [Description("Available Strategies: AVCIT, RS232, GenericTcp")]
        [JsonPropertyName("protocolStrategy")]
        public string ProtocolStrategy { get; set; } = "GenericTcp";
    }

    public class WidgetModel
    {
        [Category("General")]
        [ReadOnly(true)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Category("General")]
        [DisplayName("Component Type")]
        [ReadOnly(true)] // 동결 처리
        [JsonPropertyName("type")]
        public WidgetType Type { get; set; } = WidgetType.Button;

        [Category("Visual")]
        [DisplayName("Image Asset Path")]
        [Editor(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [JsonPropertyName("imagePath")]
        public string ImagePath { get; set; } = string.Empty;

        private int _x;
        [Category("Transform")]
        [DisplayName("X Position")]
        [JsonPropertyName("x")]
        public int X
        {
            get => _x;
            set => _x = Math.Max(0, value);
        }

        private int _y;
        [Category("Transform")]
        [DisplayName("Y Position")]
        [JsonPropertyName("y")]
        public int Y
        {
            get => _y;
            set => _y = Math.Max(0, value);
        }

        private int _w = 100;
        [Category("Transform")]
        [DisplayName("Width (W)")]
        [JsonPropertyName("w")]
        public int W
        {
            get => _w;
            set => _w = Math.Max(10, value);
        }

        private int _h = 100;
        [Category("Transform")]
        [DisplayName("Height (H)")]
        [JsonPropertyName("h")]
        public int H
        {
            get => _h;
            set => _h = Math.Max(10, value);
        }

        [Category("Visual")]
        [DisplayName("Z-Index Overlap")]
        [JsonPropertyName("zIndex")]
        public int ZIndex { get; set; } = 0;

        [Category("Behavior")]
        [DisplayName("Is Static Guide")]
        [JsonPropertyName("isStatic")]
        public bool IsStatic { get; set; } = false;

        [Category("Mapping")]
        [DisplayName("Target Device ID")]
        [Description("Which device captures the interaction commands mapping.")]
        [JsonPropertyName("targetDeviceId")]
        public string TargetDeviceId { get; set; } = string.Empty;

        [Category("Mapping")]
        [DisplayName("Execution Command")]
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [Category("Grid Constraints")]
        [DisplayName("Is Grid Dependent")]
        [JsonPropertyName("isGridDependent")]
        public bool IsGridDependent { get; set; } = false;

        [Category("Grid Constraints")]
        [DisplayName("Internal Cell Row")]
        [JsonPropertyName("cellRow")]
        public int CellRow { get; set; } = 0;

        [Category("Grid Constraints")]
        [DisplayName("Internal Cell Col")]
        [JsonPropertyName("cellCol")]
        public int CellCol { get; set; } = 0;

        [Category("Engine Rules")]
        [DisplayName("Grid Rows (N)")]
        [JsonPropertyName("gridRows")]
        public int GridRows { get; set; } = 2; // Default 2

        [Category("Engine Rules")]
        [DisplayName("Grid Cols (M)")]
        [JsonPropertyName("gridCols")]
        public int GridCols { get; set; } = 4; // Default 4

        [Category("Engine Rules")]
        [DisplayName("Source Entries")]
        [JsonPropertyName("sourceItems")]
        public List<SourceItemNode> SourceItems { get; set; } = new List<SourceItemNode>();
    }

    public class PresetState
    {
        [JsonPropertyName("presetId")]
        public string PresetId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Scenario Preset";

        [JsonPropertyName("deviceCommands")]
        public Dictionary<string, string> DeviceCommands { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("gridMappings")]
        public Dictionary<string, string> GridMappings { get; set; } = new Dictionary<string, string>();
    }
}
