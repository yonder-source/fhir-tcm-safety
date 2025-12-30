# SMART on FHIR MAUI Blazor Hybrid App - Copilot 開發指南

**⚠️ 請用繁體中文回應所有提問和建議。**

## 專案架構

這是一個 **Mono Repo** 架構的 SMART on FHIR 醫療應用程式，使用 .NET MAUI Blazor Hybrid 技術，可同時產生手機 App 和 Web App。

```
src/
├── SmartFhirApp.Core/      # 核心服務層 (FHIR 服務、認證、配置)
├── SmartFhirApp.Shared/    # 共用 Razor 組件 (UI、頁面、佈局)
├── SmartFhirApp.Maui/      # MAUI Blazor Hybrid (Android/iOS/Windows/Mac)
└── SmartFhirApp.Web/       # Blazor WebAssembly (瀏覽器)
tests/
└── SmartFhirApp.Tests/     # 單元測試 (xUnit + Moq)
```

## 關鍵設計決策

### 程式碼共用策略
- **UI 組件**: 放在 `SmartFhirApp.Shared/Components/`
- **頁面**: 放在 `SmartFhirApp.Shared/Pages/`
- **業務邏輯**: 放在 `SmartFhirApp.Core/Services/`
- **MAUI/Web 專案**: 僅包含平台特定程式碼和啟動配置

### FHIR 相關
- 使用 `Hl7.Fhir.R4` NuGet 套件 (Firely SDK) v5.11.2
- FHIR 服務封裝在 `IFhirClientService` 介面
- 支援 SMART on FHIR OAuth2 + PKCE 認證流程
- 預設連接 SMART Health IT Sandbox 測試伺服器
- 內建重試機制 (3次嘗試，指數退避)

### 認證流程
- `SmartAuthService` 處理 OAuth2 授權流程
- PKCE (Proof Key for Code Exchange) 增強安全性
- Token 安全儲存透過 `ISecureStorageService` 抽象
  - MAUI: 使用原生 SecureStorage API
  - Web: 使用 sessionStorage (透過 JSInterop)
- 自動 Token 刷新和過期檢測

### 配置管理
- 使用 Options pattern 管理配置
- MAUI: `appsettings.json` 作為 EmbeddedResource
- Web: `wwwroot/appsettings.json` 和 `appsettings.Production.json`

## 開發指令

```powershell
# 還原套件
dotnet restore

# 建置全部
dotnet build

# 執行測試
dotnet test

# 執行 Web 版本
dotnet run --project src/SmartFhirApp.Web

# 執行 MAUI 版本 (Windows)
dotnet run --project src/SmartFhirApp.Maui -f net9.0-windows10.0.19041.0

# 執行 MAUI 版本 (Android)
dotnet run --project src/SmartFhirApp.Maui -f net9.0-android
```

## 慣例與規範

### Blazor 組件
- 組件檔名使用 PascalCase: `PatientCard.razor`
- 組件 CSS 使用 isolated CSS: `PatientCard.razor.css`
- 使用 `@inject` 注入服務，不使用建構函式注入

### FHIR 資源操作
```csharp
// 使用 IFhirClientService 操作 FHIR 資源
@inject IFhirClientService FhirClient

var patient = await FhirClient.GetCurrentPatientAsync();
var observations = await FhirClient.GetPatientObservationsAsync(patientId, "vital-signs");
```

### 服務註冊
```csharp
// 在 MauiProgram.cs 中
builder.Services.AddSmartFhirServices();
builder.Services.AddSecureStorage<MauiSecureStorageService>();

// 在 Program.cs (Web) 中
builder.Services.AddSmartFhirServices();
builder.Services.AddSecureStorage<WebSecureStorageService>();
```

## 重要檔案

