import { DeleteOutlined, EditOutlined, PlusOutlined, StopOutlined } from '@ant-design/icons';
import { Button } from 'antd';
import type { ButtonProps } from 'antd';

/**
 * 專案硬性規定的三色操作按鈕（見 src/styles/global.css 開頭的說明）：
 * - AddButton   → 綠色系，新增/建立
 * - EditButton  → 橘色系，修改/編輯
 * - DeleteButton/VoidButton → 紅色系，刪除/作廢
 *
 * `text` 用在表格操作欄這種空間較小、想用文字按鈕（antd type="text"）而非實心按鈕的地方，
 * 兩種樣式的顏色規則完全一致，只是視覺份量不同。
 */
type ColoredButtonProps = Omit<ButtonProps, 'type' | 'danger'> & { text?: boolean };

export function AddButton({ children, className, text, icon, ...rest }: ColoredButtonProps) {
  return (
    <Button
      type={text ? 'text' : 'primary'}
      icon={icon ?? <PlusOutlined />}
      className={joinClassNames(text ? 'btn-add-text' : 'btn-add', className)}
      {...rest}
    >
      {children}
    </Button>
  );
}

export function EditButton({ children, className, text, icon, ...rest }: ColoredButtonProps) {
  return (
    <Button
      type={text ? 'text' : 'primary'}
      icon={icon ?? <EditOutlined />}
      className={joinClassNames(text ? 'btn-edit-text' : 'btn-edit', className)}
      {...rest}
    >
      {children}
    </Button>
  );
}

export function DeleteButton({ children, className, text, icon, ...rest }: ColoredButtonProps) {
  return (
    <Button
      type={text ? 'text' : 'primary'}
      icon={icon ?? <DeleteOutlined />}
      className={joinClassNames(text ? 'btn-delete-text' : 'btn-delete', className)}
      {...rest}
    >
      {children}
    </Button>
  );
}

/** 進貨單/出貨單「作廢」：性質介於刪除跟修改狀態之間，這次跟使用者確認過用紅色系。 */
export function VoidButton({ children, className, text, icon, ...rest }: ColoredButtonProps) {
  return (
    <Button
      type={text ? 'text' : 'primary'}
      icon={icon ?? <StopOutlined />}
      className={joinClassNames(text ? 'btn-delete-text' : 'btn-delete', className)}
      {...rest}
    >
      {children}
    </Button>
  );
}

function joinClassNames(...values: Array<string | undefined>): string {
  return values.filter(Boolean).join(' ');
}
