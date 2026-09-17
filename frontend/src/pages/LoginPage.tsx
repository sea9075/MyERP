import { useState } from 'react';
import { InfoCircleOutlined, LockOutlined, UserOutlined } from '@ant-design/icons';
import { Alert, Button, Card, Form, Input, Tooltip, Typography } from 'antd';
import { useMutation } from '@tanstack/react-query';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import type { Location } from 'react-router-dom';
import { login } from '@/api/auth';
import { extractErrorMessage } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { extractFormErrorMessages, notifySuccess, notifyValidationErrors } from '@/utils/alerts';
import type { LoginRequest } from '@/api/types';

// 2026-09-16：測試用帳號密碼，方便展示/測試時直接查看，不用另外去問。
// 注意：這是展示用的 side project，才會把測試帳密直接放在登入頁上；
// 真正上線給客戶用的系統不應該這樣做，之後如果要正式對外營運，記得把這個提示拿掉。
const TEST_ACCOUNTS = [
  { username: 'admin', password: 'Admin@123456' },
  { username: 'hr01', password: 'Hr01@123456' },
  { username: 'manager01', password: 'Manager01@123456' },
  { username: 'support01', password: 'Support01@123456' },
  { username: 'product01', password: 'Product01@123456' },
];

function TestAccountsTooltipContent() {
  return (
    <div style={{ fontFamily: 'monospace', fontSize: 12, lineHeight: 1.8 }}>
      <div style={{ fontFamily: 'inherit', fontWeight: 'bold', marginBottom: 4 }}>測試帳號密碼</div>
      {TEST_ACCOUNTS.map((account) => (
        <div key={account.username}>
          {account.username} / {account.password}
        </div>
      ))}
    </div>
  );
}

export function LoginPage() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const storeLogin = useAuthStore((state) => state.login);
  const navigate = useNavigate();
  const location = useLocation();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (response) => {
      storeLogin(response);
      notifySuccess(`歡迎回來，${response.displayName}`);
      const redirectTo = (location.state as { from?: Location })?.from?.pathname ?? '/';
      navigate(redirectTo, { replace: true });
    },
    onError: (error) => {
      setErrorMessage(extractErrorMessage(error, '登入失敗，請稍後再試。'));
    },
  });

  // 已經登入的話不用再看到登入頁，直接導回去。
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleFinish = (values: LoginRequest) => {
    setErrorMessage(null);
    mutation.mutate(values);
  };

  const handleFinishFailed = (info: { errorFields: { errors: string[] }[] }) => {
    notifyValidationErrors(extractFormErrorMessages(info));
  };

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #1677ff 0%, #003a8c 100%)',
      }}
    >
      <Tooltip title={<TestAccountsTooltipContent />} placement="bottomRight">
        <InfoCircleOutlined
          style={{
            position: 'fixed',
            top: 20,
            right: 20,
            fontSize: 22,
            color: '#fff',
            cursor: 'pointer',
          }}
        />
      </Tooltip>

      <Card style={{ width: 380 }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Typography.Title level={3} style={{ marginBottom: 4 }}>
            MyERP 進銷存管理系統
          </Typography.Title>
          <Typography.Text type="secondary">請登入以繼續</Typography.Text>
        </div>

        {/* 2026-09-17：資料庫用的是 Azure SQL Database Serverless 方案，長時間沒人用會自動
            暫停、下一次連線要花幾秒到十幾秒喚醒。這段時間第一次登入很可能會失敗（或卡著等
            很久），不是系統壞了，先讓使用者知道這是預期行為，请他稍等一下再試一次即可，
            不用懷疑帳密打錯或系統故障。*/}
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="若剛才長時間沒有人使用系統，資料庫可能正在喚醒中"
          description="第一次登入偶爾會失敗或需要等待較久，屬正常現象，請稍待幾秒後再嘗試登入一次即可。"
        />

        <Form
          layout="vertical"
          onFinish={handleFinish}
          onFinishFailed={handleFinishFailed}
          autoComplete="off"
          disabled={mutation.isPending}
        >
          <Form.Item name="username" label="帳號" rules={[{ required: true, message: '請輸入帳號' }]}>
            <Input prefix={<UserOutlined />} placeholder="請輸入帳號" autoFocus />
          </Form.Item>

          <Form.Item name="password" label="密碼" rules={[{ required: true, message: '請輸入密碼' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="請輸入密碼" />
          </Form.Item>

          {errorMessage && (
            <Typography.Paragraph type="danger" style={{ marginBottom: 16 }}>
              {errorMessage}
            </Typography.Paragraph>
          )}

          <Form.Item style={{ marginBottom: 0 }}>
            <Button type="primary" htmlType="submit" block loading={mutation.isPending}>
              登入
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
