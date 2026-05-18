import { useEffect, useState } from 'react'
import {
  Card, Form, Input, InputNumber, Select, Radio,
  Button, Divider, message, Spin, Typography,
} from 'antd'
import { SaveOutlined } from '@ant-design/icons'
import api from '../../api/axiosInstance'

const { Title } = Typography
const { Option } = Select

const YN_OPTIONS = [
  { label: 'Yes', value: 'Y' },
  { label: 'No',  value: 'N' },
]

const BILL_SERIES_OPTIONS = [
  'New Year', 'Academic Year', 'Financial Year', 'Continues', 'Cash-Bank', 'Category-wise',
]

export default function SchoolSettingsPage() {
  const [form]    = Form.useForm()
  const [loading, setLoading]  = useState(true)
  const [saving,  setSaving]   = useState(false)

  useEffect(() => {
    api.get('/school/settings')
      .then(res => {
        const d = res.data.data || {}
        form.setFieldsValue(d)
      })
      .catch(() => message.error('Failed to load settings.'))
      .finally(() => setLoading(false))
  }, [])

  const onFinish = async (values) => {
    setSaving(true)
    try {
      await api.put('/school/settings', values)
      message.success('Settings saved.')
    } catch (err) {
      message.error(err.message || 'Failed to save settings.')
    } finally { setSaving(false) }
  }

  if (loading) return <Spin style={{ display: 'block', marginTop: 80 }} />

  return (
    <div style={{ maxWidth: 860, margin: '0 auto', padding: '24px 16px' }}>
      <Title level={4} style={{ marginBottom: 24 }}>School Settings</Title>

      <Form form={form} layout="vertical" onFinish={onFinish}>

        {/* ── Admission ──────────────────────────────────────────── */}
        <Card title="Admission" style={{ marginBottom: 20 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="admissionNoType" label="Admission No. Type">
              <Select allowClear placeholder="Select…">
                <Option value="Auto">Auto</Option>
                <Option value="Manual">Manual</Option>
                <Option value="Prefix-Auto">Prefix + Auto</Option>
              </Select>
            </Form.Item>
            <Form.Item name="newStudentEntryMode" label="New Student Entry Mode">
              <Select allowClear placeholder="Select…">
                <Option value="Fast Entry">Fast Entry</Option>
                <Option value="Normal Entry">Normal Entry</Option>
              </Select>
            </Form.Item>
          </div>

          <Form.Item name="categoryWiseAdmissions" label="Category-wise Admissions">
            <Radio.Group options={YN_OPTIONS} optionType="button" />
          </Form.Item>

          <Divider plain style={{ fontSize: 12 }}>Admission Number Prefixes</Divider>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="prePrimaryAdmissionPrefix" label="Pre-Primary Prefix">
              <Input maxLength={15} placeholder="e.g. PP" />
            </Form.Item>
            <Form.Item name="primaryAdmissionPrefix" label="Primary Prefix">
              <Input maxLength={15} placeholder="e.g. PR" />
            </Form.Item>
            <Form.Item name="highSchoolAdmissionPrefix" label="High School Prefix">
              <Input maxLength={15} placeholder="e.g. HS" />
            </Form.Item>
            <Form.Item name="generalAdmissionPrefix" label="General Prefix">
              <Input maxLength={15} placeholder="e.g. GN" />
            </Form.Item>
          </div>
        </Card>

        {/* ── Fee & Billing ──────────────────────────────────────── */}
        <Card title="Fee & Billing" style={{ marginBottom: 20 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="feeReceiptLock" label="Fee Receipt Lock (days)">
              <InputNumber min={0} style={{ width: '100%' }} placeholder="0 = no lock" />
            </Form.Item>
            <Form.Item name="receiptPrintCopies" label="Receipt Print Copies">
              <InputNumber min={1} max={5} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="feeReceiptPrint" label="Fee Receipt Print">
              <Select allowClear placeholder="Select…">
                <Option value="Yes">Yes</Option>
                <Option value="No">No</Option>
              </Select>
            </Form.Item>
            <Form.Item name="billNoSeriesType" label="Bill No. Series Type">
              <Select allowClear placeholder="Select…">
                {BILL_SERIES_OPTIONS.map(o => <Option key={o} value={o}>{o}</Option>)}
              </Select>
            </Form.Item>
            <Form.Item name="receiptFeeTypeSeparator" label="Receipt Fee-Type Separator">
              <Input maxLength={5} placeholder="e.g. /" />
            </Form.Item>
            <Form.Item name="billingStatus" label="Billing Status">
              <Input maxLength={10} placeholder="Active / Inactive" />
            </Form.Item>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16, marginTop: 4 }}>
            <Form.Item name="transportFeeIncluded" label="Transport Fee Included">
              <Radio.Group options={YN_OPTIONS} optionType="button" />
            </Form.Item>
            <Form.Item name="fineEnabled" label="Fine Enabled">
              <Radio.Group options={YN_OPTIONS} optionType="button" />
            </Form.Item>
            <Form.Item name="feeMessageToTeacher" label="Fee Message to Teacher">
              <Radio.Group options={YN_OPTIONS} optionType="button" />
            </Form.Item>
          </div>

          <Form.Item name="studentConcessionEnabled" label="Student Concession">
            <Radio.Group optionType="button">
              <Radio.Button value="Enable">Enable</Radio.Button>
              <Radio.Button value="Disable">Disable</Radio.Button>
            </Radio.Group>
          </Form.Item>
        </Card>

        {/* ── Reports & Institution ──────────────────────────────── */}
        <Card title="Reports & Institution" style={{ marginBottom: 24 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Form.Item name="institutionHeadName" label="Institution Head Name">
              <Input maxLength={20} />
            </Form.Item>
            <Form.Item name="progressReportType" label="Progress Report Type">
              <Input maxLength={25} />
            </Form.Item>
            <Form.Item name="otherSubjectsType" label="Other Subjects Type">
              <Input maxLength={10} />
            </Form.Item>
          </div>
          <Form.Item name="institutionHeadSignature" label="Head Signature (file path or URL)">
            <Input maxLength={255} placeholder="e.g. /signatures/principal.png or https://…" />
          </Form.Item>
        </Card>

        <Button type="primary" htmlType="submit" icon={<SaveOutlined />} loading={saving} size="large">
          Save Settings
        </Button>
      </Form>
    </div>
  )
}
