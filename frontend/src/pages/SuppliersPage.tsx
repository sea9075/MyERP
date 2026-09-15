import { useState } from 'react';
import { UndoOutlined } from '@ant-design/icons';
import { Form, Input, Modal, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { extractErrorMessage } from '@/api/client';
import { useCreateSupplier, useDeleteSupplier, useSuppliers, useUpdateSupplier } from '@/api/suppliers';
import type { SupplierDto } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, extractFormErrorMessages, notifyError, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import { formatDateTime } from '@/utils/format';

interface SupplierFormValues {
  name: string;
  contactPerson?: string;
  phone?: string;
  address?: string;
  note?: string;
}

export function SuppliersPage() {
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<SupplierDto | null>(null);
  const [form] = Form.useForm<SupplierFormValues>();

  const { data, isLoading } = useSuppliers(includeDeleted);
  const createMutation = useCreateSupplier();
  const updateMutation = useUpdateSupplier();
  const deleteMutation = useDeleteSupplier();

  const openCreateModal = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEditModal = (record: SupplierDto) => {
    setEditing(record);
    form.setFieldsValue({
      name: record.name,
      contactPerson: record.contactPerson ?? undefined,
      phone: record.phone ?? undefined,
      address: record.address ?? undefined,
      note: record.note ?? undefined,
    });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    let values: SupplierFormValues;
    try {
      values = await form.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }

    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, request: { ...values, isDeleted: editing.isDeleted } });
        notifySuccess('供應商已更新');
      } else {
        await createMutation.mutateAsync(values);
        notifySuccess('供應商已新增');
      }
      setModalOpen(false);
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const handleDelete = async (record: SupplierDto) => {
    const confirmed = await confirmDelete('供應商', record.name);
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('供應商已刪除');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  /** 恢復已刪除的供應商：走 PUT（Update）把 isDeleted 設回 false，屬於「修改」，用橘色按鈕。 */
  const handleRestore = async (record: SupplierDto) => {
    try {
      await updateMutation.mutateAsync({
        id: record.id,
        request: {
          name: record.name,
          contactPerson: record.contactPerson,
          phone: record.phone,
          address: record.address,
          note: record.note,
          isDeleted: false,
        },
      });
      notifySuccess('供應商已恢復');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<SupplierDto> = [
    { title: '供應商名稱', dataIndex: 'name' },
    { title: '聯絡人', dataIndex: 'contactPerson', render: (v?: string | null) => v ?? '-' },
    { title: '電話', dataIndex: 'phone', render: (v?: string | null) => v ?? '-' },
    { title: '地址', dataIndex: 'address', render: (v?: string | null) => v ?? '-', ellipsis: true },
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
      width: 200,
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
        title="供應商管理"
        filters={<SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />}
        actions={<AddButton onClick={openCreateModal}>新增供應商</AddButton>}
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />

      <Modal
        title={editing ? '編輯供應商' : '新增供應商'}
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
            label="供應商名稱"
            rules={[{ required: true, message: '請輸入供應商名稱' }, { max: 100, message: '最多 100 個字' }]}
          >
            <Input placeholder="例如：大豐食品行" />
          </Form.Item>
          <Form.Item name="contactPerson" label="聯絡人" rules={[{ max: 50 }]}>
            <Input placeholder="選填" />
          </Form.Item>
          <Form.Item name="phone" label="電話" rules={[{ max: 30 }]}>
            <Input placeholder="選填" />
          </Form.Item>
          <Form.Item name="address" label="地址" rules={[{ max: 200 }]}>
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
