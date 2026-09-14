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
import { notifyError, notifySuccess } from '@/utils/alerts';
import { formatCurrency } from '@/utils/format';

interface SalesOrderFormValues {
  customerId?: number;
  orderDate: dayjs.Dayjs;
  note?: string;
  items: CreateSalesOrderItemRequest[];
}

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
    () =>
      (productsQuery.data ?? []).map((p) => ({
        label: `${p.name}（${p.sku}）目前庫存 ${p.currentStock}`,
        value: p.id,
        salePrice: p.salePrice,
      })),
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
        customerId: values.customerId,
        orderDate: values.orderDate?.toISOString(),
        note: values.note,
        items: values.items,
      });
      notifySuccess('出貨單已建立，庫存已自動扣減');
      navigate('/sales-orders');
    } catch (error) {
      // 庫存不足時，後端整張單會失敗（BusinessRuleException → 400），訊息會直接說明是哪個商品庫存不足。
      notifyError(extractErrorMessage(error));
    }
  };

  return (
    <div className="page-container">
      <Typography.Title level={4}>新增出貨單</Typography.Title>

      <Card>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleFinish}
          initialValues={{ orderDate: dayjs(), items: [{}] }}
        >
          <Form.Item name="customerId" label="客戶（可留空＝一般散客）">
            <Select placeholder="一般散客" allowClear options={customerOptions} style={{ maxWidth: 320 }} />
          </Form.Item>

          <Form.Item name="orderDate" label="出貨日期">
            <DatePicker showTime style={{ maxWidth: 320 }} />
          </Form.Item>

          <Form.Item name="note" label="備註" rules={[{ max: 200 }]}>
            <Input.TextArea rows={2} placeholder="選填" style={{ maxWidth: 480 }} />
          </Form.Item>

          <Typography.Title level={5}>商品明細</Typography.Title>

          <Form.List
            name="items"
            rules={[
              {
                validator: async (_, list) => {
                  if (!list || list.length === 0) {
                    throw new Error('至少需要一筆商品明細');
                  }
                },
              },
            ]}
          >
            {(fields, { add, remove }, { errors }) => (
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
                            style={{ minWidth: 260 }}
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
                      title: '銷售單價',
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

                {errors.length > 0 && (
                  <Typography.Text type="danger" style={{ display: 'block', marginTop: 8 }}>
                    {errors.join('、')}
                  </Typography.Text>
                )}

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
