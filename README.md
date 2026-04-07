# Integrated Control System
통합제어프로그램 개발(C#)

고신뢰도 하드웨어 제어와 동적 UI 렌더링 엔진을 결합한 통합 제어 솔루션입니다. 
산업용 환경에서의 무중단 운영을 목표로 하며, 저사양 PC에서도 안정적인 퍼포먼스를 보장합니다.

**dotnet restore / dotnet run**

## 🛠 Tech Stack
- **Language:** C# (.NET 8.0)
- **Framework:** Windows Forms (WinForms)
- **Architecture:** 3+1 Slim Layer (UI, Data, Hardware + Harness)
- **Tooling:** Git Extensions, Visual Studio 2022, Antigravity AI Harness

---

## 📊 Layer-specific Progress (레이어별 진행률)

| 레이어 | 상태 | 진행률 | 완료된 핵심 과업 | 향후 과업 (To-Do) |
| :--- | :---: | :---: | :--- | :--- |
| **1. UI & Rendering** | 🔄 진행 | 85% | GDI+ 핸들 최적화, 메모리 스트림 로딩, 동적 버튼 생성 | 애니메이션, 고해상도 자산 최적화 |
| **2. Data Model** | ✅ 완료 | 100% | JSON 기반 ProjectConfig 설계, .gmn 포맷 정의 | 확장 속성(Extra Props) 정의 |
| **3. Hardware (HAL)** | 🔄 진행 | 40% | IDevice 인터페이스, TcpDevice 비동기 골격 구축 | AVCIT 프로토콜 연동, UDP 확장 |
| **4. Infra & Harness** | 🔄 진행 | 70% | AI 워크플로우(.agent) 구축, 빌드 환경 최적화 | GitHub CI/CD, 환경 동기화 완료 |

---

## ✅ 핵심 기술 구현 사항 (Technical Achievement)

### 🔹 UI & Rendering Core
- **GDI+ Handle Optimization:** 모든 요소를 컨트롤로 만들지 않고 정적 요소는 `OnPaint`에서 직접 렌더링하여 `Win32Exception` 방지.
- **Non-Blocking File I/O:** `Image.FromFile` 대신 `MemoryStream` 로딩 방식을 채택하여 이미지 파일 잠금(File Lock) 현상 원천 차단.
- **Deterministic Layout:** JSON 스키마에 정의된 좌표와 크기에 따라 런타임에 UI가 실시간으로 구성되는 엔진 구현.

### 🔹 Hardware Abstraction Layer (HAL)
- **Interface-Driven Design:** `IDevice` 규격을 통해 TCP/UDP/Serial 등 물리적 통신 수단에 관계없이 동일한 제어 로직 유지.
- **Async/Await Pattern:** 장비 응답 대기 중 UI가 멈추지 않도록 모든 통신 과정을 비동기로 처리.

---

## 🚀 Getting Started (개발 환경 동기화)

집이나 다른 환경에서 프로젝트를 이어서 진행하려면 다음 과정을 따르세요.

1. **사전 준비 (Prerequisites):**
   - [.NET 8 SDK](https://dotnet.microsoft.com/download) 설치
   - [Git for Windows](https://git-scm.com/) 엔진 설치
2. **저장소 복제 (Clone):**
   ```bash
   git clone [https://github.com/kimdaeone/Integrated-control-system.git](https://github.com/kimdaeone/Integrated-control-system.git)
   cd Integrated-control-system/total_contorl


  
