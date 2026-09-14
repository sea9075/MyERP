import type { ThemeConfig } from 'antd';

/**
 * 全站 antd 主題設定。品牌主色維持 antd 預設藍（用在導覽列選中狀態、連結、一般 primary 按鈕，
 * 例如「登入」「送出」「搜尋」這類跟新增/修改/刪除無關的操作）。
 *
 * 新增／修改／刪除三色是專案硬性規定的按鈕規則，因為 antd 沒有對應的第三種語意色 token，
 * 所以不是在這裡用 ConfigProvider 設定，而是用 src/styles/global.css 的 .btn-add/.btn-edit/.btn-delete
 * 類別直接覆蓋，讓「同一個 primary 按鈕」依用途呈現三種不同顏色。
 */
export const themeConfig: ThemeConfig = {
  token: {
    colorPrimary: '#1677ff',
    borderRadius: 6,
    fontFamily:
      "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'PingFang TC', 'Microsoft JhengHei', Roboto, Helvetica, Arial, sans-serif",
  },
  components: {
    Layout: {
      siderBg: '#001529',
      headerBg: '#ffffff',
    },
  },
};
