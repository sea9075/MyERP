import { useMemo } from 'react';
import { LoginOutlined, LogoutOutlined } from '@ant-design/icons';
import { Alert, Button, Card, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useClockIn, useClockOut, useMyAttendance } from '@/api/attendance';
import { extractErrorMessage } from '@/api/client';
import type { AttendanceRecordDto } from '@/api/types';
import { notifyError, notifySuccess } from '@/utils/alerts';
import { formatDate, formatDateTime } from '@/utils/format';

/**
 * 獨立的「我的出勤」頁面：不分部門，任何登入使用者都能用（後端 /api/attendance/clock-in、
 * clock-out、me 這 3 支 API 只要求登入，不限定部門）。
 *
 * 邊界情況：目前系統裡的 seed admin 帳號沒有連結 Employee 資料，打卡時後端會回
 * BusinessRuleException（「找不到你的員工資料，無法打卡，請聯絡人資建立員工資料」），
 * 這裡直接用既有的 notifyError(extractErrorMessage(error)) 顯示，不特別隱藏打卡按鈕。
 */
export function MyAttendancePage() {
  const monthStart = useMemo(() => dayjs().startOf('month').toISOString(), []);
  const todayEnd = useMemo(() => dayjs().endOf('day').toISOString(), []);

  const { data, isLoading } = useMyAttendance({ dateFrom: monthStart, dateTo: todayEnd });
  const clockInMutation = useClockIn();
  const clockOutMutation = useClockOut();

  const todayRecord = useMemo(
    () => (data ?? []).find((record) => dayjs(record.workDate).isSame(dayjs(), 'day')),
    [data],
  );

  const canClockIn = !todayRecord || !todayRecord.clockInAt;
  const canClockOut = !!todayRecord?.clockInAt && !todayRecord.clockOutAt;

  const handleClockIn = async () => {
    try {
      await clockInMutation.mutateAsync({});
      notifySuccess('上班打卡成功');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleClockOut = async () => {
    try {
      await clockOutMutation.mutateAsync({});
      notifySuccess('下班打卡成功');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<AttendanceRecordDto> = [
    { title: '日期', dataIndex: 'workDate', render: (v: string) => formatDate(v), width: 120 },
    { title: '上班時間', dataIndex: 'clockInAt', render: (v?: string | null) => formatDateTime(v) },
    { title: '下班時間', dataIndex: 'clockOutAt', render: (v?: string | null) => formatDateTime(v) },
    {
      title: '工時',
      dataIndex: 'workedHours',
      width: 100,
      render: (v?: number | null) => (v !== undefined && v !== null ? `${v} 小時` : '-'),
    },
    { title: '備註', dataIndex: 'note', render: (v?: string | null) => v ?? '-' },
  ];

  return (
    <div className="page-container">
      <Typography.Title level={4}>我的出勤</Typography.Title>

      <Card style={{ marginBottom: 16 }}>
        <Space size="middle">
          <Button
            type="primary"
            icon={<LoginOutlined />}
            disabled={!canClockIn}
            loading={clockInMutation.isPending}
            onClick={handleClockIn}
          >
            上班打卡
          </Button>
          <Button icon={<LogoutOutlined />} disabled={!canClockOut} loading={clockOutMutation.isPending} onClick={handleClockOut}>
            下班打卡
          </Button>
          {todayRecord?.clockInAt && (
            <Typography.Text type="secondary">
              今天已於 {formatDateTime(todayRecord.clockInAt)} 上班打卡
              {todayRecord.clockOutAt ? `，${formatDateTime(todayRecord.clockOutAt)} 下班打卡` : ''}
            </Typography.Text>
          )}
        </Space>
      </Card>

      {!isLoading && !todayRecord && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="今天還沒有出勤紀錄，請先上班打卡。"
        />
      )}

      <Table
        rowKey="id"
        loading={isLoading}
        dataSource={data}
        columns={columns}
        title={() => '本月出勤紀錄'}
      />
    </div>
  );
}
