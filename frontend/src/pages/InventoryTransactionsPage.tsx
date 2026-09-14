import { ArrowLeftOutlined } from '@ant-design/icons';
import { Button, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate, useParams } from 'react-router-dom';
import { useInventoryTransactions } from '@/api/inventory';
import { useProduct } from '@/api/products';
import type { InventoryChangeType, InventoryTransactionDto } from '@/api/types';
import { formatDateTime } from '@/utils/format';

const CHANGE_TYPE_LABEL: Record<InventoryChangeType, { label: string; color: string }> = {
  Purchase: { label: '進貨', color: 'green' },
  Sale: { label: '出貨', color: 'blue' },
  ManualAdjustment: { label: '手動調整', color: 'orange' },
  PurchaseVoid: { label: '進貨作廢', color: 'red' },
  SaleVoid: { label: '出貨作廢', color: 'red' },
};

export function InventoryTransactionsPage() {
  const { productId } = useParams<{ productId: string }>();
  const navigate = useNavigate();
  const numericProductId = productId ? Number(productId) : undefined;

  const productQuery = useProduct(numericProductId);
  const { data, isLoading } = useInventoryTransactions(numericProductId);

  const columns: ColumnsType<InventoryTransactionDto> = [
    { title: '時間', dataIndex: 'createdAt', render: (v: string) => formatDateTime(v), width: 170 },
    {
      title: '類型',
      dataIndex: 'changeType',
      width: 110,
      render: (type: InventoryChangeType) => {
        const meta = CHANGE_TYPE_LABEL[type];
        return <Tag color={meta?.color}>{meta?.label ?? type}</Tag>;
      },
    },
    {
      title: '異動量',
      dataIndex: 'quantityChange',
      width: 100,
      render: (value: number) => (
        <span style={{ color: value >= 0 ? '#16a34a' : '#dc2626' }}>
          {value >= 0 ? `+${value}` : value}
        </span>
      ),
    },
    { title: '異動後庫存', dataIndex: 'stockAfter', width: 110 },
    { title: '來源單據', dataIndex: 'refTable', width: 130 },
    { title: '來源單號 Id', dataIndex: 'refId', render: (v?: number | null) => v ?? '-', width: 110 },
    { title: '原因/備註', dataIndex: 'reason', render: (v?: string | null) => v ?? '-' },
    { title: '操作人', dataIndex: 'createdByUsername', render: (v?: string | null) => v ?? '-', width: 100 },
  ];

  return (
    <div className="page-container">
      <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/inventory')} style={{ marginBottom: 12 }}>
        返回庫存總覽
      </Button>

      <Typography.Title level={4}>
        庫存異動明細
        {productQuery.data && (
          <Typography.Text type="secondary" style={{ marginLeft: 12, fontSize: 16 }}>
            {productQuery.data.name}（{productQuery.data.sku}）
          </Typography.Text>
        )}
      </Typography.Title>

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />
    </div>
  );
}
