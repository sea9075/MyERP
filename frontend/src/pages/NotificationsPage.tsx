import { useState } from 'react';
import { Button, Space, Switch, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
} from '@/api/notifications';
import type { NotificationDto } from '@/api/types';
import { PageToolbar } from '@/components/common/PageToolbar';
import { extractErrorMessage } from '@/api/client';
import { notifyError, notifySuccess } from '@/utils/alerts';
import { formatDateTime } from '@/utils/format';

/**
 * 系統通知（2026-09-15 新增：worker 低庫存自動通知，見 Infra-Progress.md §31）。
 * 目前唯一的通知類型是低庫存，畫面刻意做得很單純：列表 + 標記已讀/全部已讀，
 * 沒有像操作紀錄那樣的日期/使用者篩選——通知本來就是「最近沒處理的事」，不需要查歷史。
 */
export function NotificationsPage() {
  const [unreadOnly, setUnreadOnly] = useState(true);
  const { data, isLoading } = useNotifications(unreadOnly);
  const markReadMutation = useMarkNotificationRead();
  const markAllReadMutation = useMarkAllNotificationsRead();

  const handleMarkRead = async (id: number) => {
    try {
      await markReadMutation.mutateAsync(id);
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await markAllReadMutation.mutateAsync();
      notifySuccess('已將所有通知標記為已讀');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<NotificationDto> = [
    {
      title: '狀態',
      dataIndex: 'isRead',
      width: 90,
      render: (isRead: boolean) => (isRead ? <Tag>已讀</Tag> : <Tag color="red">未讀</Tag>),
    },
    { title: '時間', dataIndex: 'createdAt', render: (v: string) => formatDateTime(v), width: 180 },
    { title: '內容', dataIndex: 'message' },
    {
      title: '操作',
      key: 'actions',
      width: 120,
      render: (_, record) =>
        record.isRead ? null : (
          <Button size="small" onClick={() => handleMarkRead(record.id)} loading={markReadMutation.isPending}>
            標記已讀
          </Button>
        ),
    },
  ];

  return (
    <div className="page-container">
      <PageToolbar
        title="通知"
        filters={
          <Space>
            <Typography.Text>只看未讀</Typography.Text>
            <Switch checked={unreadOnly} onChange={setUnreadOnly} />
          </Space>
        }
        actions={
          <Button onClick={handleMarkAllRead} loading={markAllReadMutation.isPending}>
            全部標記已讀
          </Button>
        }
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />
    </div>
  );
}
