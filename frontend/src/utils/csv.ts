/**
 * 報表模組的 CSV 匯出（2026-09-15 新增，見 ERP.md §4.6、Infra-Progress.md §27）。
 * 設計決策：直接把「目前畫面上已經拿到的資料」在前端轉成 CSV 觸發瀏覽器下載，不走後端匯出端點——
 * 後端匯出端點需要另外處理「下載連結要怎麼帶 JWT」的問題（一般 <a href> 連結不會帶 Authorization
 * header），前端這樣做最簡單，資料來源也跟畫面上看到的完全一致。
 */

export interface CsvColumn<T> {
  header: string;
  accessor: (row: T) => string | number | null | undefined;
}

export function exportToCsv<T>(filename: string, columns: CsvColumn<T>[], rows: T[]): void {
  const escapeCell = (raw: string): string => {
    if (/["\n,]/.test(raw)) {
      return `"${raw.replace(/"/g, '""')}"`;
    }
    return raw;
  };

  const headerLine = columns.map((c) => escapeCell(c.header)).join(',');
  const bodyLines = rows.map((row) =>
    columns.map((c) => escapeCell(String(c.accessor(row) ?? ''))).join(','),
  );
  const csvContent = [headerLine, ...bodyLines].join('\r\n');

  // 開頭加 UTF-8 BOM，Excel 開啟時中文欄位才不會變亂碼。
  const blob = new Blob([`﻿${csvContent}`], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);

  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
