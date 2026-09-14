import { useState } from 'react';
import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { Button, Card, Form, Input, Typography } from 'antd';
import { useMutation } from '@tanstack/react-query';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import type { Location } from 'react-router-dom';
import { login } from '@/api/auth';
import { extractErrorMessage } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { notifySuccess } from '@/utils/alerts';
import type { LoginRequest } from '@/api/types';

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
      <Card style={{ width: 380 }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Typography.Title level={3} style={{ marginBottom: 4 }}>
            MyERP 進銷存管理系統
          </Typography.Title>
          <Typography.Text type="secondary">請登入以繼續</Typography.Text>
        </div>

        <Form layout="vertical" onFinish={handleFinish} autoComplete="off" disabled={mutation.isPending}>
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
