// 這份檔案的型別是照後端 MyErp.Application/DTOs 底下的實際 C# DTO 逐一對應過來的，
// 欄位名稱、選填/必填都跟後端一致，避免前端自己猜欄位長怎樣。

export type UserRole = 'Admin' | 'Staff';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  userId: number;
  username: string;
  displayName: string;
  role: UserRole;
}

interface AuditableDto {
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
  isDeleted: boolean;
}

export interface CategoryDto extends AuditableDto {
  id: number;
  name: string;
}

export interface CreateCategoryRequest {
  name: string;
}

export type UpdateCategoryRequest = CreateCategoryRequest;

export interface SupplierDto extends AuditableDto {
  id: number;
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
  address?: string | null;
  note?: string | null;
}

export interface CreateSupplierRequest {
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
  address?: string | null;
  note?: string | null;
}

export interface UpdateSupplierRequest extends CreateSupplierRequest {
  isDeleted: boolean;
}

export interface CustomerDto extends AuditableDto {
  id: number;
  name: string;
  phone?: string | null;
  note?: string | null;
}

export interface CreateCustomerRequest {
  name: string;
  phone?: string | null;
  note?: string | null;
}

export type UpdateCustomerRequest = CreateCustomerRequest;

export interface ProductDto extends AuditableDto {
  id: number;
  sku: string;
  barcode?: string | null;
  name: string;
  categoryId: number;
  categoryName?: string | null;
  unit: string;
  costPrice: number;
  salePrice: number;
  safetyStock: number;
  currentStock: number;
  isLowStock: boolean;
  supplierId?: number | null;
  supplierName?: string | null;
}

export interface CreateProductRequest {
  sku: string;
  barcode?: string | null;
  name: string;
  categoryId: number;
  unit: string;
  costPrice: number;
  salePrice: number;
  safetyStock: number;
  supplierId?: number | null;
}

export interface UpdateProductRequest extends CreateProductRequest {
  isDeleted: boolean;
}

export type OrderStatus = 'Normal' | 'Voided';

export interface PurchaseOrderItemDto {
  productId: number;
  productName?: string | null;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface PurchaseOrderDto {
  id: number;
  orderNo: string;
  supplierId: number;
  supplierName?: string | null;
  orderDate: string;
  status: OrderStatus;
  note?: string | null;
  totalAmount: number;
  items: PurchaseOrderItemDto[];
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export interface CreatePurchaseOrderItemRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface CreatePurchaseOrderRequest {
  supplierId: number;
  orderDate?: string | null;
  note?: string | null;
  items: CreatePurchaseOrderItemRequest[];
}

export interface SalesOrderItemDto {
  productId: number;
  productName?: string | null;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface SalesOrderDto {
  id: number;
  orderNo: string;
  customerId?: number | null;
  customerName?: string | null;
  orderDate: string;
  status: OrderStatus;
  note?: string | null;
  totalAmount: number;
  items: SalesOrderItemDto[];
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export interface CreateSalesOrderItemRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface CreateSalesOrderRequest {
  customerId?: number | null;
  orderDate?: string | null;
  note?: string | null;
  items: CreateSalesOrderItemRequest[];
}

export interface InventoryItemDto {
  productId: number;
  sku: string;
  name: string;
  categoryName?: string | null;
  unit: string;
  currentStock: number;
  safetyStock: number;
  isLowStock: boolean;
}

export type InventoryChangeType = 'Purchase' | 'Sale' | 'ManualAdjustment' | 'PurchaseVoid' | 'SaleVoid';

export interface InventoryTransactionDto {
  id: number;
  productId: number;
  changeType: InventoryChangeType;
  quantityChange: number;
  stockAfter: number;
  refTable: string;
  refId?: number | null;
  reason?: string | null;
  createdByUserId: number;
  createdByUsername?: string | null;
  createdAt: string;
}

export interface AdjustInventoryRequest {
  productId: number;
  adjustmentQuantity: number;
  reason: string;
}

export interface ActivityLogDto {
  id: number;
  api: string;
  createdAt: string;
  createdBy: string;
}

/** 後端 ExceptionHandlingMiddleware／401 統一回傳格式：{ message: string }。 */
export interface ApiErrorPayload {
  message?: string;
  /** ASP.NET Core [ApiController] 自動 Model Validation 失敗時的 ValidationProblemDetails 格式。 */
  errors?: Record<string, string[]>;
  title?: string;
}
