import { useEffect, useState, useCallback } from 'react'
import {
  Card, Select, DatePicker, Button, Table, Typography, App as AntApp,
  Row, Col, Input, Space, Tag, Progress, Alert, Tabs, Badge,
  Statistic, Divider, Checkbox,
} from 'antd'
import {
  SendOutlined, ReloadOutlined, MessageOutlined, HistoryOutlined,
  CheckCircleOutlined, CloseCircleOutlined, UserOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Text, Title } = Typography
const { TextArea }    = Input

// Default dropdown labels (fallback). The Send tab overrides each label with the
// matching sms_template's Title loaded from the server (see SMS_TYPE_KEY).
const SMS_TYPES = [
  { value: 'Absent',  label: 'Absent Notification' },
  { value: 'FeeDue',  label: 'Fee Due Reminder'    },
  { value: 'Custom',  label: 'Custom Message'       },
]

// SMS Center type -> sms_templates.template_key (so the template Title can drive the label)
const SMS_TYPE_KEY = { Absent: 'ABSENT', FeeDue: 'FEE_DUE', Custom: 'CUSTOM' }

const BATCH_SIZES = [25, 50]

// Split array into chunks of size n
function chunk(arr, n) {
  const result = []
  for (let i = 0; i < arr.length; i += n) result.push(arr.slice(i, i + n))
  return result
}

// Build preview message (mirrors server-side BuildMessage logic)
function buildPreview(smsType, recipient, date, customMessage) {
  if (!recipient) return ''
  switch (smsType) {
    case 'Absent':
      return `Dear Parent, your ward ${recipient.studentName} was absent on ${date || 'today'}. Please ensure regular attendance.`
    case 'FeeDue':
      return `Dear Parent, your ward ${recipient.studentName} has an outstanding fee of Rs.${Number(recipient.outstandingAmount || 0).toFixed(2)}. Please pay at the earliest.`
    case 'Custom':
      return (customMessage || '').replace('{name}', recipient.studentName)
    default:
      return ''
  }
}

// ── Send Tab ──────────────────────────────────────────────────────────────────

