import { useQuery } from '@tanstack/react-query';
import { apiClient } from './client';
import type { ActivityLogDto } from './types';

export interface ActivityLogSearchParams {
  username?: string;
  dateFrom?: string;
  dateTo?: string;
}

async function searchActivityLogs(params: ActivityLogSearchParams): Promise<ActivityLogDto[]> {
  const { data } = await apiClient.get<ActivityLogDto[]>('/activity-logs', { params });
  return data;
}

export function useActivityLogs(params: ActivityLogSearchParams, enabled = true) {
  return useQuery({
    queryKey: ['activity-logs', params],
    queryFn: () => searchActivityLogs(params),
    enabled,
  });
}
