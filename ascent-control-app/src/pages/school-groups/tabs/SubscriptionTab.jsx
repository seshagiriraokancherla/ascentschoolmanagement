import { Form, Input, Select, DatePicker, InputNumber, Button, Spin, Alert, Tag } from 'antd'
import { useState, useEffect } from 'react'
import dayjs from 'dayjs'
import api from '../../../api/axiosInstance'

const PLANS  = ['Basic', 'Standard', 'Premium']
const CYCLES = ['Monthly', 'Yearly']

export default function SubscriptionTab({ groupId }) {
  const [sub,     setSub]     = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving,  setSaving]  = useState(false)
  const [success, setSuccess] = useState(false)
  const [form]                = Form.useForm()

  useEffect(() => { fetchSub() }, [groupId])

  const fetchSub = async () => {
    setLoading(true)
    try {
      const res = await api.get(`/control/school-groups/${groupId}/subscription`)
      const data = res.data.data
      setSub(data)
      if (data) {
        form.setFieldsValue({
          ...data,
          startDate:  data.startDate  ? dayjs(data.startDate)  : null,
          expiryDate: data.expiryDate ? dayjs(data.expiryDate) : null,
        })
      }
    } finally { setLoading(false) }
  }

  const handleSave = async (values) => {
    setSaving(true)
    setSuccess(false)
    try {
      await api.put(`/control/school-groups/${groupId}/subscription`, {
        ...values,
        startDate:  values.startDate?.format('YYYY-MM-DD'),
        expiryDate: values.expiryDate?.format('YYYY-MM-DD'),
      })
      setSuccess(true)
      fetchSub()
    } catch (err) {
      alert(err.response?.data?.message || 'Save failed.')
    } finally { setSaving(false) }
  }

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}><Spin /></div>

  return (
    <div style={{ maxWidth: 560, padding: '24px 0' }}>
      {success && <Alert type="success" message="Subscription saved." showIcon style={{ marginBottom: 16 }} />}

      {sub && (
        <div style={{ marginBottom: 16 }}>
          <Tag color={sub.status === 'Active' ? 'green' : 'red'}>{sub.status}</Tag>
          <Tag color="blue">{sub.planName}</Tag>
        </div>
      )}

      <Form form={form} layout="vertical" onFinish={handleSave}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <Form.Item name="planName" label="Plan" rules={[{ required: true }]}>
            <Select options={PLANS.map(p => ({ value: p, label: p }))} />
          </Form.Item>
          <Form.Item name="billingCycle" label="Billing Cycle">
            <Select options={CYCLES.map(c => ({ value: c, label: c }))} />
          </Form.Item>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <Form.Item name="startDate" label="Start Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="expiryDate" label="Expiry Date" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16 }}>
          <Form.Item name="amount" label="Amount (₹)">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
          <Form.Item name="maxSchools" label="Max Branches">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
          <Form.Item name="maxStudents" label="Max Students">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
        </div>
        <Form.Item name="status" label="Status" rules={[{ required: true }]}>
          <Select options={['Active','Expired','Suspended'].map(s => ({ value: s, label: s }))} />
        </Form.Item>
        <Button type="primary" htmlType="submit" loading={saving}>Save Subscription</Button>
      </Form>
    </div>
  )
}
