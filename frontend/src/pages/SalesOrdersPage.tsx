import { useMemo, useState } from 'react';
import { DatePicker, Descriptions, Select, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { extractErrorMessage } from '@/api/client';
import { useActiveCustomers } from '@/api/customers';
import { useSalesOrders, useVoidSalesOrder } from '@/api/salesOrders';
import type { SalesOrderDto } from '@/api/types';
import { AddButton, VoidButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { confirmVoid, notifyError, notifySuccess } from '@/utils/alerts';
import { formatCurrency, formatDateTime } from '@/utils/format';

const { RangePicker } = DatePicker;

export function SalesOrdersPage() {
  const navigate = useNavigate();
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [customerId, setCustomerId] = useState<number | undefined>();

  const customersQuery = useActiveCustomers();
  const { data, isLoading } = useSalesOrders({
    dateFrom: dateRange?.[0]?.startOf('day').toISOString(),
    dateTo: dateRange?.[1]?.endOf('day').toISOString(),
    customerId,
  });
  const voidMutation = useVoidSalesOrder();

  const customerOptions = useMemo(
    () => (customersQuery.data ?? []).map((c) => ({ label: c.name, value: c.id })),
    [customersQuery.data],
  );

  const handleVoid = async (record: SalesOrderDto) => {
    const confirmed = await confirmVoid('出貨單', record.orderNo);
    if (!confirmed) return;

    try {
      await voidMutation.mutateAsync(record.id);
      notifySuccess('出貨單已作廢，庫存已加回');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<SalesOrderDto> = [
    { title: '單號', dataIndex: 'orderNo', width: 180 },
    { title: '客戶', dataIndex: 'customerName', render: (v?: string | null) => v ?? '一般散客' },
    { title: '日期', dataIndex: 'orderDate', render: (v: string) => formatDateTime(v), width: 160 },
    {
      title: '狀態',
      dataIndex: 'status',
      width: 90,
      render: (status: string) => (status === 'Voided' ? <Tag color="red">已作廢</Tag> : <Tag color="green">正常</Tag>),
    },
    { title: '總金額', dataIndex: 'totalAmount', render: (v: number) => formatCurrency(v), width: 120 },
    { title: '備註', dataIndex: 'note', render: (v?: string | null) => v ?? '-', ellipsis: true },
    { title: '建立者', dataIndex: 'createdBy', width: 100 },
    {
      title: '操作',
      key: 'actions',
      width: 110,
      render: (_, record) =>
        record.status === 'Normal' ? (
          <VoidButton text onClick={() => handleVoid(record)}>
            作廢
          </VoidButton>
        ) : (
          <span style={{ color: 'rgba(0,0,0,0.25)' }}>已作廢</span>
        ),
    },
  ];

  return (
    <div className="page-container">
      <PageToolbar
        title="出貨單"
        filters={
          <Space wrap>
            <RangePicker value={dateRange} onChange={(value) => setDateRange(value as [dayjs.Dayjs, dayjs.Dayjs] | null)} />
            <Select
              placeholder="全部客戶"
              allowClear
              style={{ width: 180 }}
              options={customerOptions}
              value={customerId}
              onChange={setCustomerId}
            />
          </Space>
        }
        actions={<AddButton onClick={() => navigate('/sales-orders/new')}>新增出貨單</AddButton>}
      />

      <Table
        rowKey="id"
        loading={isLoading}
        dataSource={data}
        columns={columns}
        expandable={{
          expandedRowRender: (record) => (
            <Descriptions size="small" column={1} title="明細">
              {record.items.map((item, index) => (
                <Descriptions.Item key={index} label={item.productName ?? `商品 #${item.productId}`}>
                  數量 {item.quantity} × 單價 {formatCurrency(item.unitPrice)} = {formatCurrency(item.subtotal)}
                </Descriptions.Item>
              ))}
            </Descriptions>
          ),
        }}
      />
    </div>
  );
}
