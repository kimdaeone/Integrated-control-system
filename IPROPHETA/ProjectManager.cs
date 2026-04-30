// 클래스의 데이터 흐름을 관제합니다. 
// 저장 및 위젯의 탄생과 소멸, undo 기능을 추가합니다.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using IPROPHETA.Core.Models;

namespace IPROPHETA.Services
{
    /// <summary>
    /// 프로젝트 데이터의 생명주기와 변경 이력을 관리하는 싱글톤 서비스입니다.
    /// </summary>
    public class ProjectManager
    {
        private static ProjectManager _instance;
        public static ProjectManager Instance => _instance ??= new ProjectManager();

        // [Blueprint] 현재 메모리에 로드된 진실의 단일 원천 (SSOT)
        public ProjectModel CurrentProject { get; private set; } = new ProjectModel();

        // Undo/Redo 스택 (JSON 스냅샷 방식)
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private const int MaxHistory = 20;

        // 프로젝트 상태가 변경되었음을 UI에 알리는 이벤트 (예: 로드 완료, 새 프로젝트 등)
        public event Action ProjectChanged;

        private ProjectManager() { }

        /// <summary>
        /// 새로운 프로젝트를 생성하고 초기화합니다.
        /// </summary>
        public void CreateNewProject(string name)
        {
            CurrentProject = new ProjectModel { ProjectName = name };
            _undoStack.Clear();
            _redoStack.Clear();
            ProjectChanged?.Invoke();
        }

        /// <summary>
        /// 현재 상태를 스냅샷으로 찍어 기록합니다. (변경 직전에 호출)
        /// </summary>
        public void SaveHistory()
        {
            // 효율성을 위해 인덴트 없이 압축된 JSON 저장
            var snapshot = JsonConvert.SerializeObject(CurrentProject, Formatting.None);
            _undoStack.Push(snapshot);
            
            if (_undoStack.Count > MaxHistory) { /* 오래된 기록 삭제 로직 추가 가능 */ }
            _redoStack.Clear(); 
        }

        /// <summary>
        /// 과거 상태로 되돌립니다.
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            // 현재를 Redo에 보관 후 과거를 복원
            _redoStack.Push(JsonConvert.SerializeObject(CurrentProject, Formatting.None));
            RestoreSnapshot(_undoStack.Pop());
        }

        /// <summary>
        /// 취소했던 동작을 다시 실행합니다.
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            _undoStack.Push(JsonConvert.SerializeObject(CurrentProject, Formatting.None));
            RestoreSnapshot(_redoStack.Pop());
        }

        private void RestoreSnapshot(string json)
        {
            // TODO: Step 2.2에서 구현할 CustomConverter가 여기서 활약해야 함
            CurrentProject = JsonConvert.DeserializeObject<ProjectModel>(json);
            ProjectChanged?.Invoke(); // UI에 갱신 신호 발송
        }

        /// <summary>
        /// 프로젝트를 .gmn 파일로 물리적 저장합니다.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            var json = JsonConvert.SerializeObject(CurrentProject, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 파일을 읽어와 메모리에 프로젝트를 올립니다.
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var json = File.ReadAllText(filePath);
            RestoreSnapshot(json);
        }
    }
}