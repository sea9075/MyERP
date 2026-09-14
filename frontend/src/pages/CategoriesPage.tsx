import { useState } from 'react';
import { Form, Input, Modal, Table, Tag, Tooltip } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useCategories, useCreateCategory, useDeleteCategory, useUpdateCategory } from '@/api/categories';
import { extractErrorMessage } from '@/api/client';
import type { CategoryDto } from '@/api/types';
import { AddButton, DeleteButton, EditButton } from '@/components/common/ActionButtons';
import { PageToolbar } from '@/components/common/PageToolbar';
import { SoftDeleteFilter } from '@/components/common/SoftDeleteFilter';
import { confirmDelete, notifyError, notifySuccess } from '@/utils/alerts';
import { formatDateTime } from '@/utils/format';

interface CategoryFormValues {
  name: string;
}

export function CategoriesPage() {
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<CategoryDto | null>(null);
  const [form] = Form.useForm<CategoryFormValues>();

  const { data, isLoading } = useCategories(includeDeleted);
  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();
  const deleteMutation = useDeleteCategory();

  const openCreateModal = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEditModal = (record: CategoryDto) => {
    setEditing(record);
    form.setFieldsValue({ name: record.name });
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, request: values });
        notifySuccess('分類已更新');
      } else {
        await createMutation.mutateAsync(values);
        notifySuccess('分類已新增');
      }
      setModalOpen(false);
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const handleDelete = async (record: CategoryDto) => {
    const confirmed = await confirmDelete('分類', record.name);
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(record.id);
      notifySuccess('分類已刪除');
    } catch (error) {
      notifyError(extractErrorMessage(error));
    }
  };

  const columns: ColumnsType<CategoryDto> = [
    { title: '分類名稱', dataIndex: 'name' },
    {
      title: '狀態',
      dataIndex: 'isDeleted',
      width: 100,
      render: (isDeleted: boolean) => (isDeleted ? <Tag color="red">已刪除</Tag> : <Tag color="green">正常</Tag>),
    },
    { title: '建立時間', dataIndex: 'createdAt', render: (v: string) => formatDateTime(v) },
    { title: '建立者', dataIndex: 'createdBy' },
    { title: '最後更新', dataIndex: 'updatedAt', render: (v: string) => formatDateTime(v) },
    { title: '更新者', dataIndex: 'updatedBy' },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_, record) =>
        record.isDeleted ? (
          // 分類的 API 沒有提供「取消刪除」的欄位（UpdateCategoryRequest 只有 Name），
          // 已刪除的分類目前無法從畫面上復原，只能顯示狀態。
          <Tooltip title="分類刪除後無法從畫面復原">
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
        title="分類管理"
        filters={<SoftDeleteFilter includeDeleted={includeDeleted} onChange={setIncludeDeleted} />}
        actions={<AddButton onClick={openCreateModal}>新增分類</AddButton>}
      />

      <Table rowKey="id" loading={isLoading} dataSource={data} columns={columns} />

      <Modal
        title={editing ? '編輯分類' : '新增分類'}
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
            label="分類名稱"
            rules={[{ required: true, message: '請輸入分類名稱' }, { max: 50, message: '最多 50 個字' }]}
          >
            <Input placeholder="例如：飲料" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}
