import { useMemo } from 'react';
import { MinusCircleOutlined } from '@ant-design/icons';
import { Button, Card, DatePicker, Form, Input, InputNumber, Select, Table, Typography } from 'antd';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { extractErrorMessage } from '@/api/client';
import { useActiveCustomers } from '@/api/customers';
import { useActiveProducts } from '@/api/products';
import { useCreateSalesOrder } from '@/api/salesOrders';
import type { CreateSalesOrderItemRequest } from '@/api/types';
import { AddButton } from '@/components/common/ActionButtons';
import { extractFormErrorMessages, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import { formatCurrency } from '@/utils/format';

interface SalesOrderFormValues {
  customerId?: number;
  orderDate: dayjs.Dayjs;
  note?: string;
  items: CreateSalesOrderItemRequest[];
}

/** 只有 Support（客服部門）能進到這頁：新增出貨單、自動扣庫存（ERP.md §4.4）。 */
export function SalesOrderFormPage() {
  const navigate = useNavigate();
  const [form] = Form.useForm<SalesOrderFormValues>();
  const items = Form.useWatch('items', form) as CreateSalesOrderItemRequest[] | undefined;

  const customersQuery = useActiveCustomers();
  const productsQuery = useActiveProducts();
  const createMutation = useCreateSalesOrder();

  const customerOptions = useMemo(
    () => (customersQuery.data ?? []).map((c) => ({ label: c.name, value: c.id })),
    [customersQuery.data],
  );
  const productOptions = useMemo(
    () => (productsQuery.data ?? []).map((p) => ({ label: `${p.name}（${p.sku}）`, value: p.id, salePrice: p.salePrice, currentStock: p.currentStock })),
    [productsQuery.data],
  );
  // 依 productId 查目前庫存，給「目前庫存」欄位跟數量是否超過庫存的提示用。
  const stockByProductId = useMemo(
    () => new Map((productsQuery.data ?? []).map((p) => [p.id, p.currentStock])),
    [productsQuery.data],
  );

  const totalAmount = (items ?? []).reduce((sum, item) => {
    const quantity = item?.quantity ?? 0;
    const unitPrice = item?.unitPrice ?? 0;
    return sum + quantity * unitPrice;
  }, 0);

  const handleFinish = async (values: SalesOrderFormValues) => {
    try {
      await createMutation.mutateAsync({
        // 可留空＝一般散客（ERP.md §4.4）。
        customerId: values.customerId ?? null,
        // 出貨日期只需要 yyyy-MM-dd，DatePicker 已經不給選時間，這裡固定送當天一開始（00:00）。
        orderDate: values.orderDate?.startOf('day').toISOString(),
        note: values.note,
        items: values.items,
      });
      notifySuccess('出貨單已建立，庫存已自動更新');
      navigate('/sales-orders');
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const handleFinishFailed = (info: { errorFields: { errors: string[] }[] }) => {
    notifyValidationErrors(extractFormErrorMessages(info));
  };

  return (
    <div className="page-container">
      <Typography.Title level={4}>新增出貨單</Typography.Title>

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleFinish}
          onFinishFailed={handleFinishFailed}
          initialValues={{ orderDate: dayjs().startOf('day'), items: [{}] }}
        >
          <Form.Item name="customerId" label="客戶">
            <Select placeholder="不選＝一般散客" allowClear showSearch optionFilterProp="label" options={customerOptions} style={{ maxWidth: 320 }} />
          </Form.Item>

          <Form.Item name="orderDate" label="出貨日期">
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
                              const opt = option as { salePrice?: number } | undefined;
                              if (opt?.salePrice !== undefined) {
                                const current = form.getFieldValue('items') as CreateSalesOrderItemRequest[];
                                current[field.name] = { ...current[field.name], unitPrice: opt.salePrice };
                                form.setFieldsValue({ items: current });
                              }
                            }}
                          />
                        </Form.Item>
                      ),
                    },
                    {
                      title: '目前庫存',
                      key: 'currentStock',
                      width: 100,
                      render: (_, field) => {
                        const productId = items?.[field.name]?.productId;
                        if (productId === undefined) return '-';
                        const stock = stockByProductId.get(productId);
                        if (stock === undefined) return '-';
                        const quantity = items?.[field.name]?.quantity ?? 0;
                        return (
                          <span style={{ color: quantity > stock ? '#dc2626' : undefined }}>{stock}</span>
                        );
                      },
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
                      title: '出貨單價',
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
              建立出貨單
            </AddButton>
            <Button style={{ marginLeft: 12 }} onClick={() => navigate('/sales-orders')}>
              取消
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
