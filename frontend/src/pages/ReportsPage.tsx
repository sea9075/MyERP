import { useMemo, useState } from 'react';
import { DownloadOutlined } from '@ant-design/icons';
import { Button, Col, DatePicker, Row, Segmented, Select, Statistic, Switch, Table, Tabs, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useCategories } from '@/api/categories';
import { useActiveCustomers } from '@/api/customers';
import { useActiveProducts } from '@/api/products';
import {
  useGrossMarginReport,
  useInventoryReport,
  usePurchaseReport,
  useSalesReport,
} from '@/api/reports';
import { useActiveSuppliers } from '@/api/suppliers';
import type {
  GrossMarginGroupBy,
  GrossMarginRowDto,
  PurchaseReportByProductDto,
  PurchaseReportBySupplierDto,
  PurchaseReportOrderRowDto,
  SalesReportByCustomerDto,
  SalesReportByProductDto,
  SalesReportOrderRowDto,
  InventoryReportRowDto,
} from '@/api/types';
import { exportToCsv } from '@/utils/csv';
import { formatCurrency, formatDate } from '@/utils/format';

const { RangePicker } = DatePicker;

/** 四個報表分頁預設都是「本月」（依瀏覽器當地日期），使用者可以自己改。 */
function defaultMonthRange(): [dayjs.Dayjs, dayjs.Dayjs] {
  return [dayjs().startOf('month'), dayjs().endOf('month')];
}

function formatPercent(value: number): string {
  return `${value.toFixed(2)}%`;
}

// ===================== 進貨統計 =====================

