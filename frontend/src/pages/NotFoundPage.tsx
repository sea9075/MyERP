import { Button, Result } from 'antd';
import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <Result
      status="404"
      title="404"
      subTitle="找不到這個頁面。"
      extra={
        <Link to="/">
          <Button type="primary">回首頁</Button>
        </Link>
      }
    />
  );
}
