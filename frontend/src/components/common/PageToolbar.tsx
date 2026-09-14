import { Typography } from 'antd';
import type { ReactNode } from 'react';

interface PageToolbarProps {
  title: string;
  filters?: ReactNode;
  actions?: ReactNode;
}

/** 每個列表頁共用的頁首排版：左邊標題+篩選條件，右邊操作按鈕（通常是新增）。 */
export function PageToolbar({ title, filters, actions }: PageToolbarProps) {
  return (
    <div className="page-toolbar">
      <div className="page-toolbar-filters">
        <Typography.Title level={4} style={{ margin: 0, marginRight: 12 }}>
          {title}
        </Typography.Title>
        {filters}
      </div>
      {actions && <div>{actions}</div>}
    </div>
  );
}
