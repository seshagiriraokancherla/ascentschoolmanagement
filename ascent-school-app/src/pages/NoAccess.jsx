import { Result, Button } from 'antd'
import { useNavigate } from 'react-router-dom'

/**
 * Shown when an authenticated user navigates to a page their role/permissions
 * do not allow. Pure UX guard — the backend remains the real security boundary.
 */
export default function NoAccess() {
  const navigate = useNavigate()
  return (
    <Result
      status="403"
      title="403"
      subTitle="You don't have permission to access this page. Contact your administrator if you believe this is a mistake."
      extra={
        <Button type="primary" onClick={() => navigate('/', { replace: true })}>
          Back to Dashboard
        </Button>
      }
    />
  )
}