| 檔案 | 用途 |
|------|------|
| `Core/Auth/SmartAuthService.cs` | SMART on FHIR OAuth2 認證 (含 PKCE) |
| `Core/Services/FhirClientService.cs` | FHIR API 操作封裝 (含重試、MedicationDispense 擴展) |
| `Core/Services/ISecureStorageService.cs` | 安全儲存抽象介面 |
| `Core/Services/InteractionRuleEngine.cs` | 中西藥交互作用規則引擎 |
| `Core/Models/InteractionRule.cs` | 交互作用規則、風險評估數據模型 |
| `Core/Models/TcmMedicine.cs` | 中藥資料結構 |
| `Core/Data/InteractionRuleData.cs` | 30+ 交互作用規則靜態資料庫 |
| `Core/Data/TcmMedicineData.cs` | 28 項中藥材靜態資料庫 (分類、風險機制) |
| `Core/Configuration/SmartConfiguration.cs` | 應用程式配置 |
| `Core/Extensions/ServiceCollectionExtensions.cs` | DI 擴展方法 |
| `Shared/Pages/TcmRiskAssessment.razor` | 中西藥風險評估主頁面 (3步驟流程) |
| `Shared/Components/TcmSelector.razor` | 中藥選擇器 (搜尋、分類、詳細表單) |
| `Shared/Components/RiskAssessmentDisplay.razor` | 風險評估結果顯示 |
| `Shared/Layout/MainLayout.razor` | 共用頁面佈局 |
| `Shared/Pages/CallbackPage.razor` | OAuth 回調處理 |
| `Shared/Components/PatientCard.razor` | 病患資訊卡片 |
| `Maui/Services/MauiSecureStorageService.cs` | MAUI 平台安全儲存 |
| `Web/Services/WebSecureStorageService.cs` | Web 平台安全儲存 |


## 中西藥交互作用評估 (TCM Risk Assessment)

### 架構概述
這是一個 **Rule Engine + Rule Data** 的分離設計：
- **InteractionRuleEngine**: 業務邏輯 - FHIR 資料提取、規則匹配、結果評估
- **InteractionRuleData**: 規則集合 - 30+ 靜態交互作用規則
- **TcmMedicineData**: 中藥資料庫 - 28 項中藥材，按類別組織

### 資料流程
1. **TcmRiskAssessment.razor** (頁面)
   - 3步驟 UI：驗證認證 → 獲取西藥 → 評估風險 → 顯示結果
   - 使用 `@inject IInteractionRuleEngine` 注入引擎

2. **InteractionRuleEngine.AssessRiskAsync()** (核心)
   - 呼叫 `FhirClient.GetPatientMedicationDispensesAsync()` 取得 MedicationDispense 資料
   - 解析西藥名稱，分類到 WesternMedicineRiskGroup (Anticoagulant、Antidiabetic 等 6 類)
   - 從 TcmSelector 獲取病患選定的中藥
   - 對每個 (中藥, 西藥分類) 配對執行 InteractionRuleData.FindApplicableRules()
   - 回傳 RiskAssessmentResult (整體風險等級 + 個別風險項目)

3. **TcmSelector.razor** (組件)
   - 搜尋、分類篩選 (TcmMedicineData 中 28 項)
   - 展開詳細表單：使用日期、使用頻率、來源
   - 儲存病患選擇到頁面狀態

4. **RiskAssessmentDisplay.razor** (結果展示)
   - 按風險等級著色 (High=紅、Medium=橙、Low=綠)
   - 顯示檢測到的風險項目 + 警告症狀
   - 西藥/中藥總結

### 添加新規則
修改 `Core/Data/InteractionRuleData.cs`：
```csharp
// 規則結構
new InteractionRule
{
    TcmCode = "石膏",                    // TcmMedicineData 中存在的代碼
    WesternGroup = WesternMedicineRiskGroup.Anticoagulant,
    Severity = RiskSeverity.High,
    Description = "可能增強抗凝血效果，提高出血風險",
    WarningSymptoms = new[] { "異常出血", "瘀青擴散" }
}
```

### 擴展西藥分類
當前為 MVP **關鍵字匹配** (in `InteractionRuleEngine.ClassifyWesternMedicine()`)。
計劃升級為 **ATC 藥理分類碼**，見 `docs/review-feedback.md` Phase 3。

## 測試

### 單元測試
```powershell
dotnet test tests/SmartFhirApp.Tests/
```

測試使用：
- **xUnit**: 測試框架
- **Moq**: Mock 框架
- **RichardSzalay.MockHttp**: HTTP Mock

### 整合測試
- 使用 SMART Health IT Launcher: https://launch.smarthealthit.org/
- 選擇 "Patient Standalone Launch" 進行測試
- 測試病患資料為合成資料，非真實資料

## 注意事項