function SendTab() {
  const { message } = AntApp.useApp()

  const [years,        setYears]        = useState([])
  const [classes,      setClasses]      = useState([])
  const [sections,     setSections]     = useState([])
  const [smsTypes,     setSmsTypes]     = useState(SMS_TYPES)   // labels overridden from template Titles

  const [smsType,      setSmsType]      = useState('Absent')
  const [date,         setDate]         = useState(dayjs())
  const [yearId,       setYearId]       = useState(null)
  const [classId,      setClassId]      = useState(null)
  const [sectionId,    setSectionId]    = useState(null)
  const [customMsg,    setCustomMsg]    = useState('')

  const [loading,      setLoading]      = useState(false)
  const [recipients,   setRecipients]   = useState([])
  const [selectedKeys, setSelectedKeys] = useState([])
  const [batchSize,    setBatchSize]    = useState(25)

  // sending state
  const [sending,      setSending]      = useState(false)
  const [progress,     setProgress]     = useState({ current: 0, total: 0, sent: 0, failed: 0 })
  const [sendDone,     setSendDone]     = useState(false)
  const [errors,       setErrors]       = useState([])

  // Load lookups on mount
  useEffect(() => {
    api.get('/school/master/academic-years?activeOnly=true').then(r => {
      const list = r.data?.data || []
      setYears(list)
      const current = list.find(y => y.isCurrent)
      if (current) setYearId(current.academicYearId)
    })
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
    // Override dropdown labels with each template's Title (key-matched); falls back
    // to the default label when a template is missing or has no title.
    api.get('/school/sms/config').then(r => {
      const byKey = {}
      ;(r.data?.data?.templates || []).forEach(t => { byKey[t.templateKey] = t })
      setSmsTypes(SMS_TYPES.map(d => {
        const t = byKey[SMS_TYPE_KEY[d.value]]
        return { value: d.value, label: (t?.title?.trim()) ? t.title : d.label }
      }))
    }).catch(() => {})
  }, [])

  const loadSections = useCallback(async (cid) => {
    setSectionId(null)
    setSections([])
    if (!cid) return
    try {
      const r = await api.get(`/school/master/sections?classId=${cid}`)
      setSections(r.data?.data || [])
    } catch { /* ignore */ }
  }, [])

  const handleClassChange = (cid) => {
    setClassId(cid)
    loadSections(cid)
  }

  const handleTypeChange = (t) => {
    setSmsType(t)
    setRecipients([])
    setSelectedKeys([])
    setSendDone(false)
    setErrors([])
    setProgress({ current: 0, total: 0, sent: 0, failed: 0 })
  }

  // Load recipients
  const loadRecipients = async () => {
    if (smsType === 'Absent' && !date) { message.warning('Select a date.'); return }
    if (smsType !== 'Absent' && !yearId) { message.warning('Select an academic year.'); return }

    setLoading(true)
    setRecipients([])
    setSelectedKeys([])
    setSendDone(false)
    setErrors([])
    setProgress({ current: 0, total: 0, sent: 0, failed: 0 })

    try {
      const params = new URLSearchParams({ smsType })
      if (smsType === 'Absent')  params.append('date',           date.format('YYYY-MM-DD'))
      if (smsType !== 'Absent')  params.append('academicYearId', yearId)
      if (classId)               params.append('classId',        classId)
      if (sectionId)             params.append('sectionId',      sectionId)

      const r    = await api.get(`/school/sms/recipients?${params}`)
      const list = r.data?.data || []
      setRecipients(list)
      setSelectedKeys(list.map(s => s.studentId))
      if (list.length === 0) message.info('No recipients found for the selected filters.')
    } catch {
      message.error('Failed to load recipients.')
    } finally {
      setLoading(false)
    }
  }

  // Send SMS in batches
  const sendSms = async () => {
    const selected = recipients.filter(r => selectedKeys.includes(r.studentId))
    if (selected.length === 0) { message.warning('Select at least one recipient.'); return }
    if (smsType === 'Custom' && !customMsg.trim()) {
      message.warning('Enter a custom message.')
      return
    }

    const batches = chunk(selected, batchSize)
    setSending(true)
    setSendDone(false)
    setErrors([])
    setProgress({ current: 0, total: batches.length, sent: 0, failed: 0 })

    let totalSent = 0, totalFailed = 0, allErrors = []

    for (let i = 0; i < batches.length; i++) {
      const batch = batches[i]
      try {
        const r = await api.post('/school/sms/send', {
          smsType,
          date:          smsType === 'Absent' ? date?.format('YYYY-MM-DD') : undefined,
          customMessage: smsType === 'Custom' ? customMsg : undefined,
          recipients:    batch.map(s => ({
            studentId:         s.studentId,
            studentName:       s.studentName,
            admissionNo:       s.admissionNo,
            className:         s.className,
            mobile:            s.fatherMobile,
            outstandingAmount: s.outstandingAmount ?? 0,
          })),
        })
        const res = r.data?.data || {}
        totalSent   += res.sent   || 0
        totalFailed += res.failed || 0
        allErrors    = [...allErrors, ...(res.errors || [])]
      } catch {
        totalFailed += batch.length
        allErrors    = [...allErrors, ...batch.map(s => ({
          studentId: s.studentId, studentName: s.studentName,
          mobile: s.fatherMobile, reason: 'Network error',
        }))]
      }
      setProgress({ current: i + 1, total: batches.length, sent: totalSent, failed: totalFailed })
    }

    setSending(false)
    setSendDone(true)
    setErrors(allErrors)
    if (totalFailed === 0)
      message.success(`${totalSent} SMS sent successfully.`)
    else
      message.warning(`${totalSent} sent, ${totalFailed} failed.`)
  }

  const firstSelected = recipients.find(r => selectedKeys.includes(r.studentId))
  const preview       = buildPreview(smsType, firstSelected, date?.format('DD-MM-YYYY'), customMsg)

  const recipientCols = [
    { title: 'Student Name',  dataIndex: 'studentName',  key: 'name',  ellipsis: true },
    { title: 'Class',         dataIndex: 'className',    key: 'class', width: 90 },
    { title: 'Section',       dataIndex: 'sectionName',  key: 'sec',   width: 80 },
    { title: 'Father Mobile', dataIndex: 'fatherMobile', key: 'mob',   width: 130 },
    smsType === 'FeeDue' && {
      title:     'Outstanding (₹)',
      dataIndex: 'outstandingAmount',
      key:       'due',
      width:     140,
      align:     'right',
      render:    v => <Text type="danger">₹{Number(v || 0).toFixed(2)}</Text>,
    },
    smsType === 'Absent' && {
      title:     'Date',
      dataIndex: 'attendanceDate',
      key:       'adate',
      width:     110,
    },
  ].filter(Boolean)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* ── Filter Panel ── */}
      <Card size="small" title={<><MessageOutlined style={{ marginRight: 6 }} />Configure SMS</>}>
        <Row gutter={[16, 12]} align="bottom">
          <Col xs={24} sm={8} md={6}>
            <div style={{ marginBottom: 4 }}><Text strong>SMS Type</Text></div>
            <Select
              style={{ width: '100%' }}
              options={smsTypes}
              value={smsType}
              onChange={handleTypeChange}
            />
          </Col>

          {smsType === 'Absent' && (
            <Col xs={24} sm={8} md={5}>
              <div style={{ marginBottom: 4 }}><Text strong>Date <span style={{ color: 'red' }}>*</span></Text></div>
              <DatePicker
                style={{ width: '100%' }}
                value={date}
                onChange={setDate}
                disabledDate={d => d && d.isAfter(dayjs(), 'day')}
              />
            </Col>
          )}

          {smsType !== 'Absent' && (
            <Col xs={24} sm={8} md={5}>
              <div style={{ marginBottom: 4 }}><Text strong>Academic Year <span style={{ color: 'red' }}>*</span></Text></div>
              <Select
                style={{ width: '100%' }}
                placeholder="Select year"
                value={yearId}
                onChange={setYearId}
                options={(years || []).map(y => ({ value: y.academicYearId, label: y.academicYear }))}
              />
            </Col>
          )}

          <Col xs={24} sm={8} md={4}>
            <div style={{ marginBottom: 4 }}><Text strong>Class</Text></div>
            <Select
              style={{ width: '100%' }}
              placeholder="All classes"
              allowClear
              value={classId}
              onChange={handleClassChange}
              options={(classes || []).map(c => ({ value: c.classId, label: c.className }))}
            />
          </Col>

          <Col xs={24} sm={8} md={4}>
            <div style={{ marginBottom: 4 }}><Text strong>Section</Text></div>
            <Select
              style={{ width: '100%' }}
              placeholder="All sections"
              allowClear
              disabled={!classId}
              value={sectionId}
              onChange={setSectionId}
              options={(sections || []).map(s => ({ value: s.sectionId, label: s.sectionName }))}
            />
          </Col>

          <Col xs={24} sm={8} md={5}>
            <Button
              type="primary"
              icon={<ReloadOutlined />}
              loading={loading}
              onClick={loadRecipients}
              style={{ width: '100%' }}
            >
              Load Recipients
            </Button>
          </Col>
        </Row>

        {smsType === 'Custom' && (
          <Row style={{ marginTop: 12 }}>
            <Col span={24}>
              <div style={{ marginBottom: 4 }}>
                <Text strong>Message </Text>
                <Text type="secondary" style={{ fontSize: 12 }}>Use <code>{'{name}'}</code> for student name</Text>
              </div>
              <TextArea
                rows={3}
                maxLength={160}
                showCount
                placeholder="e.g. Dear Parent, {name}'s school is closed tomorrow for a public holiday."
                value={customMsg}
                onChange={e => setCustomMsg(e.target.value)}
              />
            </Col>
          </Row>
        )}
      </Card>

      {/* ── Recipients Table ── */}
      {recipients.length > 0 && (
        <Card
          size="small"
          title={
            <Space>
              <UserOutlined />
              <span>Recipients</span>
              <Tag color="blue">{selectedKeys.length} of {recipients.length} selected</Tag>
            </Space>
          }
          extra={
            <Space>
              <Text style={{ fontSize: 12 }}>Batch size:</Text>
              <Select
                size="small"
                value={batchSize}
                onChange={setBatchSize}
                options={BATCH_SIZES.map(n => ({ value: n, label: `${n} per batch` }))}
                style={{ width: 120 }}
              />
              <Button
                size="small"
                onClick={() => setSelectedKeys(recipients.map(r => r.studentId))}
              >
                Select All
              </Button>
              <Button size="small" onClick={() => setSelectedKeys([])}>
                Clear
              </Button>
            </Space>
          }
        >
          <Table
            size="small"
            rowKey="studentId"
            columns={recipientCols}
            dataSource={recipients}
            pagination={{ pageSize: 50, showSizeChanger: false }}
            rowSelection={{
              selectedRowKeys: selectedKeys,
              onChange: keys => setSelectedKeys(keys),
            }}
            scroll={{ y: 320 }}
          />
        </Card>
      )}

      {/* ── Message Preview + Send ── */}
      {recipients.length > 0 && (
        <Card size="small" title="Message Preview & Send">
          {preview && (
            <Alert
              type="info"
              message="Preview (first selected student)"
              description={<Text style={{ whiteSpace: 'pre-wrap' }}>{preview}</Text>}
              style={{ marginBottom: 16 }}
            />
          )}

          {sending && (
            <div style={{ marginBottom: 16 }}>
              <Text>
                Sending batch {progress.current} of {progress.total}…
              </Text>
              <Progress
                percent={Math.round((progress.current / Math.max(progress.total, 1)) * 100)}
                status="active"
                style={{ marginTop: 4 }}
              />
              <Space style={{ marginTop: 4 }}>
                <Tag icon={<CheckCircleOutlined />} color="success">Sent: {progress.sent}</Tag>
                <Tag icon={<CloseCircleOutlined />} color="error">Failed: {progress.failed}</Tag>
              </Space>
            </div>
          )}

          {sendDone && !sending && (
            <div style={{ marginBottom: 16 }}>
              <Row gutter={16}>
                <Col>
                  <Statistic
                    title="Sent"
                    value={progress.sent}
                    valueStyle={{ color: '#3f8600' }}
                    prefix={<CheckCircleOutlined />}
                  />
                </Col>
                <Col>
                  <Statistic
                    title="Failed"
                    value={progress.failed}
                    valueStyle={{ color: progress.failed > 0 ? '#cf1322' : '#3f8600' }}
                    prefix={<CloseCircleOutlined />}
                  />
                </Col>
              </Row>

              {errors.length > 0 && (
                <div style={{ marginTop: 12 }}>
                  <Divider orientation="left" style={{ fontSize: 12 }}>Failed Recipients</Divider>
                  <Table
                    size="small"
                    rowKey="studentId"
                    dataSource={errors}
                    pagination={false}
                    columns={[
                      { title: 'Student',  dataIndex: 'studentName', key: 'n' },
                      { title: 'Mobile',   dataIndex: 'mobile',      key: 'm', width: 130 },
                      { title: 'Reason',   dataIndex: 'reason',      key: 'r', render: r => <Text type="danger">{r}</Text> },
                    ]}
                    scroll={{ y: 200 }}
                  />
                </div>
              )}
            </div>
          )}

          <Button
            type="primary"
            icon={<SendOutlined />}
            size="large"
            loading={sending}
            disabled={selectedKeys.length === 0}
            onClick={sendSms}
          >
            {sending ? 'Sending…' : `Send SMS to ${selectedKeys.length} student${selectedKeys.length !== 1 ? 's' : ''}`}
          </Button>
        </Card>
      )}
    </div>
  )
}

