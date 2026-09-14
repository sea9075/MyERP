import { useState } from 'react';
import { Form, Input, Modal, Table, Tag, Tooltip } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { extractErrorMessage } from '@/api/client';
import { useCreateCustomer, useCustomers, useDeleteCustomer, useUpdateCustomer } from '@/api/customers';
import type { CustomerDto } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, notifyError, notifySuccess } from '@/utils/alerts';
import { formatDateTime } from '@/utils/format';

interface CustomerFormValues {
  name: string;
  phone?: string;
  note?: string;
}

export function CustomersPage() {
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerDto | null>(null);
  const [form] = Form.useForm<CustomerFormValues>();

  const { data, isLoading } = useCustomers(includeDeleted);
  const createMutation = useCreateCustomer();
  const updateMutation = useUpdateCustomer();
  const deleteMutation = useDeleteCustomer();

  const openCreateModal = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEditModal = (record: CustomerDto) => {
    setEditing(record);
    form.setFieldsValue({ name: record.name, phone: record.phone ?? undefined, note: record.note ?? undefined });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, request: values });
        notifySuccess('客戶已更新');
      } else {
        await createMutation.mutateAsync(values);
        notifySuccess('客戶已新增');
      }
      setModalOpen(false);
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleDelete = async (record: CustomerDto) => {
    const confirmed = await confirmDelete('客戶', record.name);
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('客戶已刪除');
    } catch (error) {
      // 客戶還有出貨單引用時，後端會擋下來並回傳說明訊息（BusinessRuleException → 400）。
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<CustomerDto> = [
    { title: '客戶名稱', dataIndex: 'name' },
    { title: '電話', dataIndex: 'phone', render: (v?: string | null) => v ?? '-' },
    { title: '備註', dataIndex: 'note', render: (v?: string | null) => v ?? '-', ellipsis: true },
    {
      title: '狀態',
      dataIndex: 'isDeleted',
      width: 100,
      render: (isDeleted: boolean) => (isDeleted ? <Tag color="red">已刪除</Tag> : <Tag color="green">正常</Tag>),
    },
    { title: '最後更新', dataIndex: 'updatedAt', render: (v: string) => formatDateTime(v) },
    { title: '更新者', dataIndex: 'updatedBy' },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_, record) =>
        record.isDeleted ? (
          <Tooltip title="客戶刪除後無法從畫面復原">
            <span style={{ color: 'rgba(0,0,0,0.25)' }}>已刪除</span>
          </Tooltip>
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
        title="客戶管理"
        filters={<SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />}
        actions={<AddButton onClick={openCreateModal}>新增客戶</AddButton>}
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />

      <Modal
        title={editing ? '編輯客戶' : '新增客戶'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        okText={editing ? '儲存' : '新增'}
        cancelText="取消"
        okButtonProps={{ className: editing ? 'btn-edit' : 'btn-add' }}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="客戶名稱"
            rules={[{ required: true, message: '請輸入客戶名稱' }, { max: 100, message: '最多 100 個字' }]}
          >
            <Input placeholder="例如：王小明" />
          </Form.Item>
          <Form.Item name="phone" label="電話" rules={[{ max: 30 }]}>
            <Input placeholder="選填" />
          </Form.Item>
          <Form.Item name="note" label="備註" rules={[{ max: 200 }]}>
            <Input.TextArea placeholder="選填" rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