function PurchaseReportTab() {
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>(defaultMonthRange());
  const [supplierId, setSupplierId] = useState<number | undefined>();
  const [categoryId, setCategoryId] = useState<number | undefined>();
  const [productId, setProductId] = useState<number | undefined>();
  const [viewMode, setViewMode] = useState<'supplier' | 'product' | 'orders'>('supplier');

  const { data: suppliers } = useActiveSuppliers();
  const { data: categories } = useCategories(false);
  const { data: products } = useActiveProducts();

  const supplierOptions = useMemo(() => (suppliers ?? []).map((s) => ({ value: s.id, label: s.name })), [suppliers]);
  const categoryOptions = useMemo(() => (categories ?? []).map((c) => ({ value: c.id, label: c.name })), [categories]);
  const productOptions = useMemo(() => (products ?? []).map((p) => ({ value: p.id, label: p.name })), [products]);

  const { data, isLoading } = usePurchaseReport({
    dateFrom: dateRange[0].format('YYYY-MM-DD'),
    dateTo: dateRange[1].format('YYYY-MM-DD'),
    supplierId,
    categoryId,
    productId,
  });

  const supplierColumns: ColumnsType<PurchaseReportBySupplierDto> = [
    { title: '供應商', dataIndex: 'supplierName' },
    { title: '進貨單數', dataIndex: 'orderCount', width: 100, align: 'right' },
    { title: '進貨數量', dataIndex: 'totalQuantity', width: 120, align: 'right' },
    { title: '進貨金額', dataIndex: 'totalAmount', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
  ];
  const productColumns: ColumnsType<PurchaseReportByProductDto> = [
    { title: '商品編號', dataIndex: 'sku', width: 140 },
    { title: '商品名稱', dataIndex: 'productName' },
    { title: '分類', dataIndex: 'categoryName', render: (v?: string | null) => v ?? '-' },
    { title: '進貨數量', dataIndex: 'totalQuantity', width: 120, align: 'right' },
    { title: '進貨金額', dataIndex: 'totalAmount', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
    { title: '平均單價', dataIndex: 'averageUnitPrice', width: 120, align: 'right', render: (v: number) => formatCurrency(v) },
  ];
  const orderColumns: ColumnsType<PurchaseReportOrderRowDto> = [
    { title: '單號', dataIndex: 'orderNo', width: 180 },
    { title: '日期', dataIndex: 'orderDate', width: 120, render: (v: string) => formatDate(v) },
    { title: '供應商', dataIndex: 'supplierName' },
    { title: '金額', dataIndex: 'totalAmount', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
  ];

  const handleExport = () => {
    if (!data) return;
    const from = dateRange[0].format('YYYYMMDD');
    const to = dateRange[1].format('YYYYMMDD');
    if (viewMode === 'supplier') {
      exportToCsv(`進貨統計_依供應商_${from}-${to}.csv`, [
        { header: '供應商', accessor: (r: PurchaseReportBySupplierDto) => r.supplierName },
        { header: '進貨單數', accessor: (r: PurchaseReportBySupplierDto) => r.orderCount },
        { header: '進貨數量', accessor: (r: PurchaseReportBySupplierDto) => r.totalQuantity },
        { header: '進貨金額', accessor: (r: PurchaseReportBySupplierDto) => r.totalAmount },
      ], data.bySupplier);
    } else if (viewMode === 'product') {
      exportToCsv(`進貨統計_依商品_${from}-${to}.csv`, [
        { header: '商品編號', accessor: (r: PurchaseReportByProductDto) => r.sku },
        { header: '商品名稱', accessor: (r: PurchaseReportByProductDto) => r.productName },
        { header: '分類', accessor: (r: PurchaseReportByProductDto) => r.categoryName ?? '' },
        { header: '進貨數量', accessor: (r: PurchaseReportByProductDto) => r.totalQuantity },
        { header: '進貨金額', accessor: (r: PurchaseReportByProductDto) => r.totalAmount },
        { header: '平均單價', accessor: (r: PurchaseReportByProductDto) => r.averageUnitPrice },
      ], data.byProduct);
    } else {
      exportToCsv(`進貨統計_單據明細_${from}-${to}.csv`, [
        { header: '單號', accessor: (r: PurchaseReportOrderRowDto) => r.orderNo },
        { header: '日期', accessor: (r: PurchaseReportOrderRowDto) => formatDate(r.orderDate) },
        { header: '供應商', accessor: (r: PurchaseReportOrderRowDto) => r.supplierName },
        { header: '金額', accessor: (r: PurchaseReportOrderRowDto) => r.totalAmount },
      ], data.orders);
    }
  };

  return (
    <div>
      <Row gutter={[12, 12]} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <RangePicker
            value={dateRange}
            onChange={(v) => v && setDateRange(v as [dayjs.Dayjs, dayjs.Dayjs])}
            allowClear={false}
          />
        </Col>
        <Col>
          <Select placeholder="全部供應商" allowClear style={{ width: 160 }} options={supplierOptions} value={supplierId} onChange={setSupplierId} />
        </Col>
        <Col>
          <Select placeholder="全部分類" allowClear style={{ width: 140 }} options={categoryOptions} value={categoryId} onChange={setCategoryId} />
        </Col>
        <Col>
          <Select placeholder="全部商品" allowClear showSearch optionFilterProp="label" style={{ width: 180 }} options={productOptions} value={productId} onChange={setProductId} />
        </Col>
      </Row>

      <Row gutter={24} style={{ marginBottom: 16 }}>
        <Col><Statistic title="進貨單數" value={data?.totalOrderCount ?? 0} /></Col>
        <Col><Statistic title="進貨數量" value={data?.totalQuantity ?? 0} /></Col>
        <Col><Statistic title="進貨總金額" value={data?.totalAmount ?? 0} formatter={(v) => formatCurrency(Number(v))} /></Col>
      </Row>

      <Row justify="space-between" align="middle" style={{ marginBottom: 12 }}>
        <Segmented
          value={viewMode}
          onChange={(v) => setViewMode(v as typeof viewMode)}
          options={[{ label: '依供應商', value: 'supplier' }, { label: '依商品', value: 'product' }, { label: '單據明細', value: 'orders' }]}
        />
        <Button icon={<DownloadOutlined />} onClick={handleExport} disabled={!data}>匯出 CSV</Button>
      </Row>

      {viewMode === 'supplier' && (
        <Table rowKey="supplierId" loading={isLoading} dataSource={data?.bySupplier} columns={supplierColumns} pagination={false} />
      )}
      {viewMode === 'product' && (
        <Table rowKey="productId" loading={isLoading} dataSource={data?.byProduct} columns={productColumns} pagination={false} />
      )}
      {viewMode === 'orders' && (
        <Table rowKey="orderId" loading={isLoading} dataSource={data?.orders} columns={orderColumns} pagination={{ pageSize: 20 }} />
      )}
    </div>
  );
}

// ===================== 銷售統計 =====================

function SalesReportTab() {
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>(defaultMonthRange());
  const [customerId, setCustomerId] = useState<number | undefined>();
  const [categoryId, setCategoryId] = useState<number | undefined>();
  const [productId, setProductId] = useState<number | undefined>();
  const [viewMode, setViewMode] = useState<'customer' | 'product' | 'orders'>('customer');

  const { data: customers } = useActiveCustomers();
  const { data: categories } = useCategories(false);
  const { data: products } = useActiveProducts();

  const customerOptions = useMemo(() => (customers ?? []).map((c) => ({ value: c.id, label: c.name })), [customers]);
  const categoryOptions = useMemo(() => (categories ?? []).map((c) => ({ value: c.id, label: c.name })), [categories]);
  const productOptions = useMemo(() => (products ?? []).map((p) => ({ value: p.id, label: p.name })), [products]);

  const { data, isLoading } = useSalesReport({
    dateFrom: dateRange[0].format('YYYY-MM-DD'),
    dateTo: dateRange[1].format('YYYY-MM-DD'),
    customerId,
    categoryId,
    productId,
  });

  const customerColumns: ColumnsType<SalesReportByCustomerDto> = [
    { title: '客戶', dataIndex: 'customerName' },
    { title: '訂單數', dataIndex: 'orderCount', width: 100, align: 'right' },
    { title: '銷售金額', dataIndex: 'totalAmount', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
  ];
  const productColumns: ColumnsType<SalesReportByProductDto> = [
    { title: '商品編號', dataIndex: 'sku', width: 140 },
    { title: '商品名稱', dataIndex: 'productName' },
    { title: '分類', dataIndex: 'categoryName', render: (v?: string | null) => v ?? '-' },
    { title: '銷售數量', dataIndex: 'totalQuantity', width: 120, align: 'right' },
    { title: '銷售金額', dataIndex: 'totalAmount', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
    { title: '平均售價', dataIndex: 'averageUnitPrice', width: 120, align: 'right', render: (v: number) => formatCurrency(v) },
  ];
  const orderColumns: ColumnsType<SalesReportOrderRowDto> = [
    { title: '單號', dataIndex: 'orderNo', width: 180 },
    { title: '日期', dataIndex: 'orderDate', width: 120, render: (v: string) => formatDate(v) },
    { title: '客戶', dataIndex: 'customerName' },
    { title: '金額', dataIndex: 'totalAmount', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
  ];

  const handleExport = () => {
    if (!data) return;
    const from = dateRange[0].format('YYYYMMDD');
    const to = dateRange[1].format('YYYYMMDD');
    if (viewMode === 'customer') {
      exportToCsv(`銷售統計_依客戶_${from}-${to}.csv`, [
        { header: '客戶', accessor: (r: SalesReportByCustomerDto) => r.customerName },
        { header: '訂單數', accessor: (r: SalesReportByCustomerDto) => r.orderCount },
        { header: '銷售金額', accessor: (r: SalesReportByCustomerDto) => r.totalAmount },
      ], data.byCustomer);
    } else if (viewMode === 'product') {
      exportToCsv(`銷售統計_依商品_${from}-${to}.csv`, [
        { header: '商品編號', accessor: (r: SalesReportByProductDto) => r.sku },
        { header: '商品名稱', accessor: (r: SalesReportByProductDto) => r.productName },
        { header: '分類', accessor: (r: SalesReportByProductDto) => r.categoryName ?? '' },
        { header: '銷售數量', accessor: (r: SalesReportByProductDto) => r.totalQuantity },
        { header: '銷售金額', accessor: (r: SalesReportByProductDto) => r.totalAmount },
        { header: '平均售價', accessor: (r: SalesReportByProductDto) => r.averageUnitPrice },
      ], data.byProduct);
    } else {
      exportToCsv(`銷售統計_單據明細_${from}-${to}.csv`, [
        { header: '單號', accessor: (r: SalesReportOrderRowDto) => r.orderNo },
        { header: '日期', accessor: (r: SalesReportOrderRowDto) => formatDate(r.orderDate) },
        { header: '客戶', accessor: (r: SalesReportOrderRowDto) => r.customerName },
        { header: '金額', accessor: (r: SalesReportOrderRowDto) => r.totalAmount },
      ], data.orders);
    }
  };

  return (
    <div>
      <Row gutter={[12, 12]} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <RangePicker
            value={dateRange}
            onChange={(v) => v && setDateRange(v as [dayjs.Dayjs, dayjs.Dayjs])}
            allowClear={false}
          />
        </Col>
        <Col>
          <Select placeholder="全部客戶" allowClear style={{ width: 160 }} options={customerOptions} value={customerId} onChange={setCustomerId} />
        </Col>
        <Col>
          <Select placeholder="全部分類" allowClear style={{ width: 140 }} options={categoryOptions} value={categoryId} onChange={setCategoryId} />
        </Col>
        <Col>
          <Select placeholder="全部商品" allowClear showSearch optionFilterProp="label" style={{ width: 180 }} options={productOptions} value={productId} onChange={setProductId} />
        </Col>
      </Row>

      <Row gutter={24} style={{ marginBottom: 16 }}>
        <Col><Statistic title="訂單數" value={data?.totalOrderCount ?? 0} /></Col>
        <Col><Statistic title="銷售數量" value={data?.totalQuantity ?? 0} /></Col>
        <Col><Statistic title="銷售總金額" value={data?.totalAmount ?? 0} formatter={(v) => formatCurrency(Number(v))} /></Col>
      </Row>

      <Row justify="space-between" align="middle" style={{ marginBottom: 12 }}>
        <Segmented
          value={viewMode}
          onChange={(v) => setViewMode(v as typeof viewMode)}
          options={[{ label: '依客戶', value: 'customer' }, { label: '依商品', value: 'product' }, { label: '單據明細', value: 'orders' }]}
        />
        <Button icon={<DownloadOutlined />} onClick={handleExport} disabled={!data}>匯出 CSV</Button>
      </Row>

      {viewMode === 'customer' && (
        <Table rowKey={(r) => r.customerId ?? 'walk-in'} loading={isLoading} dataSource={data?.byCustomer} columns={customerColumns} pagination={false} />
      )}
      {viewMode === 'product' && (
        <Table rowKey="productId" loading={isLoading} dataSource={data?.byProduct} columns={productColumns} pagination={false} />
      )}
      {viewMode === 'orders' && (
        <Table rowKey="orderId" loading={isLoading} dataSource={data?.orders} columns={orderColumns} pagination={{ pageSize: 20 }} />
      )}
    </div>
  );
}

// ===================== 毛利報表 =====================

function GrossMarginReportTab() {
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>(defaultMonthRange());
  const [categoryId, setCategoryId] = useState<number | undefined>();
  const [productId, setProductId] = useState<number | undefined>();
  const [groupBy, setGroupBy] = useState<GrossMarginGroupBy>('product');

  const { data: categories } = useCategories(false);
  const { data: products } = useActiveProducts();

  const categoryOptions = useMemo(() => (categories ?? []).map((c) => ({ value: c.id, label: c.name })), [categories]);
  const productOptions = useMemo(() => (products ?? []).map((p) => ({ value: p.id, label: p.name })), [products]);

  const { data, isLoading } = useGrossMarginReport({
    dateFrom: dateRange[0].format('YYYY-MM-DD'),
    dateTo: dateRange[1].format('YYYY-MM-DD'),
    categoryId,
    productId,
    groupBy,
  });

  const isByProduct = groupBy === 'product';

  const columns: ColumnsType<GrossMarginRowDto> = isByProduct
    ? [
        { title: '商品編號', dataIndex: 'sku', width: 140 },
        { title: '商品名稱', dataIndex: 'name' },
        { title: '分類', dataIndex: 'categoryName', render: (v?: string | null) => v ?? '-' },
        { title: '銷售數量', dataIndex: 'quantitySold', width: 110, align: 'right' },
        { title: '銷售金額', dataIndex: 'salesAmount', width: 130, align: 'right', render: (v: number) => formatCurrency(v) },
        { title: '成本', dataIndex: 'costAmount', width: 130, align: 'right', render: (v: number) => formatCurrency(v) },
        { title: '毛利', dataIndex: 'grossProfit', width: 130, align: 'right', render: (v: number) => formatCurrency(v) },
        { title: '毛利率', dataIndex: 'grossMarginPercent', width: 100, align: 'right', render: (v: number) => formatPercent(v) },
      ]
    : [
        { title: '分類', dataIndex: 'name' },
        { title: '銷售數量', dataIndex: 'quantitySold', width: 110, align: 'right' },
        { title: '銷售金額', dataIndex: 'salesAmount', width: 130, align: 'right', render: (v: number) => formatCurrency(v) },
        { title: '成本', dataIndex: 'costAmount', width: 130, align: 'right', render: (v: number) => formatCurrency(v) },
        { title: '毛利', dataIndex: 'grossProfit', width: 130, align: 'right', render: (v: number) => formatCurrency(v) },
        { title: '毛利率', dataIndex: 'grossMarginPercent', width: 100, align: 'right', render: (v: number) => formatPercent(v) },
      ];

  const handleExport = () => {
    if (!data) return;
    const from = dateRange[0].format('YYYYMMDD');
    const to = dateRange[1].format('YYYYMMDD');
    exportToCsv(`毛利報表_${isByProduct ? '依商品' : '依分類'}_${from}-${to}.csv`, [
      ...(isByProduct ? [{ header: '商品編號', accessor: (r: GrossMarginRowDto) => r.sku ?? '' }] : []),
      { header: isByProduct ? '商品名稱' : '分類', accessor: (r: GrossMarginRowDto) => r.name },
      { header: '銷售數量', accessor: (r: GrossMarginRowDto) => r.quantitySold },
      { header: '銷售金額', accessor: (r: GrossMarginRowDto) => r.salesAmount },
      { header: '成本', accessor: (r: GrossMarginRowDto) => r.costAmount },
      { header: '毛利', accessor: (r: GrossMarginRowDto) => r.grossProfit },
      { header: '毛利率(%)', accessor: (r: GrossMarginRowDto) => r.grossMarginPercent },
    ], data.rows);
  };

  return (
    <div>
      <Typography.Paragraph type="secondary" style={{ marginBottom: 12 }}>
        成本用商品「目前」的參考成本價估算，不是賣出當下的歷史成本（系統沒有做批次/歷史成本快照）。
      </Typography.Paragraph>

      <Row gutter={[12, 12]} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <RangePicker
            value={dateRange}
            onChange={(v) => v && setDateRange(v as [dayjs.Dayjs, dayjs.Dayjs])}
            allowClear={false}
          />
        </Col>
        <Col>
          <Select placeholder="全部分類" allowClear style={{ width: 140 }} options={categoryOptions} value={categoryId} onChange={setCategoryId} />
        </Col>
        <Col>
          <Select placeholder="全部商品" allowClear showSearch optionFilterProp="label" style={{ width: 180 }} options={productOptions} value={productId} onChange={setProductId} />
        </Col>
        <Col>
          <Segmented
            value={groupBy}
            onChange={(v) => setGroupBy(v as GrossMarginGroupBy)}
            options={[{ label: '依商品', value: 'product' }, { label: '依分類', value: 'category' }]}
          />
        </Col>
      </Row>

      <Row gutter={24} style={{ marginBottom: 16 }}>
        <Col><Statistic title="總銷售額" value={data?.totalSalesAmount ?? 0} formatter={(v) => formatCurrency(Number(v))} /></Col>
        <Col><Statistic title="總成本" value={data?.totalCostAmount ?? 0} formatter={(v) => formatCurrency(Number(v))} /></Col>
        <Col><Statistic title="總毛利" value={data?.totalGrossProfit ?? 0} formatter={(v) => formatCurrency(Number(v))} /></Col>
        <Col><Statistic title="整體毛利率" value={data?.overallGrossMarginPercent ?? 0} formatter={(v) => formatPercent(Number(v))} /></Col>
      </Row>

      <Row justify="end" style={{ marginBottom: 12 }}>
        <Button icon={<DownloadOutlined />} onClick={handleExport} disabled={!data}>匯出 CSV</Button>
      </Row>

      <Table
        rowKey={(r) => r.productId ?? r.name}
        loading={isLoading}
        dataSource={data?.rows}
        columns={columns}
        pagination={{ pageSize: 20 }}
      />
    </div>
  );
}

// ===================== 庫存總覽/低庫存清單 =====================

function InventoryReportTab() {
  const [categoryId, setCategoryId] = useState<number | undefined>();
  const [lowStockOnly, setLowStockOnly] = useState(false);

  const { data: categories } = useCategories(false);
  const categoryOptions = useMemo(() => (categories ?? []).map((c) => ({ value: c.id, label: c.name })), [categories]);

  const { data, isLoading } = useInventoryReport({ categoryId, lowStockOnly });

  const columns: ColumnsType<InventoryReportRowDto> = [
    { title: '商品編號', dataIndex: 'sku', width: 140 },
    { title: '商品名稱', dataIndex: 'name' },
    { title: '分類', dataIndex: 'categoryName', render: (v?: string | null) => v ?? '-' },
    { title: '單位', dataIndex: 'unit', width: 80 },
    { title: '目前庫存', dataIndex: 'currentStock', width: 100, align: 'right' },
    { title: '安全庫存', dataIndex: 'safetyStock', width: 100, align: 'right' },
    {
      title: '狀態',
      dataIndex: 'isLowStock',
      width: 100,
      render: (v: boolean) => (v ? <Typography.Text type="danger">低於安全庫存</Typography.Text> : <Typography.Text type="success">正常</Typography.Text>),
    },
    { title: '估計庫存金額', dataIndex: 'estimatedValue', width: 140, align: 'right', render: (v: number) => formatCurrency(v) },
  ];

  const handleExport = () => {
    if (!data) return;
    exportToCsv(`庫存總覽_${dayjs().format('YYYYMMDD')}.csv`, [
      { header: '商品編號', accessor: (r: InventoryReportRowDto) => r.sku },
      { header: '商品名稱', accessor: (r: InventoryReportRowDto) => r.name },
      { header: '分類', accessor: (r: InventoryReportRowDto) => r.categoryName ?? '' },
      { header: '單位', accessor: (r: InventoryReportRowDto) => r.unit },
      { header: '目前庫存', accessor: (r: InventoryReportRowDto) => r.currentStock },
      { header: '安全庫存', accessor: (r: InventoryReportRowDto) => r.safetyStock },
      { header: '是否低於安全庫存', accessor: (r: InventoryReportRowDto) => (r.isLowStock ? '是' : '否') },
      { header: '估計庫存金額', accessor: (r: InventoryReportRowDto) => r.estimatedValue },
    ], data.rows);
  };

  return (
    <div>
      <Row gutter={[12, 12]} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Select placeholder="全部分類" allowClear style={{ width: 140 }} options={categoryOptions} value={categoryId} onChange={setCategoryId} />
        </Col>
        <Col>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
            <Switch size="small" checked={lowStockOnly} onChange={setLowStockOnly} />
            只看低庫存
          </span>
        </Col>
      </Row>

      <Row gutter={24} style={{ marginBottom: 16 }}>
        <Col><Statistic title="低庫存品項數" value={data?.lowStockCount ?? 0} /></Col>
        <Col><Statistic title="估計庫存總值" value={data?.totalEstimatedValue ?? 0} formatter={(v) => formatCurrency(Number(v))} /></Col>
      </Row>

      <Row justify="end" style={{ marginBottom: 12 }}>
        <Button icon={<DownloadOutlined />} onClick={handleExport} disabled={!data}>匯出 CSV</Button>
      </Row>

      <Table rowKey="productId" loading={isLoading} dataSource={data?.rows} columns={columns} pagination={{ pageSize: 20 }} />
    </div>
  );
}

// ===================== 頁面入口 =====================

/**
 * 報表模組（ERP.md §4.6 Phase 3，Infra-Progress.md §27，2026-09-15 新增）。只有 Manager/Admin
 * 看得到（見 App.tsx 的 RequireDepartment 跟 AppLayout 選單），四個報表用 Tabs 切換，
 * 各自獨立管理篩選條件跟畫面狀態。
 */
export function ReportsPage() {
  const items = useMemo(
    () => [
      { key: 'purchases', label: '進貨統計', children: <PurchaseReportTab /> },
      { key: 'sales', label: '銷售統計', children: <SalesReportTab /> },
      { key: 'gross-margin', label: '毛利報表', children: <GrossMarginReportTab /> },
      { key: 'inventory', label: '庫存總覽/低庫存清單', children: <InventoryReportTab /> },
    ],
    [],
  );

  return (
    <div className="page-container">
      <Typography.Title level={4} className="reports-page-title" style={{ marginTop: 0, marginBottom: 16 }}>
        報表
      </Typography.Title>
      <Tabs items={items} />
    </div>
  );
}
