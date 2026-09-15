import { useMemo, useState } from 'react';
import { CalculatorOutlined, DollarOutlined } from '@ant-design/icons';
import { Alert, Card, DatePicker, Form, Input, InputNumber, Modal, Select, Space, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { extractErrorMessage } from '@/api/client';
import { useActiveEmployees } from '@/api/employees';
import { useCalculatePayroll, useDeletePayrollRecord, usePayrollRecords, useUpdatePayrollBonus } from '@/api/payroll';
import type { Department, PayrollRecordDto } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, extractFormErrorMessages, notifyError, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import { formatCurrency, formatDateTime } from '@/utils/format';

const DEPARTMENT_OPTIONS: { label: string; value: Department }[] = [
  { label: '商品部（Product）', value: 'Product' },
  { label: '人資部（HR）', value: 'HR' },
  { label: '主管（Manager）', value: 'Manager' },
  { label: '管理員（Admin）', value: 'Admin' },
  { label: '客服部（Support）', value: 'Support' },
];

interface CalculateFormValues {
  employeeId: number;
  periodMonth: dayjs.Dayjs;
}

interface BonusFormValues {
  bonusAmount: number;
  note?: string;
}

interface BatchBonusFormValues {
  bonusAmount: number;
  note?: string;
}

/** 部門批次作業的合併列：把「該部門在職員工」跟「該月份既有薪資紀錄」對應起來，還沒算過的也要能勾選去計算。 */
interface BatchRow {
  employeeId: number;
  employeeName: string;
  department: Department;
  monthlySalary: number;
  calculated: boolean;
  recordId?: number;
  baseSalary?: number;
  bonusAmount?: number;
  totalPay?: number;
}

export function PayrollPage() {
  // --- 部門批次作業（新增：勾選同一部門的多位員工，一次計算薪資／輸入獎金） ---
  const [batchDepartment, setBatchDepartment] = useState<Department | undefined>();
  const [batchPeriodMonth, setBatchPeriodMonth] = useState<dayjs.Dayjs | null>(null);
  const [selectedEmployeeIds, setSelectedEmployeeIds] = useState<number[]>([]);
  const [batchBonusModalOpen, setBatchBonusModalOpen] = useState(false);
  const [batchCalculating, setBatchCalculating] = useState(false);
  const [batchBonusSaving, setBatchBonusSaving] = useState(false);
  const [batchBonusForm] = Form.useForm<BatchBonusFormValues>();

  // --- 一般薪資查詢（既有功能：依員工/月份查詢＋單筆計算/編輯獎金/刪除） ---
  const [employeeId, setEmployeeId] = useState<number | undefined>();
  const [periodMonth, setPeriodMonth] = useState<dayjs.Dayjs | null>(null);
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [calculateModalOpen, setCalculateModalOpen] = useState(false);
  const [bonusEditing, setBonusEditing] = useState<PayrollRecordDto | null>(null);
  const [calculateForm] = Form.useForm<CalculateFormValues>();
  const [bonusForm] = Form.useForm<BonusFormValues>();

  const employeesQuery = useActiveEmployees();
  const calculateMutation = useCalculatePayroll();
  const updateBonusMutation = useUpdatePayrollBonus();
  const deleteMutation = useDeletePayrollRecord();

  const employeeOptions = useMemo(
    () => (employeesQuery.data ?? []).map((e) => ({ label: `${e.displayName}（${e.username}）`, value: e.id })),
    [employeesQuery.data],
  );

  // --- 部門批次作業：資料組合 ---
  const batchDepartmentEmployees = useMemo(
    () => (employeesQuery.data ?? []).filter((e) => e.department === batchDepartment),
    [employeesQuery.data, batchDepartment],
  );

  const batchPeriodMonthIso = batchPeriodMonth?.startOf('month').toISOString();

  const batchRecordsQuery = usePayrollRecords(
    { periodMonth: batchPeriodMonthIso, includeDeleted: false },
    !!batchPeriodMonth,
  );

  const batchRows: BatchRow[] = useMemo(() => {
    if (!batchDepartment || !batchPeriodMonth) return [];
    const recordByEmployeeId = new Map((batchRecordsQuery.data ?? []).map((r) => [r.employeeId, r]));
    return batchDepartmentEmployees.map((employee) => {
      const record = recordByEmployeeId.get(employee.id);
      return {
        employeeId: employee.id,
        employeeName: `${employee.displayName}（${employee.username}）`,
        department: employee.department,
        monthlySalary: employee.monthlySalary,
        calculated: !!record,
        recordId: record?.id,
        baseSalary: record?.baseSalary,
        bonusAmount: record?.bonusAmount,
        totalPay: record?.totalPay,
      };
    });
  }, [batchDepartment, batchPeriodMonth, batchDepartmentEmployees, batchRecordsQuery.data]);

  const selectedBatchRows = useMemo(
    () => batchRows.filter((row) => selectedEmployeeIds.includes(row.employeeId)),
    [batchRows, selectedEmployeeIds],
  );
  const selectedCalculatedRows = selectedBatchRows.filter((row) => row.calculated);

  const handleDepartmentChange = (value: Department | undefined) => {
    setBatchDepartment(value);
    setSelectedEmployeeIds([]);
  };

  const handleBatchMonthChange = (value: dayjs.Dayjs | null) => {
    setBatchPeriodMonth(value);
    setSelectedEmployeeIds([]);
  };

  /** 批次計算薪資：不管該員工這個月算過沒有，都重新呼叫一次計算 API（算過的會重算）。 */
  const handleBatchCalculate = async () => {
    if (!batchPeriodMonth || selectedEmployeeIds.length === 0) return;
    setBatchCalculating(true);
    try {
      const results = await Promise.allSettled(
        selectedEmployeeIds.map((id) =>
          calculateMutation.mutateAsync({ employeeId: id, periodMonth: batchPeriodMonth.startOf('month').toISOString() }),
        ),
      );
      const failed = results.filter((r) => r.status === 'rejected') as PromiseRejectedResult[];
      const successCount = results.length - failed.length;
      if (failed.length === 0) {
        notifySuccess(`已計算 ${successCount} 位員工的薪資`);
      } else {
        notifyError(
          `${successCount} 位計算成功，${failed.length} 位失敗：${extractErrorMessage(failed[0]?.reason)}`,
        );
      }
    } finally {
      setBatchCalculating(false);
    }
  };

  const openBatchBonusModal = () => {
    batchBonusForm.resetFields();
    setBatchBonusModalOpen(true);
  };

  /** 批次輸入獎金：統一輸入一個金額套用到所有勾選、且「已經計算過薪資」的員工；還沒算過的會跳過。 */
  const handleBatchBonus = async () => {
    let values: BatchBonusFormValues;
    try {
      values = await batchBonusForm.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }
    const targets = selectedCalculatedRows;
    if (targets.length === 0) return;

    setBatchBonusSaving(true);
    try {
      const results = await Promise.allSettled(
        targets.map((row) =>
          updateBonusMutation.mutateAsync({
            id: row.recordId as number,
            request: { bonusAmount: values.bonusAmount, note: values.note },
          }),
        ),
      );
      const failed = results.filter((r) => r.status === 'rejected') as PromiseRejectedResult[];
      const successCount = results.length - failed.length;
      const skipped = selectedBatchRows.length - targets.length;
      const skippedNote = skipped > 0 ? `，${skipped} 位尚未計算薪資已略過` : '';
      if (failed.length === 0) {
        notifySuccess(`已為 ${successCount} 位員工更新獎金${skippedNote}`);
        setBatchBonusModalOpen(false);
      } else {
        notifyError(
          `${successCount} 位更新成功，${failed.length} 位失敗：${extractErrorMessage(failed[0]?.reason)}${skippedNote}`,
        );
      }
    } finally {
      setBatchBonusSaving(false);
    }
  };

  const batchColumns: ColumnsType<BatchRow> = [
    { title: '員工', dataIndex: 'employeeName' },
    { title: '目前月薪', dataIndex: 'monthlySalary', render: (v: number) => formatCurrency(v), width: 110 },
    {
      title: '本月狀態',
      dataIndex: 'calculated',
      width: 100,
      render: (calculated: boolean) => (calculated ? <Tag color="green">已計算</Tag> : <Tag color="default">未計算</Tag>),
    },
    { title: '底薪', dataIndex: 'baseSalary', render: (v?: number) => (v !== undefined ? formatCurrency(v) : '-'), width: 110 },
    { title: '獎金', dataIndex: 'bonusAmount', render: (v?: number) => (v !== undefined ? formatCurrency(v) : '-'), width: 110 },
    { title: '應發總額', dataIndex: 'totalPay', render: (v?: number) => (v !== undefined ? formatCurrency(v) : '-'), width: 120 },
  ];

  // --- 一般薪資查詢 ---
  const { data, isLoading } = usePayrollRecords({
    employeeId,
    periodMonth: periodMonth?.startOf('month').toISOString(),
    includeDeleted,
  });

  const openCalculateModal = () => {
    calculateForm.resetFields();
    setCalculateModalOpen(true);
  };

  const handleCalculate = async () => {
    let values: CalculateFormValues;
    try {
      values = await calculateForm.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }

    try {
      await calculateMutation.mutateAsync({
        employeeId: values.employeeId,
        periodMonth: values.periodMonth.startOf('month').toISOString(),
      });
      notifySuccess('薪資已計算完成');
      setCalculateModalOpen(false);
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const openBonusModal = (record: PayrollRecordDto) => {
    setBonusEditing(record);
    bonusForm.setFieldsValue({ bonusAmount: record.bonusAmount, note: record.note ?? undefined });
  };

  const handleUpdateBonus = async () => {
    if (!bonusEditing) return;
    let values: BonusFormValues;
    try {
      values = await bonusForm.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }

    try {
      await updateBonusMutation.mutateAsync({
        id: bonusEditing.id,
        request: { bonusAmount: values.bonusAmount, note: values.note },
      });
      notifySuccess('獎金已更新');
      setBonusEditing(null);
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const handleDelete = async (record: PayrollRecordDto) => {
    const confirmed = await confirmDelete(
      '薪資紀錄',
      `${record.employeeName ?? ''}（${dayjs(record.periodMonth).format('YYYY-MM')}）`,
    );
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('薪資紀錄已刪除');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<PayrollRecordDto> = [
    { title: '員工', dataIndex: 'employeeName', render: (v?: string | null) => v ?? '-', width: 140 },
    { title: '薪資月份', dataIndex: 'periodMonth', render: (v: string) => dayjs(v).format('YYYY-MM'), width: 100 },
    { title: '底薪', dataIndex: 'baseSalary', render: (v: number) => formatCurrency(v), width: 110 },
    { title: '正常工時', dataIndex: 'regularHours', width: 100 },
    { title: '加班(1)', dataIndex: 'overtimeHoursTier1', width: 90 },
    { title: '加班(2)', dataIndex: 'overtimeHoursTier2', width: 90 },
    { title: '加班費', dataIndex: 'overtimePay', render: (v: number) => formatCurrency(v), width: 110 },
    { title: '獎金', dataIndex: 'bonusAmount', render: (v: number) => formatCurrency(v), width: 110 },
    {
      title: '應發總額',
      dataIndex: 'totalPay',
      render: (v: number) => <Typography.Text strong>{formatCurrency(v)}</Typography.Text>,
      width: 130,
    },
    {
      title: '狀態',
      dataIndex: 'isDeleted',
      width: 90,
      render: (isDeleted: boolean) => (isDeleted ? <Tag color="red">已刪除</Tag> : <Tag color="green">正常</Tag>),
    },
    { title: '最後更新', dataIndex: 'updatedAt', render: (v: string) => formatDateTime(v), width: 160 },
    {
      title: '操作',
      key: 'actions',
      width: 180,
      render: (_, record) =>
        record.isDeleted ? null : (
          <>
            <EditButton text onClick={() => openBonusModal(record)}>
              編輯獎金
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
      <Typography.Title level={4} style={{ marginTop: 0 }}>薪資管理</Typography.Title>

      {/* 部門批次作業 */}
      <Card
        title="部門批次作業"
        style={{ marginBottom: 24 }}
        extra={
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            篩選部門與月份後勾選員工，一次計算薪資或輸入獎金
          </Typography.Text>
        }
      >
        <Space wrap style={{ marginBottom: 16 }}>
          <Select
            placeholder="請選擇部門"
            options={DEPARTMENT_OPTIONS}
            style={{ width: 200 }}
            value={batchDepartment}
            onChange={handleDepartmentChange}
            allowClear
          />
          <DatePicker
            picker="month"
            placeholder="請選擇薪資月份"
            value={batchPeriodMonth}
            onChange={handleBatchMonthChange}
          />
          <AddButton
            icon={<CalculatorOutlined />}
            disabled={selectedEmployeeIds.length === 0}
            loading={batchCalculating}
            onClick={handleBatchCalculate}
          >
            批次計算薪資（{selectedEmployeeIds.length}）
          </AddButton>
          <EditButton
            icon={<DollarOutlined />}
            disabled={selectedCalculatedRows.length === 0}
            onClick={openBatchBonusModal}
          >
            批次輸入獎金（{selectedCalculatedRows.length}）
          </EditButton>
        </Space>

        {!batchDepartment || !batchPeriodMonth ? (
          <Alert type="info" showIcon message="請先選擇部門與薪資月份，才會列出可勾選的員工。" />
        ) : (
          <Table
            rowKey="employeeId"
            size="small"
            loading={batchRecordsQuery.isLoading}
            dataSource={batchRows}
            columns={batchColumns}
            pagination={false}
            rowSelection={{
              selectedRowKeys: selectedEmployeeIds,
              onChange: (keys) => setSelectedEmployeeIds(keys as number[]),
            }}
            locale={{ emptyText: '這個部門目前沒有在職員工' }}
          />
        )}
      </Card>

      {/* 一般薪資查詢／單筆操作 */}
      <PageToolbar
        title="薪資紀錄查詢"
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
            <DatePicker picker="month" placeholder="依月份篩選" value={periodMonth} onChange={setPeriodMonth} />
            <SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />
          </Space>
        }
        actions={
          <AddButton icon={<CalculatorOutlined />} onClick={openCalculateModal}>
            計算薪資
          </AddButton>
        }
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} scroll={{ x: 1200 }} />

      <Modal
        title="計算薪資"
        open={calculateModalOpen}
        onOk={handleCalculate}
        onCancel={() => setCalculateModalOpen(false)}
        confirmLoading={calculateMutation.isPending}
        okText="計算"
        cancelText="取消"
        okButtonProps={{ className: 'btn-add' }}
      >
        <Form form={calculateForm} layout="vertical">
          <Form.Item name="employeeId" label="員工" rules={[{ required: true, message: '請選擇員工' }]}>
            <Select placeholder="請選擇員工" showSearch optionFilterProp="label" options={employeeOptions} />
          </Form.Item>
          <Form.Item name="periodMonth" label="薪資月份" rules={[{ required: true, message: '請選擇月份' }]}>
            <DatePicker picker="month" style={{ width: '100%' }} />
          </Form.Item>
        </Form>
        <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>
          會依該員工當月的出勤紀錄自動加總工時、計算加班費；如果該員工這個月已經算過，會重新計算一次。
        </Typography.Paragraph>
      </Modal>

      <Modal
        title="編輯獎金"
        open={!!bonusEditing}
        onOk={handleUpdateBonus}
        onCancel={() => setBonusEditing(null)}
        confirmLoading={updateBonusMutation.isPending}
        okText="儲存"
        cancelText="取消"
        okButtonProps={{ className: 'btn-edit' }}
      >
        <Form form={bonusForm} layout="vertical">
          <Form.Item name="bonusAmount" label="獎金金額" rules={[{ required: true, message: '請輸入獎金金額' }]}>
            <InputNumber min={0} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label="備註" rules={[{ max: 200 }]}>
            <Input.TextArea rows={2} placeholder="選填" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`批次輸入獎金（${selectedCalculatedRows.length} 位員工）`}
        open={batchBonusModalOpen}
        onOk={handleBatchBonus}
        onCancel={() => setBatchBonusModalOpen(false)}
        confirmLoading={batchBonusSaving}
        okText="套用"
        cancelText="取消"
        okButtonProps={{ className: 'btn-edit' }}
      >
        <Form form={batchBonusForm} layout="vertical">
          <Form.Item
            name="bonusAmount"
            label="獎金金額（套用到所有勾選且已計算薪資的員工）"
            rules={[{ required: true, message: '請輸入獎金金額' }]}
          >
            <InputNumber min={0} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label="備註" rules={[{ max: 200 }]}>
            <Input.TextArea rows={2} placeholder="選填，會套用到所有勾選的員工" />
          </Form.Item>
        </Form>
        {selectedBatchRows.length > selectedCalculatedRows.length && (
          <Alert
            type="warning"
            showIcon
            message={`勾選的員工中有 ${selectedBatchRows.length - selectedCalculatedRows.length} 位這個月還沒計算薪資，會自動略過，請先幫他們計算薪資。`}
          />
        )}
      </Modal>
    </div>
  );
}
