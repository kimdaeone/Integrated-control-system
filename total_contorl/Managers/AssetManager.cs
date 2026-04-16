using System;
using System.IO;

namespace GmnPlayer.Managers
{
    public static class AssetManager
    {
        public static string CopyAndGetRelativePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)) return string.Empty;
            if (!File.Exists(absolutePath)) return absolutePath;

            // 이미 .assets 내부의 파일이라면 무시
            string projectDir = ProjectManager.Instance.ActiveFilePath;
            if (string.IsNullOrEmpty(projectDir))
            {
                projectDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                projectDir = Path.GetDirectoryName(projectDir) ?? AppDomain.CurrentDomain.BaseDirectory;
            }

            string assetsDir = Path.Combine(projectDir, ".assets");

            // 절대 경로가 이미 assetsDir 안에 있다면 그대로 리턴 (결과적으로 .gmn 입장에서는 상대경로로 기입되도록 수정해야 함)
            // 우선 이 함수는 무조건 ".assets/파일명" 형태의 상대경로만 리턴하게 만든다!
            // Gmn-Player 기동 시 이 상대경로를 바탕으로 로드 절차를 거친다.

            if (!Directory.Exists(assetsDir))
            {
                Directory.CreateDirectory(assetsDir);
            }

            string fileName = Path.GetFileName(absolutePath);
            string destPath = Path.Combine(assetsDir, fileName);

            // 파일이 다르면 덮어씀
            if (!absolutePath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(absolutePath, destPath, true);
            }

            // ".assets/filename.png" 형태의 문자열 리턴
            return $".assets\\{fileName}";
        }

        public static string GetAbsolutePath(string relativeImagePath)
        {
            if (string.IsNullOrWhiteSpace(relativeImagePath)) return string.Empty;
            
            // 이미 절대 경로면 그냥 통과
            if (Path.IsPathRooted(relativeImagePath))
            {
                if (File.Exists(relativeImagePath)) return relativeImagePath;
            }

            // 상대 경로인 경우 프로젝트 현재 위치에서 결합
            string projectDir = ProjectManager.Instance.ActiveFilePath;
            if (string.IsNullOrEmpty(projectDir))
            {
                projectDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                projectDir = Path.GetDirectoryName(projectDir) ?? AppDomain.CurrentDomain.BaseDirectory;
            }

            string combined = Path.Combine(projectDir, relativeImagePath);
            return combined;
        }
    }
}
