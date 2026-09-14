import { useState } from 'react';
import { DatePicker, Input, Space, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useActivityLogs } from '@/api/activityLogs';
import type { ActivityLogDto } from '@/api/types';
import { PageToolbar } from '@/components/common/PageToolbar';
import { formatDateTime } from '@/utils/format';

const { RangePicker } = DatePicker;

const METHOD_COLOR: Record<string, string> = {
  POST: 'green',
  PUT: 'orange',
  PATCH: 'orange',
  DELETE: 'red',
};

function renderApi(api: string) {
  const [method, ...rest] = api.split(' ');
  const path = rest.join(' ');
  const color = METHOD_COLOR[method] ?? 'default';
  return (
    <Space>
      <Tag color={color}>{method}</Tag>
      <Typography.Text code>{path}</Typography.Text>
    </Space>
  );
}

export function ActivityLogsPage() {
  const [username, setUsername] = useState<string>();
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);

  const { data, isLoading } = useActivityLogs({
    username: username || undefined,
    dateFrom: dateRange?.[0]?.startOf('day').toISOString(),
    dateTo: dateRange?.[1]?.endOf('day').toISOString(),
  });

  const columns: ColumnsType<ActivityLogDto> = [
    { title: '時間', dataIndex: 'createdAt', render: (v: string) => formatDateTime(v), width: 180 },
    { title: '操作人', dataIndex: 'createdBy', width: 140 },
    { title: 'API', dataIndex: 'api', render: (v: string) => renderApi(v) },
  ];

  return (
    <div className="page-container">
      <PageToolbar
        title="操作紀錄"
        filters={
          <Space wrap>
            <Input.Search placeholder="依使用者名稱搜尋" allowClear style={{ width: 200 }} onSearch={setUsername} />
            <RangePicker value={dateRange} onChange={(value) => setDateRange(value as [dayjs.Dayjs, dayjs.Dayjs] | null)} />
          </Space>
        }
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />
    </div>
  );
}
