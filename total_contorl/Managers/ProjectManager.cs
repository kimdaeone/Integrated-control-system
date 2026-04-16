using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using GmnPlayer.Models;

namespace GmnPlayer.Managers
{
    /// <summary>
    /// .gmn 파일 (JSON 포맷) 기반 프로젝트 직렬화/역직렬화를 관장합니다.
    /// 앱 재시작 시 appsettings.json 기반으로 마지막 프로젝트를 오토 로드합니다.
    /// </summary>
    public class ProjectManager
    {
        private static readonly Lazy<ProjectManager> _instance = new Lazy<ProjectManager>(() => new ProjectManager());
        public static ProjectManager Instance => _instance.Value;

        public ProjectConfig? CurrentProject { get; set; }
        public string ActiveFilePath { get; private set; } = string.Empty;

        private readonly string _appSettingsPath = "appsettings.json";

        private ProjectManager()
        {
        }

        /// <summary>
        /// .gmn 파일을 읽어 CurrentProject에 로드합니다.
        /// </summary>
        public bool LoadProject(string filepath)
        {
            if (!File.Exists(filepath))
            {
                SystemLogger.Warning(LogCategory.SYSTEM, $"Project file not found: {filepath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filepath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<ProjectConfig>(json, options);

                if (config != null)
                {
                    CurrentProject = config;
                    ActiveFilePath = filepath;
                    SystemLogger.Info(LogCategory.SYSTEM, $"Successfully loaded project: {config.ProjectId}");
                    UpdateLastProjectAppSetting(filepath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                SystemLogger.Error(LogCategory.SYSTEM, $"Failed to load project : {filepath}", ex);
            }
            return false;
        }

        /// <summary>
        /// Editor 모드에서 수정한 정보를 .gmn 물리 파일로 저장(덮어쓰기)합니다.
        /// </summary>
        public bool SaveProject()
        {
            if (string.IsNullOrEmpty(ActiveFilePath)) return false;
            return SaveProject(ActiveFilePath);
        }

        public bool SaveProject(string filepath)
        {
            if (CurrentProject == null)
            {
                SystemLogger.Warning(LogCategory.SYSTEM, "No project is currently loaded to save.");
                return false;
            }

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(CurrentProject, options);
                File.WriteAllText(filepath, json);
                
                ActiveFilePath = filepath; // BUG FIX: Retain active file path so future saves don't fail silently.
                
                SystemLogger.Info(LogCategory.SYSTEM, $"Project saved successfully to {filepath}");
                UpdateLastProjectAppSetting(filepath);
                return true;
            }
            catch (Exception ex)
            {
                SystemLogger.Error(LogCategory.SYSTEM, $"Failed to save project. Path: {filepath}", ex);
                return false;
            }
        }

        /// <summary>
        /// 프로그램 시작 시 가장 최근에 열었던 파일을 자동으로 로드합니다.
        /// </summary>
        public bool LoadLastProject()
        {
            try
            {
                if (File.Exists(_appSettingsPath))
                {
                    string settingsJson = File.ReadAllText(_appSettingsPath);
                    var settings = JsonNode.Parse(settingsJson);
                    
                    string? lastPath = settings?["LastProjectPath"]?.GetValue<string>();
                    
                    if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
                    {
                        SystemLogger.Info(LogCategory.SYSTEM, $"Auto-restoring last project: {lastPath}");
                        return LoadProject(lastPath);
                    }
                }
            }
            catch (Exception ex)
            {
                SystemLogger.Warning(LogCategory.SYSTEM, $"Failed to read appsettings.json for auto-restore: {ex.Message}");
            }
            
            SystemLogger.Info(LogCategory.SYSTEM, "No last project found. Ready for new project loading.");
            return false;
        }

        private void UpdateLastProjectAppSetting(string filepath)
        {
            try
            {
                // Create or Update
                JsonObject settingsObj;
                if (File.Exists(_appSettingsPath))
                {
                    string existing = File.ReadAllText(_appSettingsPath);
                    settingsObj = JsonNode.Parse(existing)!.AsObject();
                }
                else
                {
                    settingsObj = new JsonObject();
                }

                settingsObj["LastProjectPath"] = filepath;
                
                File.WriteAllText(_appSettingsPath, settingsObj.ToJsonString(new JsonSerializerOptions{ WriteIndented = true }));
            }
            catch(Exception ex)
            {
                SystemLogger.Warning(LogCategory.SYSTEM, $"Failed to update last project path in appsettings: {ex.Message}");
            }
        }
    }
}
