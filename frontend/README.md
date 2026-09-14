# MyERP 前端（Vite + React + TypeScript + Ant Design）

對應 `ERP.md` §2 / §7 的前端規劃，串接 `MyErp.Api` 目前已完工的 API（商品、分類、供應商、客戶、進貨單、
出貨單、庫存、操作紀錄）。「報表」和「使用者管理」這兩個頁面因為後端還沒有對應的 Controller，這次
沒有做，之後後端補上再回來補前端。

## 重要：這裡只有原始碼，沒有安裝任何 npm 套件

專案裡**沒有** `node_modules`、也**沒有**跑過 `npm install`。這是刻意的：所有程式碼都已經寫好、
也都用到了 `package.json` 裡列出的套件，但套件本身完全沒有被安裝過。第一次使用前，你需要自己執行：

```bash
cd frontend
npm install
```

`package.json` 裡列出的完整套件清單（見下方「套件清單」章節）。

## 開發環境設定

0. **手動建立 `.env.development` 和 `.env.production` 這兩個檔案**：基於安全限制，工具沒辦法把
   `.env` 開頭的檔案直接寫進你的電腦，這兩個檔案改用聊天室的檔案卡片單獨傳給你了，
   直接把它們存到 `frontend/` 資料夾底下（跟 `package.json`同一層）就可以，內容不需要修改。

1. **確認後端網址**：預設會打 `https://localhost:7073/api`（對應 `MyErp.Api` 的 https launch
   profile）。如果你 `dotnet run` 起來的網址不同，改 `.env.development` 裡的 `VITE_API_BASE_URL`
   就好，不用改任何程式碼。

2. **信任後端的開發憑證**（只需要做一次，否則瀏覽器可能會擋掉 `https://localhost:7073` 的自我簽章
   憑證，造成打 API 全部失敗）：

   ```powershell
   dotnet dev-certs https --trust
   ```

3. **啟動後端**（另一個終端機視窗）：

   ```powershell
   cd "MyERP\src\MyErp.Api"
   dotnet run --launch-profile https
   ```

4. **安裝前端套件並啟動開發伺服器**：

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

   預設會在 `http://localhost:5173` 啟動（後端 `Program.cs` 的 CORS 設定已經開放這個 port，見
   `MyErp.Api/Program.cs` 的 `AddCors`）。

5. 用種子帳號登入：`admin` / `Admin@123456`（見 `SeedData.cs`）。

## 目錄結構

```
frontend/
├─ src/
│  ├─ api/            # axios client + 每個資源的 API 呼叫與 React Query hooks
│  ├─ components/
│  │  ├─ common/       # 三色按鈕（AddButton/EditButton/DeleteButton/VoidButton）、頁首、篩選開關
│  │  └─ layout/       # AppLayout：側邊選單 + Header
│  ├─ pages/           # 每個路由一個頁面元件
│  ├─ router/          # ProtectedRoute（未登入導回 /login）、AdminOnlyRoute
│  ├─ stores/          # authStore（zustand，含 localStorage 持久化）
│  ├─ styles/          # global.css（三色按鈕的實際色碼定義）
│  ├─ theme/           # antd ConfigProvider 主題設定
│  └─ utils/           # SweetAlert2 封裝、日期/金額格式化
├─ .env.development    # 本機開發用的後端網址
├─ .env.production     # 正式環境用的後端網址（myerp-api.kuei.dev，部署前請再次確認）
└─ package.json
```

## 按鈕顏色規則（專案規定，寫死在 `src/styles/global.css`）

- **新增／建立** → 綠色系（`.btn-add` / `.btn-add-text`，色碼 `#16a34a`）
- **修改／編輯** → 橘色系（`.btn-edit` / `.btn-edit-text`，色碼 `#f97316`）
- **刪除／作廢** → 紅色系（`.btn-delete` / `.btn-delete-text`，色碼 `#dc2626`）

進貨單/出貨單的「作廢」按鈕比照刪除，用紅色系（`VoidButton` 元件，內部用跟 `DeleteButton` 一樣的
class）。所有刪除/作廢操作都會先跳出 SweetAlert2 二次確認，確認鍵也是同一套紅色，避免使用者搞混。

商品、供應商支援「取消刪除」（因為後端 Update API 有開放 `isDeleted` 欄位可以改回 `false`），這個操作
本質上是「修改」，所以用橘色按鈕；分類、客戶的後端 API 沒有提供這個欄位，畫面上刪除後就無法復原
（如果之後需要，要先請後端加欄位）。

## 這次沒有做的部分（超出範圍）

- **報表**（`/reports`）、**使用者管理**（`/users`）：後端完全沒有對應的 Controller，做出來也串不到
  真實資料。
- 進貨單/出貨單目前用表格展開列顯示明細，沒有做獨立的 `/purchase-orders/:id` 詳情頁（ERP.md §7 原本
  規劃的頁面清單裡也沒有這個路徑）。
