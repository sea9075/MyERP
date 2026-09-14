import { useMemo, useState } from 'react';
import {
  AppstoreOutlined,
  BarsOutlined,
  DashboardOutlined,
  FileSearchOutlined,
  InboxOutlined,
  LogoutOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  TagsOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { Avatar, Dropdown, Layout, Menu, Space, Tag, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';

const { Header, Sider, Content } = Layout;

type MenuItem = Required<MenuProps>['items'][number];

export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  // 個別呼叫 useAuthStore 取單一欄位，而不是回傳一個新物件：zustand（底層用
  // useSyncExternalStore）預設用 Object.is 比對 selector 結果，每次 render 都回傳新物件
  // 字面量會被判定成「一直有變化」，除了多餘 re-render，嚴重時還可能觸發 React 的
  // getSnapshot 警告或無限更新迴圈。
  const displayName = useAuthStore((state) => state.displayName);
  const role = useAuthStore((state) => state.role);
  const logout = useAuthStore((state) => state.logout);

  const menuItems: MenuItem[] = useMemo(() => {
    const items: MenuItem[] = [
      { key: '/', icon: <DashboardOutlined />, label: <Link to="/">儀表板</Link> },
      { key: '/products', icon: <ShopOutlined />, label: <Link to="/products">商品管理</Link> },
      { key: '/categories', icon: <TagsOutlined />, label: <Link to="/categories">分類管理</Link> },
      { key: '/suppliers', icon: <AppstoreOutlined />, label: <Link to="/suppliers">供應商管理</Link> },
      { key: '/customers', icon: <TeamOutlined />, label: <Link to="/customers">客戶管理</Link> },
      { key: '/purchase-orders', icon: <InboxOutlined />, label: <Link to="/purchase-orders">進貨單</Link> },
      { key: '/sales-orders', icon: <ShoppingCartOutlined />, label: <Link to="/sales-orders">出貨單</Link> },
      { key: '/inventory', icon: <BarsOutlined />, label: <Link to="/inventory">庫存總覽</Link> },
    ];

    if (role === 'Admin') {
      items.push({
        key: '/activity-logs',
        icon: <FileSearchOutlined />,
        label: <Link to="/activity-logs">操作紀錄</Link>,
      });
    }

    return items;
  }, [role]);

  // 選單選中狀態用「最長前綴相符」判斷，這樣 /purchase-orders/new 這種子頁面也會讓
  // 「進貨單」維持選中，不會整排選單都沒有 highlight。
  const selectedKey = useMemo(() => {
    const matched = menuItems
      .map((item) => item?.key as string)
      .filter((key) => key === '/' ? location.pathname === '/' : location.pathname.startsWith(key))
      .sort((a, b) => b.length - a.length)[0];
    return matched ? [matched] : [];
  }, [location.pathname, menuItems]);

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '登出',
      onClick: () => {
        logout();
        navigate('/login', { replace: true });
      },
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider collapsible collapsed={collapsed} onCollapse={setCollapsed}>
        <div
          style={{
            height: 48,
            margin: 12,
            color: '#fff',
            fontWeight: 600,
            fontSize: collapsed ? 16 : 18,
            textAlign: 'center',
            overflow: 'hidden',
            whiteSpace: 'nowrap',
          }}
        >
          {collapsed ? 'ERP' : 'MyERP 進銷存'}
        </div>
        <Menu theme="dark" mode="inline" selectedKeys={selectedKey} items={menuItems} />
      </Sider>
      <Layout>
        <Header
          style={{
            padding: '0 20px',
            background: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'flex-end',
          }}
        >
          <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
            <Space style={{ cursor: 'pointer' }}>
              <Avatar icon={<UserOutlined />} size="small" />
              <Typography.Text>{displayName}</Typography.Text>
              <Tag color={role === 'Admin' ? 'blue' : 'default'}>{role === 'Admin' ? '管理員' : '店員'}</Tag>
            </Space>
          </Dropdown>
        </Header>
        <Content style={{ background: '#f5f5f5' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
