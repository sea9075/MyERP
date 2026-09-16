# MyERP

Self-hosted K8s + Azure hybrid-cloud ERP portfolio project.

一套從零開始設計的進銷存管理系統，涵蓋商品／庫存／進出貨／客戶供應商／人資薪資出勤／報表等完整業務模組，並以此為載體實作一套真實可運作的混合雲基礎設施：後端部署在自建的 3 節點 Kubernetes 家庭實驗室叢集，前端部署在 Azure Static Web Apps，透過 GitOps 自動化串接兩端。

**Live demo**: [myerp.kuei.dev](https://myerp.kuei.dev)（首頁右上角 info 圖示有測試帳號密碼，可直接登入體驗）

## 系統架構

```
瀏覽器
  │
  ├─► Azure Static Web Apps（前端 SPA，React + Vite）
  │
  └─► Cloudflare（TLS 終止）→ Cloudflare Tunnel → Cilium Gateway API
          │
          ▼
      Self-hosted K8s 叢集（kubeadm + Cilium）
          ├─ myerp-api（.NET 10 Web API，Clean Architecture）
          ├─ myerp-worker（訂閱 Service Bus，KEDA 依佇列長度自動擴縮 0→N）
          ├─ ArgoCD（GitOps，監看 MyErp-gitops repo 自動 sync）
          ├─ External Secrets Operator（同步 Azure Key Vault → K8s Secret）
          └─ kube-prometheus-stack（Prometheus + Grafana 監控）
                │
                ▼
      Azure（SQL Database／Service Bus／Key Vault／Container Registry）
```

後端與前端的原始碼都在這個 repo，K8s 用的 Helm chart 與 GitOps 設定則獨立放在 [`MyErp-gitops`](https://github.com/sea9075/MyErp-gitops)：push 這個 repo 的 `main` 後，GitHub Actions 會自動 build image、推送到 Azure Container Registry、更新 `MyErp-gitops` 的 image tag，ArgoCD 偵測到變更後自動同步部署到叢集，全程不需要手動操作 `kubectl apply`。

## 技術棧

**後端**：.NET 10、ASP.NET Core Web API、Entity Framework Core、Clean Architecture（Domain / Application / Infra / Api / Tests 五層專案）、JWT Bearer 驗證、Azure Service Bus（非同步事件發布）

**前端**：React 18、TypeScript、Vite、Ant Design、TanStack Query、Zustand、React Router

**基礎設施**：Kubernetes（kubeadm、Cilium CNI + Gateway API）、Docker、ArgoCD、Helm、KEDA（事件驅動自動擴縮）、External Secrets Operator、cert-manager、kube-prometheus-stack、Cloudflare Tunnel、GitHub Actions、Azure（SQL Database、Service Bus、Key Vault、Container Registry、Static Web Apps）

## 功能模組

- 商品／分類／供應商／客戶管理
- 進貨單、出貨單（含自動加減庫存、庫存不足檢查、作廢回沖）
- 庫存總覽與異動查詢、手動盤點調整
- 人資：員工、出勤、薪資管理（部門制權限模型）
- 報表：進貨統計、銷售統計、毛利報表、庫存總覽（可匯出 CSV）
- 低庫存自動通知（`MyErp.Worker` 訂閱 Service Bus 事件、非同步處理，避免拖慢主要交易流程）
- JWT 登入、自助改密碼、稽核欄位與操作紀錄（誰在什麼時候做了什麼）

## 這個專案解決過的真實工程問題

不只是把程式碼跑起來，過程中實際排查並修復了幾個上線後才會遇到的真實狀況：

- EF Core 預設的可重試錯誤清單沒有涵蓋 DNS 暫時性錯誤（SQL Error 11002），導致叢集節點資源緊繃時偶發連線失敗，補上 `errorNumbersToAdd` 後才真正被重試機制接住。
- 應用程式啟動時要先跑完 DB migration 才開始監聽連線，K8s 預設的 readiness/liveness 探針寬限期不夠長，導致還沒啟動完就被誤判不健康、反覆重啟——用 `startupProbe` 解決。
- Azure Key Vault 密碼更新後，`ExternalSecret` 的被動定時同步還沒反應過來就重啟 Pod，導致 Pod 讀到更新前的舊密碼登入失敗——找出時間序列問題後補上手動強制刷新的步驟。
- 3 台 4.5GB RAM 的 homelab 節點在多個平台元件（Cilium、ArgoCD、KEDA、kube-prometheus-stack 等）同時運作下發生記憶體耗盡，Linux OOM Killer 波及到 `sshd` 等系統服務。診斷出 `kube-prometheus-stack` 用預設值安裝、Prometheus/Grafana 未設定 `resources.limits` 是主因，補上明確的資源上限後解決。

## 本機開發

**後端**：
```bash
cd src/MyErp.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "Jwt:Key" "..."
dotnet run
```

**前端**：
```bash
cd frontend
npm install
npm run dev
```

## License

[MIT](./LICENSE)
