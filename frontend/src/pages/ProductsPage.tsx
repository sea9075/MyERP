import { useMemo, useState } from 'react';
import { ScanOutlined, UndoOutlined } from '@ant-design/icons';
import { Col, Form, Input, InputNumber, Modal, Row, Select, Space, Switch, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useCategories } from '@/api/categories';
import { extractErrorMessage } from '@/api/client';
import { findProductByBarcode, useCreateProduct, useDeleteProduct, useProducts, useUpdateProduct } from '@/api/products';
import { useActiveSuppliers } from '@/api/suppliers';
import type { ProductDto } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, notifyError, notifySuccess } from '@/utils/alerts';
import { formatCurrency } from '@/utils/format';

interface ProductFormValues {
  sku: string;
  barcode?: string;
  name: string;
  categoryId: number;
  unit: string;
  costPrice: number;
  salePrice: number;
  safetyStock: number;
  supplierId?: number;
}

export function ProductsPage() {
  const [keyword, setKeyword] = useState('');
  const [categoryId, setCategoryId] = useState<number | undefined>();
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [barcodeInput, setBarcodeInput] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ProductDto | null>(null);
  const [form] = Form.useForm<ProductFormValues>();

  const categoriesQuery = useCategories(false);
  const suppliersQuery = useActiveSuppliers();
  const { data, isLoading } = useProducts({
    keyword: keyword || undefined,
    categoryId,
    lowStock: lowStockOnly || undefined,
    includeDeleted,
  });
  const createMutation = useCreateProduct();
  const updateMutation = useUpdateProduct();
  const deleteMutation = useDeleteProduct();

  const categoryOptions = useMemo(
    () => (categoriesQuery.data ?? []).map((c) => ({ label: c.name, value: c.id })),
    [categoriesQuery.data],
  );
  const supplierOptions = useMemo(
    () => (suppliersQuery.data ?? []).map((s) => ({ label: s.name, value: s.id })),
    [suppliersQuery.data],
  );

  const openCreateModal = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEditModal = (record: ProductDto) => {
    setEditing(record);
    form.setFieldsValue({
      sku: record.sku,
      barcode: record.barcode ?? undefined,
      name: record.name,
      categoryId: record.categoryId,
      unit: record.unit,
      costPrice: record.costPrice,
      salePrice: record.salePrice,
      safetyStock: record.safetyStock,
      supplierId: record.supplierId ?? undefined,
    });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, request: { ...values, isDeleted: editing.isDeleted } });
        notifySuccess('商品已更新');
      } else {
        await createMutation.mutateAsync(values);
        notifySuccess('商品已新增');
      }
      setModalOpen(false);
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleDelete = async (record: ProductDto) => {
    const confirmed = await confirmDelete('商品', record.name);
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('商品已刪除');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleRestore = async (record: ProductDto) => {
    try {
      await updateMutation.mutateAsync({
        id: record.id,
        request: {
          sku: record.sku,
          barcode: record.barcode,
          name: record.name,
          categoryId: record.categoryId,
          unit: record.unit,
          costPrice: record.costPrice,
          salePrice: record.salePrice,
          safetyStock: record.safetyStock,
          supplierId: record.supplierId,
          isDeleted: false,
        },
      });
      notifySuccess('商品已恢復');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  /** USB 掃碼槍模擬鍵盤輸入＋Enter（ERP.md §8 Phase 2 項目 10）：掃到就直接帶出關鍵字搜尋。 */
  const handleBarcodeSearch = async (value: string) => {
    const trimmed = value.trim();
    if (!trimmed) return;

    const { product, error } = await findProductByBarcode(trimmed);
    if (product) {
      setKeyword(product.sku);
      notifySuccess(`已找到商品：${product.name}`);
    } else {
      notifyError(error ?? '查無此條碼對應的商品。');
    }
    setBarcodeInput('');
  };

  const columns: ColumnsType<ProductDto> = [
    { title: '商品編號', dataIndex: 'sku', width: 120 },
    { title: '條碼', dataIndex: 'barcode', render: (v?: string | null) => v ?? '-', width: 120 },
    { title: '商品名稱', dataIndex: 'name' },
    { title: '分類', dataIndex: 'categoryName', render: (v?: string | null) => v ?? '-' },
    { title: '單位', dataIndex: 'unit', width: 80 },
    { title: '成本價', dataIndex: 'costPrice', render: (v: number) => formatCurrency(v), width: 110 },
    { title: '售價', dataIndex: 'salePrice', render: (v: number) => formatCurrency(v), width: 110 },
    {
      title: '目前庫存',
      dataIndex: 'currentStock',
      width: 100,
      render: (value: number, record) =>
        record.isLowStock ? <Tag color="red">{value}（低庫存）</Tag> : value,
    },
    { title: '安全庫存', dataIndex: 'safetyStock', width: 90 },
    { title: '常用供應商', dataIndex: 'supplierName', render: (v?: string | null) => v ?? '-' },
    {
      title: '狀態',
      dataIndex: 'isDeleted',
      width: 90,
      render: (isDeleted: boolean) => (isDeleted ? <Tag color="red">已刪除</Tag> : <Tag color="green">正常</Tag>),
    },
    {
      title: '操作',
      key: 'actions',
      width: 200,
      fixed: 'right',
      render: (_, record) =>
        record.isDeleted ? (
          <EditButton text icon={<UndoOutlined />} onClick={() => handleRestore(record)}>
            取消刪除
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
        title="商品管理"
        filters={
          <Space wrap>
            <Input.Search
              placeholder="搜尋商品編號/名稱/條碼"
              allowClear
              style={{ width: 220 }}
              onSearch={setKeyword}
            />
            <Input
              placeholder="條碼掃描輸入後按 Enter"
              prefix={<ScanOutlined />}
              style={{ width: 200 }}
              value={barcodeInput}
              onChange={(e) => setBarcodeInput(e.target.value)}
              onPressEnter={(e) => handleBarcodeSearch(e.currentTarget.value)}
            />
            <Select
              placeholder="全部分類"
              allowClear
              style={{ width: 150 }}
              options={categoryOptions}
              value={categoryId}
              onChange={setCategoryId}
            />
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
              <Switch size="small" checked={lowStockOnly} onChange={setLowStockOnly} />
              只看低庫存
            </span>
            <SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />
          </Space>
        }
        actions={<AddButton onClick={openCreateModal}>新增商品</AddButton>}
      />

      <Table
        rowKey="id"
        loading={isLoading}
        dataSource={data}
        columns={columns}
        scroll={{ x: 1400 }}
        rowClassName={(record) => (record.isLowStock ? 'low-stock-row' : '')}
      />

      <Modal
        title={editing ? '編輯商品' : '新增商品'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        okText={editing ? '儲存' : '新增'}
        cancelText="取消"
        okButtonProps={{ className: editing ? 'btn-edit' : 'btn-add' }}
        width={560}
      >
        <Form form={form} layout="vertical">
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="sku"
                label="商品編號 (SKU)"
                rules={[{ required: true, message: '請輸入商品編號' }, { max: 30 }]}
              >
                <Input placeholder="例如：SKU-0001" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="barcode" label="條碼" rules={[{ max: 30 }]}>
                <Input placeholder="選填" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            name="name"
            label="商品名稱"
            rules={[{ required: true, message: '請輸入商品名稱' }, { max: 100 }]}
          >
            <Input placeholder="例如：可口可樂 330ml" />
          </Form.Item>

          <Row gutter={12}>
            <Col span={14}>
              <Form.Item name="categoryId" label="分類" rules={[{ required: true, message: '請選擇分類' }]}>
                <Select placeholder="請選擇分類" options={categoryOptions} />
              </Form.Item>
            </Col>
            <Col span={10}>
              <Form.Item
                name="unit"
                label="單位"
                rules={[{ required: true, message: '請輸入單位' }, { max: 10 }]}
              >
                <Input placeholder="例如：瓶/箱/個" />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="costPrice"
                label="成本價"
                rules={[{ required: true, message: '請輸入成本價' }]}
              >
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="salePrice"
                label="售價"
                rules={[{ required: true, message: '請輸入售價' }]}
              >
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="safetyStock"
                label="安全庫存量"
                rules={[{ required: true, message: '請輸入安全庫存量' }]}
              >
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="supplierId" label="常用供應商">
                <Select placeholder="選填" allowClear options={supplierOptions} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </div>
  );
}