// ── History Tab ───────────────────────────────────────────────────────────────

function HistoryTab() {
  const { message } = AntApp.useApp()

  const [logs,      setLogs]      = useState([])
  const [loading,   setLoading]   = useState(false)
  const [smsType,   setSmsType]   = useState(null)
  const [dateRange, setDateRange] = useState([null, null])

  const loadLogs = async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (smsType)         params.append('smsType',  smsType)
      if (dateRange[0])    params.append('dateFrom', dateRange[0].format('YYYY-MM-DD'))
      if (dateRange[1])    params.append('dateTo',   dateRange[1].format('YYYY-MM-DD'))
      const r = await api.get(`/school/sms/logs?${params}`)
      setLogs(r.data?.data || [])
    } catch {
      message.error('Failed to load SMS history.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { loadLogs() }, []) // eslint-disable-line

  const cols = [
    { title: 'Date/Time',  dataIndex: 'sentAt',       key: 'at',   width: 140 },
    { title: 'Type',       dataIndex: 'smsType',      key: 'type', width: 110,
      render: t => <Tag color={t === 'Absent' ? 'orange' : t === 'FeeDue' ? 'red' : 'blue'}>{t}</Tag> },
    { title: 'Student',    dataIndex: 'studentName',  key: 'name', ellipsis: true },
    { title: 'Mobile',     dataIndex: 'mobile',       key: 'mob',  width: 130 },
    { title: 'Message',    dataIndex: 'message',      key: 'msg',  ellipsis: true },
    { title: 'Status',     dataIndex: 'status',       key: 'stat', width: 90,
      render: s => <Tag color={s === 'Sent' ? 'success' : 'error'}>{s}</Tag> },
    { title: 'Sent By',    dataIndex: 'sentBy',       key: 'by',   width: 130, ellipsis: true },
  ]

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <Card size="small">
        <Row gutter={[12, 8]} align="bottom">
          <Col xs={24} sm={6}>
            <div style={{ marginBottom: 4 }}><Text strong>SMS Type</Text></div>
            <Select
              style={{ width: '100%' }}
              placeholder="All types"
              allowClear
              value={smsType}
              onChange={setSmsType}
              options={SMS_TYPES}
            />
          </Col>
          <Col xs={24} sm={9}>
            <div style={{ marginBottom: 4 }}><Text strong>Date Range</Text></div>
            <DatePicker.RangePicker
              style={{ width: '100%' }}
              value={dateRange}
              onChange={v => setDateRange(v || [null, null])}
            />
          </Col>
          <Col xs={24} sm={4}>
            <Button type="primary" icon={<ReloadOutlined />} onClick={loadLogs} loading={loading} style={{ width: '100%' }}>
              Search
            </Button>
          </Col>
        </Row>
      </Card>

      <Card size="small" title={<><HistoryOutlined style={{ marginRight: 6 }} />SMS History (last 500)</>}>
        <Table
          size="small"
          rowKey="logId"
          columns={cols}
          dataSource={logs}
          loading={loading}
          pagination={{ pageSize: 50, showSizeChanger: false }}
          scroll={{ y: 480 }}
        />
      </Card>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function SMSPage() {
  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>
        <MessageOutlined style={{ marginRight: 8 }} />
        SMS Center
      </Title>
      <Tabs
        items={[
          { key: 'send',    label: <><SendOutlined />   Send SMS</>,   children: <SendTab />    },
          { key: 'history', label: <><HistoryOutlined />SMS History</>, children: <HistoryTab /> },
        ]}
      />
    </div>
  )
}
