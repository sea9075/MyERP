import { useMemo, useState } from 'react';
import { UndoOutlined } from '@ant-design/icons';
import { DatePicker, Form, Input, Modal, Select, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import {
  useAttendanceRecords,
  useCreateManualAttendance,
  useDeleteAttendance,
  useUpdateAttendance,
} from '@/api/attendance';
import { extractErrorMessage } from '@/api/client';
import { useActiveEmployees } from '@/api/employees';
import type { AttendanceRecordDto, AttendanceSource } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, extractFormErrorMessages, notifyError, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import { formatDate, formatDateTime } from '@/utils/format';

const { RangePicker } = DatePicker;

const SOURCE_LABEL: Record<AttendanceSource, { label: string; color: string }> = {
  SelfService: { label: '自助打卡', color: 'blue' },
  ManualEntry: { label: '人工補登', color: 'orange' },
};

interface AttendanceFormValues {
  employeeId: number;
  workDate: dayjs.Dayjs;
  clockInAt?: dayjs.Dayjs;
  clockOutAt?: dayjs.Dayjs;
  note?: string;
}

/** HR/Manager/Admin 專用：查全部員工出勤 + 手動建立/修改/刪除，對應「我的出勤」以外的管理功能。 */
export function AttendanceManagementPage() {
  const [employeeId, setEmployeeId] = useState<number | undefined>();
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AttendanceRecordDto | null>(null);
  const [form] = Form.useForm<AttendanceFormValues>();

  const employeesQuery = useActiveEmployees();
  const { data, isLoading } = useAttendanceRecords({
    employeeId,
    dateFrom: dateRange?.[0]?.startOf('day').toISOString(),
    dateTo: dateRange?.[1]?.endOf('day').toISOString(),
    includeDeleted,
  });
  const createMutation = useCreateManualAttendance();
  const updateMutation = useUpdateAttendance();
  const deleteMutation = useDeleteAttendance();

  const employeeOptions = useMemo(
    () => (employeesQuery.data ?? []).map((e) => ({ label: `${e.displayName}（${e.username}）`, value: e.id })),
    [employeesQuery.data],
  );

  const openCreateModal = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEditModal = (record: AttendanceRecordDto) => {
    setEditing(record);
    form.setFieldsValue({
      employeeId: record.employeeId,
      workDate: dayjs(record.workDate),
      clockInAt: record.clockInAt ? dayjs(record.clockInAt) : undefined,
      clockOutAt: record.clockOutAt ? dayjs(record.clockOutAt) : undefined,
      note: record.note ?? undefined,
    });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    let values: AttendanceFormValues;
    try {
      values = await form.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }

    try {
      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          request: {
            workDate: values.workDate.toISOString(),
            clockInAt: values.clockInAt?.toISOString(),
            clockOutAt: values.clockOutAt?.toISOString(),
            note: values.note,
            isDeleted: editing.isDeleted,
          },
        });
        notifySuccess('出勤紀錄已更新');
      } else {
        await createMutation.mutateAsync({
          employeeId: values.employeeId,
          workDate: values.workDate.toISOString(),
          clockInAt: values.clockInAt?.toISOString(),
          clockOutAt: values.clockOutAt?.toISOString(),
          note: values.note,
        });
        notifySuccess('出勤紀錄已補登');
      }
      setModalOpen(false);
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const handleDelete = async (record: AttendanceRecordDto) => {
    const confirmed = await confirmDelete('出勤紀錄', `${record.employeeName ?? ''}（${formatDate(record.workDate)}）`);
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('出勤紀錄已刪除');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleRestore = async (record: AttendanceRecordDto) => {
    try {
      await updateMutation.mutateAsync({
        id: record.id,
        request: {
          workDate: record.workDate,
          clockInAt: record.clockInAt,
          clockOutAt: record.clockOutAt,
          note: record.note,
          isDeleted: false,
        },
      });
      notifySuccess('出勤紀錄已恢復');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<AttendanceRecordDto> = [
    { title: '員工', dataIndex: 'employeeName', render: (v?: string | null) => v ?? '-', width: 140 },
    { title: '日期', dataIndex: 'workDate', render: (v: string) => formatDate(v), width: 110 },
    { title: '上班時間', dataIndex: 'clockInAt', render: (v?: string | null) => formatDateTime(v) },
    { title: '下班時間', dataIndex: 'clockOutAt', render: (v?: string | null) => formatDateTime(v) },
    {
      title: '工時',
      dataIndex: 'workedHours',
      width: 90,
      render: (v?: number | null) => (v !== undefined && v !== null ? `${v} 小時` : '-'),
    },
    {
      title: '來源',
      dataIndex: 'source',
      width: 100,
      render: (source: AttendanceSource) => <Tag color={SOURCE_LABEL[source]?.color}>{SOURCE_LABEL[source]?.label ?? source}</Tag>,
    },
    { title: '備註', dataIndex: 'note', render: (v?: string | null) => v ?? '-' },
    {
      title: '狀態',
      dataIndex: 'isDeleted',
      width: 90,
      render: (isDeleted: boolean) => (isDeleted ? <Tag color="red">已刪除</Tag> : <Tag color="green">正常</Tag>),
    },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_, record) =>
        record.isDeleted ? (
          <EditButton text icon={<UndoOutlined />} onClick={() => handleRestore(record)}>
            恢復
          </EditButton>
        ) : (
          <>
            <EditButton text onClick={() => openEditModal(record)}>
              編輯
            </EditButton>
            <DeleteButton text onClick={() => handleDelete(record)}>
              刪除
            </DeleteButton>
          </>
        ),
    },
  ];

  return (
    <div className="page-container">
      <PageToolbar
        title="出勤管理"
        filters={
          <Space wrap>
            <Select
              placeholder="依員工篩選"
              allowClear
              showSearch
              optionFilterProp="label"
              options={employeeOptions}
              style={{ width: 200 }}
              onChange={setEmployeeId}
            />
            <RangePicker value={dateRange} onChange={(value) => setDateRange(value as [dayjs.Dayjs, dayjs.Dayjs] | null)} />
            <SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />
          </Space>
        }
        actions={<AddButton onClick={openCreateModal}>手動補登</AddButton>}
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />

      <Modal
        title={editing ? '編輯出勤紀錄' : '手動補登出勤紀錄'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        okText={editing ? '儲存' : '新增'}
        cancelText="取消"
        okButtonProps={{ className: editing ? 'btn-edit' : 'btn-add' }}
      >
        <Form form={form} layout="vertical">
          {!editing && (
            <Form.Item name="employeeId" label="員工" rules={[{ required: true, message: '請選擇員工' }]}>
              <Select placeholder="請選擇員工" showSearch optionFilterProp="label" options={employeeOptions} />
            </Form.Item>
          )}

          <Form.Item name="workDate" label="出勤日期" rules={[{ required: true, message: '請選擇日期' }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="clockInAt" label="上班時間">
            <DatePicker showTime style={{ width: '100%' }} placeholder="選填" />
          </Form.Item>

          <Form.Item name="clockOutAt" label="下班時間">
            <DatePicker showTime style={{ width: '100%' }} placeholder="選填" />
          </Form.Item>

          <Form.Item name="note" label="備註" rules={[{ max: 200 }]}>
            <Input.TextArea rows={2} placeholder="選填" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
