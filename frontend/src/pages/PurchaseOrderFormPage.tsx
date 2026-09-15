import { useMemo } from 'react';
import { MinusCircleOutlined } from '@ant-design/icons';
import { Button, Card, DatePicker, Form, Input, InputNumber, Select, Table, Typography } from 'antd';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { extractErrorMessage } from '@/api/client';
import { useCreatePurchaseOrder } from '@/api/purchaseOrders';
import { useActiveSuppliers } from '@/api/suppliers';
import { useActiveProducts } from '@/api/products';
import type { CreatePurchaseOrderItemRequest } from '@/api/types';
import { AddButton } from '@/components/common/ActionButtons';
import { extractFormErrorMessages, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import { formatCurrency } from '@/utils/format';

interface PurchaseOrderFormValues {
  supplierId: number;
  orderDate: dayjs.Dayjs;
  note?: string;
  items: CreatePurchaseOrderItemRequest[];
}

export function PurchaseOrderFormPage() {
  const navigate = useNavigate();
  const [form] = Form.useForm<PurchaseOrderFormValues>();
  const items = Form.useWatch('items', form) as CreatePurchaseOrderItemRequest[] | undefined;

  const suppliersQuery = useActiveSuppliers();
  const productsQuery = useActiveProducts();
  const createMutation = useCreatePurchaseOrder();

  const supplierOptions = useMemo(
    () => (suppliersQuery.data ?? []).map((s) => ({ label: s.name, value: s.id })),
    [suppliersQuery.data],
  );
  const productOptions = useMemo(
    () => (productsQuery.data ?? []).map((p) => ({ label: `${p.name}（${p.sku}）`, value: p.id, costPrice: p.costPrice })),
    [productsQuery.data],
  );

  const totalAmount = (items ?? []).reduce((sum, item) => {
    const quantity = item?.quantity ?? 0;
    const unitPrice = item?.unitPrice ?? 0;
    return sum + quantity * unitPrice;
  }, 0);

  const handleFinish = async (values: PurchaseOrderFormValues) => {
    try {
      await createMutation.mutateAsync({
        supplierId: values.supplierId,
        // 進貨日期只需要 yyyy-MM-dd，DatePicker 已經不給選時間，這裡固定送當天一開始（00:00）。
        orderDate: values.orderDate?.startOf('day').toISOString(),
        note: values.note,
        items: values.items,
      });
      notifySuccess('進貨單已建立，庫存已自動更新');
      navigate('/purchase-orders');
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const handleFinishFailed = (info: { errorFields: { errors: string[] }[] }) => {
    notifyValidationErrors(extractFormErrorMessages(info));
  };

  return (
    <div className="page-container">
      <Typography.Title level={4}>新增進貨單</Typography.Title>

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleFinish}
          onFinishFailed={handleFinishFailed}
          initialValues={{ orderDate: dayjs().startOf('day'), items: [{}] }}
        >
          <Form.Item name="supplierId" label="供應商" rules={[{ required: true, message: '請選擇供應商' }]}>
            <Select placeholder="請選擇供應商" options={supplierOptions} style={{ maxWidth: 320 }} />
          </Form.Item>

          <Form.Item name="orderDate" label="進貨日期">
            <DatePicker format="YYYY-MM-DD" style={{ maxWidth: 320 }} />
          </Form.Item>

          <Form.Item name="note" label="備註" rules={[{ max: 200 }]}>
            <Input.TextArea rows={2} placeholder="選填" style={{ maxWidth: 480 }} />
          </Form.Item>

          <Typography.Title level={5}>商品明細</Typography.Title>

          <Form.List name="items" rules={[{ validator: async (_, list) => {
            if (!list || list.length === 0) {
              throw new Error('至少需要一筆商品明細');
            }
          } }]}>
            {(fields, { add, remove }) => (
              <>
                <Table
                  rowKey={(field) => field.key}
                  dataSource={fields}
                  pagination={false}
                  size="small"
                  columns={[
                    {
                      title: '商品',
                      dataIndex: 'name',
                      render: (_, field) => (
                        <Form.Item
                          name={[field.name, 'productId']}
                          noStyle
                          rules={[{ required: true, message: '請選擇商品' }]}
                        >
                          <Select
                            placeholder="請選擇商品"
                            showSearch
                            optionFilterProp="label"
                            options={productOptions}
                            style={{ minWidth: 220 }}
                            onChange={(_, option) => {
                              const opt = option as { costPrice?: number } | undefined;
                              if (opt?.costPrice !== undefined) {
                                const current = form.getFieldValue('items') as CreatePurchaseOrderItemRequest[];
                                current[field.name] = { ...current[field.name], unitPrice: opt.costPrice };
                                form.setFieldsValue({ items: current });
                              }
                            }}
                          />
                        </Form.Item>
                      ),
                    },
                    {
                      title: '數量',
                      dataIndex: 'quantity',
                      width: 120,
                      render: (_, field) => (
                        <Form.Item
                          name={[field.name, 'quantity']}
                          noStyle
                          rules={[{ required: true, message: '請輸入數量' }]}
                        >
                          <InputNumber min={1} style={{ width: '100%' }} placeholder="數量" />
                        </Form.Item>
                      ),
                    },
                    {
                      title: '進貨單價',
                      dataIndex: 'unitPrice',
                      width: 140,
                      render: (_, field) => (
                        <Form.Item
                          name={[field.name, 'unitPrice']}
                          noStyle
                          rules={[{ required: true, message: '請輸入單價' }]}
                        >
                          <InputNumber min={0} style={{ width: '100%' }} placeholder="單價" />
                        </Form.Item>
                      ),
                    },
                    {
                      title: '小計',
                      key: 'subtotal',
                      width: 120,
                      render: (_, field) => {
                        const item = items?.[field.name];
                        const subtotal = (item?.quantity ?? 0) * (item?.unitPrice ?? 0);
                        return formatCurrency(subtotal);
                      },
                    },
                    {
                      title: '',
                      key: 'remove',
                      width: 50,
                      render: (_, field) =>
                        fields.length > 1 ? (
                          <MinusCircleOutlined style={{ color: '#dc2626' }} onClick={() => remove(field.name)} />
                        ) : null,
                    },
                  ]}
                />

                <AddButton style={{ marginTop: 12 }} onClick={() => add({})}>
                  新增明細
                </AddButton>
              </>
            )}
          </Form.List>

          <Typography.Title level={5} style={{ marginTop: 24 }}>
            總金額：{formatCurrency(totalAmount)}
          </Typography.Title>

          <Form.Item style={{ marginTop: 24 }}>
            <AddButton htmlType="submit" loading={createMutation.isPending}>
              建立進貨單
            </AddButton>
            <Button style={{ marginLeft: 12 }} onClick={() => navigate('/purchase-orders')}>
              取消
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
