import { useState } from 'react';
import { UndoOutlined } from '@ant-design/icons';
import { DatePicker, Form, Input, InputNumber, Modal, Select, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { extractErrorMessage } from '@/api/client';
import { useCreateEmployee, useDeleteEmployee, useEmployees, useUpdateEmployee } from '@/api/employees';
import type { Department, EmployeeDto } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, extractFormErrorMessages, notifyError, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import { formatCurrency, formatDate, formatDateTime } from '@/utils/format';

const DEPARTMENT_OPTIONS: { label: string; value: Department }[] = [
  { label: '商品部（Product）', value: 'Product' },
  { label: '人資部（HR）', value: 'HR' },
  { label: '主管（Manager）', value: 'Manager' },
  { label: '管理員（Admin）', value: 'Admin' },
  { label: '客服部（Support）', value: 'Support' },
];

const DEPARTMENT_COLOR: Record<Department, string> = {
  Product: 'green',
  HR: 'purple',
  Manager: 'gold',
  Admin: 'blue',
  Support: 'cyan',
};

interface EmployeeFormValues {
  username: string;
  password?: string;
  displayName: string;
  department: Department;
  monthlySalary: number;
  hireDate: dayjs.Dayjs;
  jobTitle?: string;
  phone?: string;
  address?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

export function EmployeesPage() {
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<EmployeeDto | null>(null);
  const [form] = Form.useForm<EmployeeFormValues>();

  const { data, isLoading } = useEmployees(includeDeleted);
  const createMutation = useCreateEmployee();
  const updateMutation = useUpdateEmployee();
  const deleteMutation = useDeleteEmployee();

  const openCreateModal = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEditModal = (record: EmployeeDto) => {
    setEditing(record);
    form.setFieldsValue({
      displayName: record.displayName,
      department: record.department,
      monthlySalary: record.monthlySalary,
      hireDate: dayjs(record.hireDate),
      jobTitle: record.jobTitle ?? undefined,
      phone: record.phone ?? undefined,
      address: record.address ?? undefined,
      emergencyContactName: record.emergencyContactName ?? undefined,
      emergencyContactPhone: record.emergencyContactPhone ?? undefined,
    });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    let values: EmployeeFormValues;
    try {
      values = await form.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }

    try {
      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          request: {
            displayName: values.displayName,
            department: values.department,
            monthlySalary: values.monthlySalary,
            hireDate: values.hireDate.toISOString(),
            jobTitle: values.jobTitle,
            phone: values.phone,
            address: values.address,
            emergencyContactName: values.emergencyContactName,
            emergencyContactPhone: values.emergencyContactPhone,
            isDeleted: editing.isDeleted,
          },
        });
        notifySuccess('員工資料已更新');
      } else {
        await createMutation.mutateAsync({
          username: values.username,
          password: values.password ?? '',
          displayName: values.displayName,
          department: values.department,
          monthlySalary: values.monthlySalary,
          hireDate: values.hireDate.toISOString(),
          jobTitle: values.jobTitle,
          phone: values.phone,
          address: values.address,
          emergencyContactName: values.emergencyContactName,
          emergencyContactPhone: values.emergencyContactPhone,
        });
        notifySuccess('員工已新增，帳號已同時建立');
      }
      setModalOpen(false);
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const handleDelete = async (record: EmployeeDto) => {
    const confirmed = await confirmDelete('員工', record.displayName);
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('員工已離職（軟刪除）');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  /** 復職：走 PUT（Update）把 isDeleted 設回 false，屬於「修改」，用橘色按鈕。 */
  const handleRestore = async (record: EmployeeDto) => {
    try {
      await updateMutation.mutateAsync({
        id: record.id,
        request: {
          displayName: record.displayName,
          department: record.department,
          monthlySalary: record.monthlySalary,
          hireDate: record.hireDate,
          jobTitle: record.jobTitle,
          phone: record.phone,
          address: record.address,
          emergencyContactName: record.emergencyContactName,
          emergencyContactPhone: record.emergencyContactPhone,
          isDeleted: false,
        },
      });
      notifySuccess('員工已恢復在職');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<EmployeeDto> = [
    { title: '帳號', dataIndex: 'username', width: 120 },
    { title: '姓名', dataIndex: 'displayName' },
    {
      title: '部門',
      dataIndex: 'department',
      width: 120,
      render: (department: Department) => <Tag color={DEPARTMENT_COLOR[department]}>{department}</Tag>,
    },
    { title: '職稱', dataIndex: 'jobTitle', render: (v?: string | null) => v ?? '-' },
    { title: '月薪', dataIndex: 'monthlySalary', render: (v: number) => formatCurrency(v), width: 120 },
    { title: '到職日', dataIndex: 'hireDate', render: (v: string) => formatDate(v), width: 110 },
    { title: '電話', dataIndex: 'phone', render: (v?: string | null) => v ?? '-', width: 120 },
    {
      title: '狀態',
      dataIndex: 'isDeleted',
      width: 100,
      render: (isDeleted: boolean) => (isDeleted ? <Tag color="red">已離職</Tag> : <Tag color="green">在職</Tag>),
    },
    { title: '最後更新', dataIndex: 'updatedAt', render: (v: string) => formatDateTime(v), width: 160 },
    {
      title: '操作',
      key: 'actions',
      width: 200,
      render: (_, record) =>
        record.isDeleted ? (
          <EditButton text icon={<UndoOutlined />} onClick={() => handleRestore(record)}>
            恢復在職
          </EditButton>
        ) : (
          <>
            <EditButton text onClick={() => openEditModal(record)}>
              編輯
            </EditButton>
            <DeleteButton text onClick={() => handleDelete(record)}>
              離職
            </DeleteButton>
          </>
        ),
    },
  ];

  return (
    <div className="page-container">
      <PageToolbar
        title="員工管理"
        filters={<SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />}
        actions={<AddButton onClick={openCreateModal}>新增員工</AddButton>}
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />

      <Modal
        title={editing ? '編輯員工' : '新增員工'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        okText={editing ? '儲存' : '新增'}
        cancelText="取消"
        okButtonProps={{ className: editing ? 'btn-edit' : 'btn-add' }}
        width={640}
      >
        <Form form={form} layout="vertical">
          {!editing && (
            <>
              <Form.Item
                name="username"
                label="帳號"
                rules={[{ required: true, message: '請輸入帳號' }, { max: 50, message: '最多 50 個字' }]}
              >
                <Input placeholder="登入帳號，建立後無法修改" />
              </Form.Item>
              <Form.Item
                name="password"
                label="初始密碼"
                rules={[{ required: true, message: '請輸入初始密碼' }, { min: 8, message: '密碼至少需要 8 個字元' }]}
              >
                <Input.Password placeholder="至少 8 個字元，請自行交付給員工" />
              </Form.Item>
            </>
          )}

          <Form.Item
            name="displayName"
            label="姓名"
            rules={[{ required: true, message: '請輸入姓名' }, { max: 50, message: '最多 50 個字' }]}
          >
            <Input placeholder="員工姓名" />
          </Form.Item>

          <Form.Item name="department" label="部門" rules={[{ required: true, message: '請選擇部門' }]}>
            <Select placeholder="請選擇部門" options={DEPARTMENT_OPTIONS} />
          </Form.Item>

          <Form.Item
            name="monthlySalary"
            label="月薪"
            rules={[{ required: true, message: '請輸入月薪' }]}
          >
            <InputNumber min={0} style={{ width: '100%' }} placeholder="月薪（用於薪資計算的底薪）" />
          </Form.Item>

          <Form.Item name="hireDate" label="到職日" rules={[{ required: true, message: '請選擇到職日' }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="jobTitle" label="職稱" rules={[{ max: 50 }]}>
            <Input placeholder="選填" />
          </Form.Item>

          <Form.Item name="phone" label="電話" rules={[{ max: 30 }]}>
            <Input placeholder="選填" />
          </Form.Item>

          <Form.Item name="address" label="地址" rules={[{ max: 200 }]}>
            <Input placeholder="選填" />
          </Form.Item>

          <Form.Item name="emergencyContactName" label="緊急聯絡人" rules={[{ max: 50 }]}>
            <Input placeholder="選填" />
          </Form.Item>

          <Form.Item name="emergencyContactPhone" label="緊急聯絡人電話" rules={[{ max: 30 }]}>
            <Input placeholder="選填" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
