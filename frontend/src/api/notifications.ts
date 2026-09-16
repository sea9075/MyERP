import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './client';
import type { NotificationDto } from './types';

const KEY = 'notifications';

async function getNotifications(unreadOnly: boolean): Promise<NotificationDto[]> {
  const { data } = await apiClient.get<NotificationDto[]>('/notifications', { params: { unreadOnly } });
  return data;
}

async function getUnreadCount(): Promise<number> {
  const { data } = await apiClient.get<number>('/notifications/unread-count');
  return data;
}

export function useNotifications(unreadOnly: boolean) {
  return useQuery({
    queryKey: [KEY, unreadOnly],
    queryFn: () => getNotifications(unreadOnly),
  });
}

/**
 * 未讀通知數（AppLayout 選單「通知」項目旁邊的紅點用）。每 30 秒輪詢一次——這個系統目前
 * 沒有 WebSocket/SignalR 即時推播的基礎建設，用輪詢是最簡單、足夠應付 1~5 人使用情境的做法。
 * enabled 由呼叫端依角色控制（後端限定 Product/Manager/Admin，其他角色不該打這支 API）。
 */
export function useUnreadNotificationCount(enabled: boolean) {
  return useQuery({
    queryKey: [KEY, 'unread-count'],
    queryFn: getUnreadCount,
    refetchInterval: 30_000,
    enabled,
  });
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiClient.put(`/notifications/${id}/read`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [KEY] });
    },
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => apiClient.put('/notifications/read-all'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [KEY] });
    },
  });
}
