import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  AppstoreOutlined,
  BarsOutlined,
  BellOutlined,
  ClockCircleOutlined,
  BarChartOutlined,
  DashboardOutlined,
  DollarOutlined,
  FileSearchOutlined,
  IdcardOutlined,
  InboxOutlined,
  KeyOutlined,
  LogoutOutlined,
  ShopOutlined,
  ShoppingCartOutlined,
  ShoppingOutlined,
  SolutionOutlined,
  TagsOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { Avatar, Badge, Dropdown, Form, Input, Layout, Menu, Modal, Space, Tag, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { useMutation } from '@tanstack/react-query';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { changePassword } from '@/api/auth';
import { extractErrorMessage } from '@/api/client';
import { useUnreadNotificationCount } from '@/api/notifications';
import type { Department } from '@/api/types';
import { useAuthStore } from '@/stores/authStore';
import { extractFormErrorMessages, notifySuccess, notifyValidationErrors } from '@/utils/alerts';

const { Header, Sider, Content } = Layout;

type MenuItem = Required<MenuProps>['items'][number];

/** 部門顯示用的中文標籤跟 Tag 顏色，統一放在這裡管理，避免各處各寫一份。 */
const DEPARTMENT_LABEL: Record<Department, { label: string; color: string }> = {
  Product: { label: '商品部', color: 'green' },
  HR: { label: '人資部', color: 'purple' },
  Manager: { label: '主管', color: 'gold' },
  Admin: { label: '管理員', color: 'blue' },
  Support: { label: '客服部', color: 'cyan' },
};

interface ChangePasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

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

  // 通知（worker 低庫存自動通知，2026-09-15 新增，見 Infra-Progress.md §31）：跟商品/庫存
  // 同一群組權限，只有這幾個角色看得到選單項目，也只有這幾個角色需要輪詢未讀數。
  const canSeeNotifications = role === 'Product' || role === 'Manager' || role === 'Admin';
  const { data: unreadNotificationCount } = useUnreadNotificationCount(canSeeNotifications);

  // 右上角選單的「密碼修改」：任何登入使用者都能用（含 HR 自己），需要先輸入目前密碼才能改，
  // 2026-09-15 新增，對應後端 PUT /api/auth/password（見 EmployeesPage 的「重設密碼」— 那支
  // 是 HR/Manager/Admin 幫別人強制重設、不需要舊密碼，是不同的 API，開放對象也不一樣）。
  const [passwordModalOpen, setPasswordModalOpen] = useState(false);
  const [passwordForm] = Form.useForm<ChangePasswordFormValues>();
  const changePasswordMutation = useMutation({ mutationFn: changePassword });

  const openPasswordModal = () => {
    passwordForm.resetFields();
    setPasswordModalOpen(true);
  };

  const handleChangePassword = async () => {
    let values: ChangePasswordFormValues;
    try {
      values = await passwordForm.validateFields();
    } catch (err) {
      notifyValidationErrors(extractFormErrorMessages(err));
      return;
    }

    try {
      await changePasswordMutation.mutateAsync({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      });
      notifySuccess('密碼已修改');
      setPasswordModalOpen(false);
    } catch (error) {
      notifyValidationErrors([extractErrorMessage(error)]);
    }
  };

  const menuItems: MenuItem[] = useMemo(() => {
    const items: MenuItem[] = [{ key: '/', icon: <DashboardOutlined />, label: <Link to="/">儀表板</Link> }];

    // 「商品與庫存」收合群組：分類管理 > 供應商管理 > 商品管理 > 庫存總覽（使用者指定的固定順序），
    // 只有 Product/Manager/Admin 看得到。這是商品部門的工作範圍，跟客戶管理無關（客戶管理是
    // Support 的工作範圍，見下方 topLevelOrderItems 的「客戶管理」項目）。
    const inventoryGroupItems: Array<{ key: string; icon: ReactNode; label: ReactNode }> = [
      { key: '/categories', icon: <TagsOutlined />, label: <Link to="/categories">分類管理</Link> },
      { key: '/suppliers', icon: <AppstoreOutlined />, label: <Link to="/suppliers">供應商管理</Link> },
      { key: '/products', icon: <ShopOutlined />, label: <Link to="/products">商品管理</Link> },
      { key: '/inventory', icon: <BarsOutlined />, label: <Link to="/inventory">庫存總覽</Link> },
      {
        key: '/notifications',
        icon: <BellOutlined />,
        label: (
          <Link to="/notifications">
            <Space>
              通知
              {!!unreadNotificationCount && <Badge count={unreadNotificationCount} size="small" />}
            </Space>
          </Link>
        ),
      },
    ];

    if (role === 'Product' || role === 'Manager' || role === 'Admin') {
      items.push({
        key: 'inventory-group',
        icon: <ShoppingOutlined />,
        label: '商品與庫存',
        children: inventoryGroupItems,
      });
    }

    // 進貨單／出貨單維持在群組外、獨立的頂層項目（使用者指定）。出貨單這次改成全面唯讀
    // （拿掉新增／作廢，見 SalesOrdersPage），Support 部門只開放查詢，所以還是留在選單上。
    // 客戶管理是 Support 的核心工作範圍之一（跟出貨單一起用），Manager/Admin 也能管理，
    // 但商品部（Product）不需要（使用者明確表示：那一點需求是針對商品部門）。
    const topLevelOrderItems: Array<{ key: string; icon: ReactNode; label: ReactNode; allowed: Department[] }> = [
      { key: '/purchase-orders', icon: <InboxOutlined />, label: <Link to="/purchase-orders">進貨單</Link>, allowed: ['Product', 'Manager', 'Admin'] },
      { key: '/sales-orders', icon: <ShoppingCartOutlined />, label: <Link to="/sales-orders">出貨單</Link>, allowed: ['Product', 'Manager', 'Admin', 'Support'] },
      { key: '/customers', icon: <TeamOutlined />, label: <Link to="/customers">客戶管理</Link>, allowed: ['Manager', 'Admin', 'Support'] },
    ];

    if (role) {
      items.push(
        ...topLevelOrderItems
          .filter((item) => item.allowed.includes(role))
          .map(({ key, icon, label }) => ({ key, icon, label })),
      );
    }

    // 打卡（我的出勤）：不分部門，任何登入使用者都要用，所以獨立放在最外層，不放進「人資」群組。
    items.push({
      key: '/my-attendance',
      icon: <ClockCircleOutlined />,
      label: <Link to="/my-attendance">我的出勤</Link>,
    });

    // 「人資」收合群組：員工管理／出勤管理（查全部＋手動補登）／薪資管理，只有 HR/Manager/Admin 看得到。
    if (role === 'HR' || role === 'Manager' || role === 'Admin') {
      items.push({
        key: 'hr-group',
        icon: <SolutionOutlined />,
        label: '人資',
        children: [
          { key: '/employees', icon: <IdcardOutlined />, label: <Link to="/employees">員工管理</Link> },
          {
            key: '/attendance/manage',
            icon: <ClockCircleOutlined />,
            label: <Link to="/attendance/manage">出勤管理</Link>,
          },
          { key: '/payroll', icon: <DollarOutlined />, label: <Link to="/payroll">薪資管理</Link> },
        ],
      });
    }

    if (role === 'Manager' || role === 'Admin') {
      items.push({
        key: '/activity-logs',
        icon: <FileSearchOutlined />,
        label: <Link to="/activity-logs">操作紀錄</Link>,
      });

      // 報表模組（ERP.md §4.6 Phase 3，Infra-Progress.md §27，2026-09-15 新增），只有 Manager/Admin
      // 看得到（使用者決定，跟操作紀錄同樣的權限收斂）。
      items.push({
        key: '/reports',
        icon: <BarChartOutlined />,
        label: <Link to="/reports">報表</Link>,
      });
    }

    return items;
  }, [role, unreadNotificationCount]);

  // 選單選中狀態用「最長前綴相符」判斷，這樣 /purchase-orders/new 這種子頁面也會讓
  // 「進貨單」維持選中，不會整排選單都沒有 highlight。子選單（人資群組）的 key 不是路徑，
  // 要排除掉，不然會被 location.pathname.startsWith('hr-group') 誤判。
  const selectedKey = useMemo(() => {
    const flatKeys: string[] = [];
    for (const item of menuItems) {
      const key = item?.key as string;
      if (key?.startsWith('/')) flatKeys.push(key);
      const children = (item as { children?: MenuItem[] })?.children;
      if (children) {
        for (const child of children) {
          const childKey = child?.key as string;
          if (childKey?.startsWith('/')) flatKeys.push(childKey);
        }
      }
    }
    const matched = flatKeys
      .filter((key) => (key === '/' ? location.pathname === '/' : location.pathname.startsWith(key)))
      .sort((a, b) => b.length - a.length)[0];
    return matched ? [matched] : [];
  }, [location.pathname, menuItems]);

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'change-password',
      icon: <KeyOutlined />,
      label: '密碼修改',
      onClick: openPasswordModal,
    },
    { type: 'divider' },
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

  const departmentMeta = role ? DEPARTMENT_LABEL[role] : undefined;

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
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={selectedKey}
          defaultOpenKeys={['hr-group']}
          items={menuItems}
        />
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
              {departmentMeta && <Tag color={departmentMeta.color}>{departmentMeta.label}</Tag>}
            </Space>
          </Dropdown>
        </Header>
        <Content style={{ background: '#f5f5f5' }}>
          <Outlet />
        </Content>
      </Layout>

      <Modal
        title="密碼修改"
        open={passwordModalOpen}
        onOk={handleChangePassword}
        onCancel={() => setPasswordModalOpen(false)}
        confirmLoading={changePasswordMutation.isPending}
        okText="修改密碼"
        cancelText="取消"
      >
        <Form form={passwordForm} layout="vertical">
          <Form.Item
            name="currentPassword"
            label="目前密碼"
            rules={[{ required: true, message: '請輸入目前密碼' }]}
          >
            <Input.Password placeholder="請輸入目前密碼" autoFocus />
          </Form.Item>

          <Form.Item
            name="newPassword"
            label="新密碼"
            rules={[{ required: true, message: '請輸入新密碼' }, { min: 8, message: '密碼至少需要 8 個字元' }]}
          >
            <Input.Password placeholder="至少 8 個字元" />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label="確認新密碼"
            dependencies={['newPassword']}
            rules={[
              { required: true, message: '請再輸入一次新密碼' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || value === getFieldValue('newPassword')) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('兩次輸入的新密碼不一致'));
                },
              }),
            ]}
          >
            <Input.Password placeholder="請再輸入一次新密碼" />
          </Form.Item>
        </Form>
      </Modal>
    </Layout>
  );
}
