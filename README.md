# SMART on FHIR 多平台 Mono Repo

一個基於 .NET MAUI Blazor Hybrid + Blazor WebAssembly 的 SMART on FHIR 醫療應用程式雛形，支援 Web、Android、iOS。

## 功能特色

- SMART on FHIR 認證（Web：Blazor WASM OIDC / MAUI：Duende OidcClient）
- 多平台：Web、Android、iOS
- FHIR R4 連線
- Blazor WebAssembly + MAUI Blazor Hybrid

## 專案結構

```
SmartFhirApp.sln
├── src/
│   ├── SmartFhirApp.Core/      # SMART on FHIR 核心服務（Discovery + PKCE + Token）
│   ├── SmartFhirApp.Shared/    # 共用 Blazor 組件
│   ├── SmartFhirApp.Maui/      # MAUI App（Android/iOS）
│   └── SmartFhirApp.Web/       # Blazor WASM（Web）
```

## 快速開始

### 先決條件

- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- (選用) Android SDK / Xcode

### 建置與執行

```powershell
# 還原套件
dotnet restore

# 執行 Web 版本
dotnet run --project src\SmartFhirApp.Web

# 建置 MAUI Android 版本（需 Android SDK）
dotnet build src\SmartFhirApp.Maui -f net8.0-android
```

## 配置

在以下設定檔更新 SMART on FHIR 參數（尤其 ClientId 與 RedirectUri）：

- Web：`src/SmartFhirApp.Web/wwwroot/appsettings.json`
- MAUI：`src/SmartFhirApp.Maui/Resources/Raw/appsettings.json`

目前預設：

- Issuer（SMART sandbox）：
  `https://thas.mohw.gov.tw/v/r4/sim/WzIsIiIsIiIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMSwiIl0/fhir`
- FHIR Server（資料查詢）：
  `https://thas.mohw.gov.tw/v/r4/fhir`

## Web OAuth 回呼

- Web（Blazor WASM OIDC）：`http://localhost:5155/authentication/login-callback`

## 行動裝置 OAuth 回呼

- Android：`smartfhirapp://callback` 已在 `Platforms/Android/WebAuthenticationCallbackActivity.cs` 註冊
- iOS：`Info.plist` 已加入 `smartfhirapp` URL Scheme

## 測試

可用 SMART sandbox 或實際院方環境進行測試。

## 授權

MIT License
