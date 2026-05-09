import { Table, Switch, Tag, Spin, Typography } from 'antd'
import { useState, useEffect } from 'react'
import api from '../../../api/axiosInstance'
import useAuthStore from '../../../store/authStore'

const TIER_COLOR = { Basic: 'blue', Standard: 'purple', Premium: 'gold' }

export default function ModulesTab({ groupId }) {
  const [modules, setModules] = useState([])
  const [loading, setLoading] = useState(true)
  const role                  = useAuthStore(s => s.user?.role)

  useEffect(() => { fetchModules() }, [groupId])

  const fetchModules = async () => {
    setLoading(true)
    try {
      const res = await api.get(`/control/school-groups/${groupId}/modules`)
      setModules(res.data.data || [])
    } finally { setLoading(false) }
  }

  const handleToggle = async (moduleId, checked) => {
    setModules(prev => prev.map(m => m.moduleId === moduleId ? { ...m, isEnabled: checked } : m))
    try {
      await api.put(`/control/school-groups/${groupId}/modules/${moduleId}`, { isEnabled: checked })
    } catch {
      // revert on error
      setModules(prev => prev.map(m => m.moduleId === moduleId ? { ...m, isEnabled: !checked } : m))
    }
  }

  const columns = [
    { title: 'Module',      dataIndex: 'moduleName', render: (v, r) => <><strong>{v}</strong><br /><Typography.Text type="secondary" style={{ fontSize: 12 }}>{r.moduleCode}</Typography.Text></> },
    { title: 'Plan Tier',   dataIndex: 'planTier',   width: 110, render: v => <Tag color={TIER_COLOR[v]}>{v}</Tag> },
    {
      title: 'Enabled',
      dataIndex: 'isEnabled',
      width: 90,
      render: (v, r) => (
        <Switch
          checked={v}
          onChange={checked => handleToggle(r.moduleId, checked)}
          disabled={role !== 'super_admin'}
        />
      ),
    },
  ]

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}><Spin /></div>

  return (
    <>
      {role !== 'super_admin' && (
        <Typography.Text type="secondary" style={{ display: 'block', margin: '16px 0 8px' }}>
          Only super_admin can toggle modules.
        </Typography.Text>
      )}
      <Table
        rowKey="moduleId"
        dataSource={modules}
        columns={columns}
        pagination={false}
        style={{ marginTop: 16 }}
      />
    </>
  )
}
