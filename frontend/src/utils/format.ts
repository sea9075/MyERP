import dayjs from 'dayjs';

export function formatDateTime(value: string | undefined | null): string {
  if (!value) return '-';
  return dayjs(value).format('YYYY-MM-DD HH:mm');
}

export function formatDate(value: string | undefined | null): string {
  if (!value) return '-';
  return dayjs(value).format('YYYY-MM-DD');
}

export function formatCurrency(value: number | undefined | null): string {
  if (value === undefined || value === null) return '-';
  return `NT$ ${value.toLocaleString('zh-TW', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
}
