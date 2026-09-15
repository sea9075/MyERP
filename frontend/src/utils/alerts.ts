import Swal from 'sweetalert2';

/**
 * 全站共用的 SweetAlert2 封裝。刪除／作廢這類「不可逆、會影響資料或庫存」的操作一律要先跳出
 * 二次確認，確認按鈕統一用跟按鈕規則一致的紅色（跟畫面上的刪除/作廢按鈕顏色一致，使用者比較
 * 不會搞混「這個確認框到底是要做什麼危險的事」）。
 */

const DANGER_COLOR = '#dc2626';

export async function confirmDelete(itemLabel: string, itemName: string): Promise<boolean> {
  const result = await Swal.fire({
    icon: 'warning',
    title: `確定要刪除這筆${itemLabel}嗎？`,
    html: `<b>${escapeHtml(itemName)}</b><br/>刪除後可以在「顯示已刪除」篩選裡看到，但不會出現在一般列表中。`,
    showCancelButton: true,
    confirmButtonText: '確定刪除',
    cancelButtonText: '取消',
    confirmButtonColor: DANGER_COLOR,
    reverseButtons: true,
  });
  return result.isConfirmed;
}

export async function confirmVoid(orderLabel: string, orderNo: string, extraWarning?: string): Promise<boolean> {
  const result = await Swal.fire({
    icon: 'warning',
    title: `確定要作廢這張${orderLabel}嗎？`,
    html: `<b>${escapeHtml(orderNo)}</b><br/>作廢後庫存會自動沖銷，且無法復原。${extraWarning ? `<br/><span style="color:${DANGER_COLOR}">${escapeHtml(extraWarning)}</span>` : ''}`,
    showCancelButton: true,
    confirmButtonText: '確定作廢',
    cancelButtonText: '取消',
    confirmButtonColor: DANGER_COLOR,
    reverseButtons: true,
  });
  return result.isConfirmed;
}

export function notifySuccess(message: string): void {
  void Swal.fire({
    icon: 'success',
    title: message,
    toast: true,
    position: 'top-end',
    timer: 2200,
    showConfirmButton: false,
  });
}

export function notifyError(message: string): void {
  void Swal.fire({
    icon: 'error',
    title: '操作失敗',
    text: message,
    confirmButtonColor: DANGER_COLOR,
  });
}

/**
 * 全站表單驗證錯誤的統一顯示方式（使用者需求）：不在欄位下方顯示文字說明（該部分由
 * global.css 隱藏 antd 的 .ant-form-item-explain-error），改用 SweetAlert2 的 toast 顯示在
 * 畫面右上角；欄位本身還是會照 antd 預設行為出現紅框。如果同時有多個欄位出錯，也合併成
 *「一則」訊息顯示，不會跳出好幾個 toast。
 */
export function notifyValidationErrors(messages: string[]): void {
  const unique = Array.from(new Set(messages.filter((m) => !!m)));
  if (unique.length === 0) return;

  const text = unique.length === 1 ? unique[0] : unique.map((m) => `．${m}`).join('\n');
  void Swal.fire({
    icon: 'error',
    title: '請確認輸入內容',
    text,
    toast: true,
    position: 'top-end',
    timer: 5000,
    showConfirmButton: false,
  });
}

/**
 * 把 antd Form.validateFields() 失敗時丟出的 rejection（{ errorFields: { errors: string[] }[] }），
 * 或是 <Form onFinishFailed> 收到的 { errorFields } 物件，統一整理成一份不重複的錯誤訊息陣列，
 * 餵給 notifyValidationErrors 顯示。
 */
export function extractFormErrorMessages(err: unknown): string[] {
  const errorFields = (err as { errorFields?: { errors: string[] }[] } | undefined)?.errorFields;
  if (!errorFields) return [];
  return errorFields.flatMap((field) => field.errors);
}

function escapeHtml(value: string): string {
  const div = document.createElement('div');
  div.textContent = value;
  return div.innerHTML;
}
