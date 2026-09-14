import { useMemo } from 'react';
import { InboxOutlined, ShoppingCartOutlined, WarningOutlined } from '@ant-design/icons';
import { Alert, Card, Col, Row, Statistic, Table, Typography } from 'antd';
import dayjs from 'dayjs';
import { Link } from 'react-router-dom';
import { usePurchaseOrders } from '@/api/purchaseOrders';
import { useSalesOrders } from '@/api/salesOrders';
import { useInventory } from '@/api/inventory';
import { formatCurrency } from '@/utils/format';

/**
 * ERP.md §7 原本規劃儀表板要有「今日進/出貨摘要、低庫存警示卡片」，但報表 API
 * （/api/reports/...）後端還沒開發，所以這裡改用現有的「進貨單/出貨單依日期查詢」+
 * 「即時庫存列表」API，在前端自己算出今天的摘要，不等報表模組完成。
 */
export function DashboardPage() {
  const { startOfToday, endOfToday } = useMemo(() => {
    const start = dayjs().startOf('day').toDate().toISOString();
    const end = dayjs().endOf('day').toDate().toISOString();
    return { startOfToday: start, endOfToday: end };
  }, []);

  const purchaseOrdersQuery = usePurchaseOrders({ dateFrom: startOfToday, dateTo: endOfToday });
  const salesOrdersQuery = useSalesOrders({ dateFrom: startOfToday, dateTo: endOfToday });
  const inventoryQuery = useInventory();

  const todayPurchaseOrders = (purchaseOrdersQuery.data ?? []).filter((o) => o.status === 'Normal');
  const todaySalesOrders = (salesOrdersQuery.data ?? []).filter((o) => o.status === 'Normal');
  const todayPurchaseAmount = todayPurchaseOrders.reduce((sum, o) => sum + o.totalAmount, 0);
  const todaySalesAmount = todaySalesOrders.reduce((sum, o) => sum + o.totalAmount, 0);
  const lowStockItems = (inventoryQuery.data ?? []).filter((item) => item.isLowStock);

  return (
    <div className="page-container">
      <Typography.Title level={4}>儀表板</Typography.Title>
      <Typography.Paragraph type="secondary">{dayjs().format('YYYY年MM月DD日 dddd')}</Typography.Paragraph>

      <Row gutter={16}>
        <Col xs={24} sm={12} lg={8}>
          <Card loading={purchaseOrdersQuery.isLoading}>
            <Statistic
              title={
                <Link to="/purchase-orders">
                  <InboxOutlined /> 今日進貨單
                </Link>
              }
              value={todayPurchaseOrders.length}
              suffix="張"
            />
            <Typography.Text type="secondary">進貨金額 {formatCurrency(todayPurchaseAmount)}</Typography.Text>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card loading={salesOrdersQuery.isLoading}>
            <Statistic
              title={
                <Link to="/sales-orders">
                  <ShoppingCartOutlined /> 今日出貨單
                </Link>
              }
              value={todaySalesOrders.length}
              suffix="張"
            />
            <Typography.Text type="secondary">銷售金額 {formatCurrency(todaySalesAmount)}</Typography.Text>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card loading={inventoryQuery.isLoading}>
            <Statistic
              title={
                <Link to="/inventory">
                  <WarningOutlined /> 低庫存商品
                </Link>
              }
              value={lowStockItems.length}
              suffix="項"
              valueStyle={lowStockItems.length > 0 ? { color: '#dc2626' } : undefined}
            />
            <Typography.Text type="secondary">低於安全庫存量，建議盡快補貨</Typography.Text>
          </Card>
        </Col>
      </Row>

      <Card
        title="低庫存清單"
        style={{ marginTop: 24 }}
        extra={<Link to="/inventory">查看完整庫存 &gt;</Link>}
      >
        {lowStockItems.length === 0 ? (
          <Alert type="success" showIcon message="目前沒有商品低於安全庫存。" />
        ) : (
          <Table
            rowKey="productId"
            size="small"
            pagination={false}
            loading={inventoryQuery.isLoading}
            dataSource={lowStockItems}
            columns={[
              { title: '商品編號', dataIndex: 'sku' },
              { title: '商品名稱', dataIndex: 'name' },
              { title: '分類', dataIndex: 'categoryName', render: (value?: string | null) => value ?? '-' },
              { title: '目前庫存', dataIndex: 'currentStock' },
              { title: '安全庫存', dataIndex: 'safetyStock' },
              { title: '單位', dataIndex: 'unit' },
            ]}
          />
        )}
      </Card>
    </div>
  );
}
