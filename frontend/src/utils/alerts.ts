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

function escapeHtml(value: string): string {
  const div = document.createElement('div');
  div.textContent = value;
  return div.innerHTML;
}
