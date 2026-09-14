import { Switch } from 'antd';

interface SoftDeleteFilterProps {
  includeDeleted: boolean;
  onChange: (includeDeleted: boolean) => void;
}

/** 主檔管理頁共用的「顯示已刪除」開關，對應後端 GetAll(includeDeleted) 查詢參數。 */
export function SoftDeleteFilter({ includeDeleted, onChange }: SoftDeleteFilterProps) {
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 8 }}>
      <Switch checked={includeDeleted} onChange={onChange} size="small" />
      顯示已刪除
    </span>
  );
}
