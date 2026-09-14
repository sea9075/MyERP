import { useMemo, useState } from 'react';
import { DatePicker, Descriptions, Select, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { extractErrorMessage } from '@/api/client';
import { usePurchaseOrders, useVoidPurchaseOrder } from '@/api/purchaseOrders';
import { useActiveSuppliers } from '@/api/suppliers';
import type { PurchaseOrderDto } from '@/api/types';
import { AddButton, VoidButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { confirmVoid, notifyError, notifySuccess } from '@/utils/alerts';
import { formatCurrency, formatDateTime } from '@/utils/format';

const { RangePicker } = DatePicker;

export function PurchaseOrdersPage() {
  const navigate = useNavigate();
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs] | null>(null);
  const [supplierId, setSupplierId] = useState<number | undefined>();

  const suppliersQuery = useActiveSuppliers();
  const { data, isLoading } = usePurchaseOrders({
    dateFrom: dateRange?.[0]?.startOf('day').toISOString(),
    dateTo: dateRange?.[1]?.endOf('day').toISOString(),
    supplierId,
  });
  const voidMutation = useVoidPurchaseOrder();

  const supplierOptions = useMemo(
    () => (suppliersQuery.data ?? []).map((s) => ({ label: s.name, value: s.id })),
    [suppliersQuery.data],
  );

  const handleVoid = async (record: PurchaseOrderDto) => {
    const confirmed = await confirmVoid('進貨單', record.orderNo, '如果貨品已經被後續出貨單賣掉一部分，作廢可能會失敗。');
    if (!confirmed) return;

    try {
      await voidMutation.mutateAsync(record.id);
      notifySuccess('進貨單已作廢');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<PurchaseOrderDto> = [
    { title: '單號', dataIndex: 'orderNo', width: 180 },
    { title: '供應商', dataIndex: 'supplierName', render: (v?: string | null) => v ?? '-' },
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
        title="進貨單"
        filters={
          <Space wrap>
            <RangePicker value={dateRange} onChange={(value) => setDateRange(value as [dayjs.Dayjs, dayjs.Dayjs] | null)} />
            <Select
              placeholder="全部供應商"
              allowClear
              style={{ width: 180 }}
              options={supplierOptions}
              value={supplierId}
              onChange={setSupplierId}
            />
          </Space>
        }
        actions={<AddButton onClick={() => navigate('/purchase-orders/new')}>新增進貨單</AddButton>}
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
