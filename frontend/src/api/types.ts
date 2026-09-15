// 這份檔案的型別是照後端 MyErp.Application/DTOs 底下的實際 C# DTO 逐一對應過來的，
// 欄位名稱、選填/必填都跟後端一致，避免前端自己猜欄位長怎樣。

/**
 * 部門，同時也是系統的權限角色（人資/薪資/權限系統）。取代舊的 UserRole('Admin'|'Staff')。
 * Product＝一般 ERP 操作人員，HR＝人資與薪資，Manager／Admin＝管理層（兩者權限目前完全相同），
 * Support＝客服部門（新增），只能用客戶管理跟出貨單，商品只有唯讀權限（挑商品用），其餘 ERP
 * 模組、人資/薪資、操作紀錄都看不到。對應後端 Domain.Enums.Department，LoginResponse 仍沿用
 * 舊欄位名稱 "role" 承載這個字串值。
 */
export type Department = 'Product' | 'HR' | 'Manager' | 'Admin' | 'Support';

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
  /** 欄位名稱沿用後端舊命名 "role"，但內容是 Department 字串（見後端 AuthDtos.cs 的說明）。 */
  role: Department;
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
  /** 使用者自訂的分類編號，最長 5 碼、只允許英文大寫與數字，用來當商品標號的前綴（見 ProductDto.sku）。 */
  code: string;
}

export interface CreateCategoryRequest {
  name: string;
  code: string;
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

/** 新增商品：sku／barcode 不開放輸入，由後端依分類編號自動產生（見 ProductDto 的說明）。 */
export interface CreateProductRequest {
  name: string;
  categoryId: number;
  unit: string;
  costPrice: number;
  salePrice: number;
  safetyStock: number;
  supplierId?: number | null;
}

/** 修改商品：不含 categoryId／sku／barcode——分類只能在新增時選擇，建立後不能更換分類。 */
export interface UpdateProductRequest {
  name: string;
  unit: string;
  costPrice: number;
  salePrice: number;
  safetyStock: number;
  supplierId?: number | null;
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

// ---------------------------------------------------------------------------
// 人資（Employee）—— 對應後端 MyErp.Application/DTOs/EmployeeDtos.cs
// ---------------------------------------------------------------------------

export interface EmployeeDto extends AuditableDto {
  id: number;
  userId: number;
  /** 屬於帳號（User），建立後不能修改。 */
  username: string;
  displayName: string;
  department: Department;
  monthlySalary: number;
  hireDate: string;
  jobTitle?: string | null;
  phone?: string | null;
  address?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
}

/** 新增員工＝同時開一組新帳號＋一筆人資資料，所以帶 Username/Password。 */
export interface CreateEmployeeRequest {
  username: string;
  /** 初始密碼，至少 8 個字元；由 HR 手動輸入，沒有自動產生/一次性顯示流程。 */
  password: string;
  displayName: string;
  department: Department;
  monthlySalary: number;
  hireDate: string;
  jobTitle?: string | null;
  phone?: string | null;
  address?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
}

/** 修改員工：不含 Username/Password（帳號/密碼不透過這支 API 改）。 */
export interface UpdateEmployeeRequest {
  displayName: string;
  department: Department;
  monthlySalary: number;
  hireDate: string;
  jobTitle?: string | null;
  phone?: string | null;
  address?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  isDeleted: boolean;
}

// ---------------------------------------------------------------------------
// 出勤（Attendance）—— 對應後端 MyErp.Application/DTOs/AttendanceDtos.cs
// ---------------------------------------------------------------------------

export type AttendanceSource = 'SelfService' | 'ManualEntry';

export interface AttendanceRecordDto extends AuditableDto {
  id: number;
  employeeId: number;
  employeeName?: string | null;
  workDate: string;
  clockInAt?: string | null;
  clockOutAt?: string | null;
  source: AttendanceSource;
  /** ClockInAt/ClockOutAt 都有值時才會算，否則是 null。 */
  workedHours?: number | null;
  note?: string | null;
}

export interface ClockInRequest {
  note?: string | null;
}

export interface ClockOutRequest {
  note?: string | null;
}

/** HR/Manager/Admin 幫員工手動建立/補登一筆出勤紀錄。 */
export interface CreateManualAttendanceRequest {
  employeeId: number;
  workDate: string;
  clockInAt?: string | null;
  clockOutAt?: string | null;
  note?: string | null;
}

/** HR/Manager/Admin 修改一筆出勤紀錄。 */
export interface UpdateManualAttendanceRequest {
  workDate: string;
  clockInAt?: string | null;
  clockOutAt?: string | null;
  note?: string | null;
  isDeleted: boolean;
}

// ---------------------------------------------------------------------------
// 薪資（Payroll）—— 對應後端 MyErp.Application/DTOs/PayrollDtos.cs
// ---------------------------------------------------------------------------

export interface PayrollRecordDto extends AuditableDto {
  id: number;
  employeeId: number;
  employeeName?: string | null;
  /** 薪資月份，例如 2026-09-01 代表 2026 年 9 月。 */
  periodMonth: string;
  baseSalary: number;
  regularHours: number;
  overtimeHoursTier1: number;
  overtimeHoursTier2: number;
  overtimePay: number;
  bonusAmount: number;
  totalPay: number;
  note?: string | null;
}

export interface CalculatePayrollRequest {
  employeeId: number;
  /** 只會看年/月，日期部分會自動忽略。 */
  periodMonth: string;
}

export interface UpdatePayrollBonusRequest {
  bonusAmount: number;
  note?: string | null;
}

/** 後端 ExceptionHandlingMiddleware／401 統一回傳格式：{ message: string }。 */
export interface ApiErrorPayload {
  message?: string;
  /** ASP.NET Core [ApiController] 自動 Model Validation 失敗時的 ValidationProblemDetails 格式。 */
  errors?: Record<string, string[]>;
  title?: string;
}