1. **跨平台差異**: MAUI 使用 `smartfhirapp://callback` 作為重新導向 URI，Web 使用相對路徑 `/callback`
2. **HttpClient**: 使用 IHttpClientFactory 管理 HttpClient 生命週期
3. **狀態管理**: 認證狀態由 `SmartAuthService` 單例管理
4. **安全儲存**: Token 透過 ISecureStorageService 安全儲存，不要直接存到 localStorage
5. **重試機制**: FhirClientService 內建 3 次重試，指數退避策略
6. **平台配置**: 
   - Android: 需在 `AndroidManifest.xml` 設定 intent-filter
   - iOS: 需在 `Info.plist` 設定 URL Types

## 開發工作流程

### 多平台除錯 (VS Code)
使用 `.vscode/launch.json` 中的預設組態配合 `.vscode/tasks.json`：

| 平台 | 命令 | 環境 | 重導 URI |
|------|------|------|---------|
| **Web** | F5 → 🌐 Web (Blazor) | localhost:7001 (https) | `/callback` |
| **Android** | F5 → 🤖 Android Emulator | Pixel_7_API_34 | `smartfhirapp://callback` |
| **iOS** | F5 → 📱 iOS Simulator | macOS only | `smartfhirapp://callback` |
| **Windows** | F5 → 🪟 Windows Desktop | Native MAUI | `smartfhirapp://callback` |

預啟動任務自動執行對應 Target Framework 的 `dotnet build`。

### 常見開發任務

**擴展 FHIR 服務**
- 添加新方法到 `IFhirClientService` 介面
- 在 `FhirClientService.cs` 實作，使用 `FhirClient.SearchAsync()` 查詢
- 內建重試邏輯自動應用 (底層 HttpClient 配置)
- 範例: `GetPatientMedicationDispensesAsync()` 查詢 `MedicationDispense` 資源

**新增 Blazor 組件**
- 檔案: `Shared/Components/YourComponent.razor`
- 樣式: `Shared/Components/YourComponent.razor.css` (isolated)
- 注入服務: `@inject IServiceInterface Service`
- 回呼頁面參數: `[Parameter] public string Id { get; set; }`

**添加交互作用規則**
1. 確認中藥代碼存在於 `TcmMedicineData.All` (28 項)
2. 添加 `InteractionRule` 記錄到 `InteractionRuleData.Rules` 列表
3. 測試: `RiskAssessmentResult result = await engine.AssessRiskAsync(patientId, selectedTcms)`

**時間/日期處理 (FHIR)**
```csharp
// MedicationDispense Element 存儲字符串，需手動解析
if (DateTimeOffset.TryParse(dispense.WhenHandedOverElement.Value, out var handedOver))
{
    // handedOver 為 DateTimeOffset
}
```

### 快速測試流程
```powershell
# 1. 完整構建
dotnet build

# 2. 執行測試
dotnet test tests/SmartFhirApp.Tests/

# 3. 在 VS Code 啟動偵錯 (F5)
# 選擇目標平台並連接到 SMART Health IT Sandbox

# 4. 手動測試 TCM 風險評估
# - 登入 (點擊 OAuth 連結)
# - 導覽到 TcmRiskAssessment
# - 驗證從 MedicationDispense 解析西藥
# - 選擇中藥並檢視風險結果
```

## 常見模式

### 規則引擎模式 (Rule Engine + Data)
分離業務邏輯 (引擎) 和配置資料 (規則集)，便於擴展：
- `InteractionRuleEngine` 包含評估演算法
- `InteractionRuleData` 存儲靜態規則清單
- `TcmMedicineData` 存儲實體資料庫

添加新規則不需修改引擎邏輯，僅增加新 `InteractionRule` 物件。

### FHIR 資源回退策略
檢索藥物資訊時使用回退鏈 (優先順序)：
1. `MedicationDispense` (分派記錄)
2. `MedicationRequest` (處方)
3. `MedicationStatement` (病人報告用藥)

見 `InteractionRuleEngine.GetWesternMedicationsAsync()` 實作。

### 狀態管理 (Blazor 頁面)
`TcmRiskAssessment.razor` 使用區域狀態 (`@code { private ... }`) 管理：
- `selectedTcms`: 病人選擇的中藥清單
- `assessmentResult`: 風險評估結果
- `currentStep`: UI 步驟指示 (1-4)

無集中狀態容器，保持頁面自主性。

