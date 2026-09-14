import { useState } from 'react';
import { HistoryOutlined } from '@ant-design/icons';
import { Form, Input, InputNumber, Modal, Switch, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { Link } from 'react-router-dom';
import { useAdjustInventory, useInventory } from '@/api/inventory';
import { extractErrorMessage } from '@/api/client';
import type { InventoryItemDto } from '@/api/types';
import { EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { notifyError, notifySuccess } from '@/utils/alerts';

interface AdjustFormValues {
  adjustmentQuantity: number;
  reason: string;
}

export function InventoryPage() {
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [adjusting, setAdjusting] = useState<InventoryItemDto | null>(null);
  const [form] = Form.useForm<AdjustFormValues>();

  const { data, isLoading } = useInventory();
  const adjustMutation = useAdjustInventory();

  const filtered = (data ?? []).filter((item) => !lowStockOnly || item.isLowStock);

  const openAdjustModal = (record: InventoryItemDto) => {
    setAdjusting(record);
    form.resetFields();
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    if (!adjusting) return;
    const values = await form.validateFields();

    if (values.adjustmentQuantity === 0) {
      notifyError('調整量不可為 0。');
      return;
    }

    try {
      await adjustMutation.mutateAsync({
        productId: adjusting.productId,
        adjustmentQuantity: values.adjustmentQuantity,
        reason: values.reason,
      });
      notifySuccess('庫存已調整');
      setModalOpen(false);
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<InventoryItemDto> = [
    { title: '商品編號', dataIndex: 'sku', width: 140 },
    { title: '商品名稱', dataIndex: 'name' },
    { title: '分類', dataIndex: 'categoryName', render: (v?: string | null) => v ?? '-' },
    { title: '單位', dataIndex: 'unit', width: 80 },
    {
      title: '目前庫存',
      dataIndex: 'currentStock',
      width: 120,
      render: (value: number, record) => (record.isLowStock ? <Tag color="red">{value}</Tag> : value),
    },
    { title: '安全庫存', dataIndex: 'safetyStock', width: 100 },
    {
      title: '狀態',
      dataIndex: 'isLowStock',
      width: 100,
      render: (isLowStock: boolean) => (isLowStock ? <Tag color="red">低於安全庫存</Tag> : <Tag color="green">正常</Tag>),
    },
    {
      title: '操作',
      key: 'actions',
      width: 220,
      render: (_, record) => (
        <>
          <EditButton text onClick={() => openAdjustModal(record)}>
            手動調整
          </EditButton>
          <Link to={`/inventory/${record.productId}/transactions`}>
            <HistoryOutlined /> 異動明細
          </Link>
        </>
      ),
    },
  ];

  return (
    <div className="page-container">
      <PageToolbar
        title="庫存總覽"
        filters={
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
            <Switch size="small" checked={lowStockOnly} onChange={setLowStockOnly} />
            只看低庫存
          </span>
        }
      />

      <Table rowKey="productId" loading={isLoading} dataSource={filtered} columns={columns} />

      <Modal
        title={`手動盤點調整 - ${adjusting?.name ?? ''}`}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={adjustMutation.isPending}
        okText="確定調整"
        cancelText="取消"
        okButtonProps={{ className: 'btn-edit' }}
      >
        <Typography.Paragraph type="secondary">
          目前庫存：{adjusting?.currentStock} {adjusting?.unit}
        </Typography.Paragraph>
        <Form form={form} layout="vertical">
          <Form.Item
            name="adjustmentQuantity"
            label="調整量（正數＝盤盈增加，負數＝盤損減少）"
            rules={[{ required: true, message: '請輸入調整量' }]}
          >
            <InputNumber style={{ width: '100%' }} placeholder="例如：-2 或 5" />
          </Form.Item>
          <Form.Item
            name="reason"
            label="調整原因"
            rules={[{ required: true, message: '請填寫調整原因' }, { max: 200 }]}
          >
            <Input.TextArea rows={3} placeholder="例如：盤點發現破損 2 瓶" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
