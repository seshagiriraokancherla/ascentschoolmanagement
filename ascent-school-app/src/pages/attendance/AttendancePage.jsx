import { useEffect, useState } from 'react'
import {
  Card, Select, DatePicker, Button, Table, Tag, Space,
  Typography, App as AntApp, Row, Col, Radio, Input, Tabs, Popconfirm, Empty,
} from 'antd'
import {
  SearchOutlined, SaveOutlined, CheckCircleOutlined, DeleteOutlined,
} from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

const STATUS_OPTIONS = ['Present', 'Absent', 'Late', 'HalfDay', 'Holiday']
const STATUS_COLOR   = { Present: 'success', Absent: 'error', Late: 'warning', HalfDay: 'blue', Holiday: 'default' }
const STATUS_LABEL   = { HalfDay: 'Half Day' }

export default function AttendancePage() {
  const { message, modal } = AntApp.useApp()

  const [classes,          setClasses]          = useState([])
  const [selectedClass,    setSelectedClass]    = useState(null)
  const [sections,         setSections]         = useState([])
  const [selectedSection,  setSelectedSection]  = useState(null)
  const [selectedDate,     setSelectedDate]     = useState(dayjs())

  const [grid,    setGrid]    = useState(null)
  const [entries, setEntries] = useState({})   // studentId → { status, remarks }
  const [loading, setLoading] = useState(false)
  const [saving,  setSaving]  = useState(false)

  // Monthly summary tab
  const [summaryClass,    setSummaryClass]    = useState(null)
  const [summarySections, setSummarySections] = useState([])
  const [summarySection,  setSummarySection]  = useState(null)
  const [summaryMonth,    setSummaryMonth]    = useState(dayjs())
  const [summary,         setSummary]         = useState([])
  const [summaryLoad,     setSummaryLoad]     = useState(false)

  // Delete attendance tab
  const [delClass,    setDelClass]    = useState(null)
  const [delSections, setDelSections] = useState([])
  const [delSection,  setDelSection]  = useState(null)
  const [delDate,     setDelDate]     = useState(dayjs())
  const [delGrid,     setDelGrid]     = useState(null)
  const [delLoading,  setDelLoading]  = useState(false)
  const [deleting,    setDeleting]    = useState(false)

  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data.data || []))
  }, [])

  async function loadSections(classId, setter) {
    if (!classId) { setter([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${classId}`)
      setter(r.data?.data || [])
    } catch { setter([]) }
  }

  const loadGrid = async () => {
    if (!selectedClass || !selectedSection || !selectedDate) {
      message.warning('Select class, section and date first.')
      return
    }
    setLoading(true)
    try {
      const date = selectedDate.format('YYYY-MM-DD')
      const r    = await api.get(`/school/attendance?classId=${selectedClass}&sectionId=${selectedSection}&date=${date}`)
      const g    = r.data.data
      setGrid(g)

      const init = {}
      g.students.forEach(s => {
        init[s.studentId] = { status: s.status || 'Present', remarks: s.remarks || '' }
      })
      setEntries(init)
    } catch {
      message.error('Failed to load attendance.')
    } finally {
      setLoading(false)
    }
  }

  const markAll = (status) => {
    setEntries(prev => {
      const next = { ...prev }
      Object.keys(next).forEach(id => { next[id] = { ...next[id], status } })
      return next
    })
  }

  const setStatus = (studentId, status) =>
    setEntries(prev => ({ ...prev, [studentId]: { ...prev[studentId], status } }))

  const setRemarks = (studentId, remarks) =>
    setEntries(prev => ({ ...prev, [studentId]: { ...prev[studentId], remarks } }))

  const handleSave = async () => {
    if (!grid) return
    setSaving(true)
    try {
      await api.post('/school/attendance', {
        classId: selectedClass,
        date:    selectedDate.format('YYYY-MM-DD'),
        entries: Object.entries(entries).map(([studentId, val]) => ({
          studentId: Number(studentId),
          status:    val.status,
          remarks:   val.remarks || null,
        })),
      })
      message.success('Attendance saved.')

      // Offer to SMS the absentees (same flow/template as SMS Center → Absent).
      const absentCount = Object.values(entries).filter(e => e.status === 'Absent').length

      // Reload to reflect isMarked flag
      loadGrid()

      if (absentCount > 0) {
        modal.confirm({
          title:      'Send SMS to absentees?',
          content:    `${absentCount} student(s) marked Absent. Send the Absent SMS to their parents?`,
          okText:     'Yes, send SMS',
          cancelText: 'No',
          onOk:       () => sendAbsentSms(),
        })
      }
    } catch (e) {
      message.error(e.message || 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  // Sends the Absent SMS to today's absentees for the loaded class/section, reusing the
  // SMS Center endpoints: the server recomputes absentees (with parent mobile) from the
  // just-saved attendance and uses the school's ABSENT template.
  const sendAbsentSms = async () => {
    const date = selectedDate.format('YYYY-MM-DD')
    try {
      const rr = await api.get(
        `/school/sms/recipients?smsType=Absent&date=${date}&classId=${selectedClass}&sectionId=${selectedSection}`
      )
      const list = rr.data?.data || []
      if (list.length === 0) {
        message.info('No absentees with a mobile number to SMS.')
        return
      }
      const res = await api.post('/school/sms/send', {
        smsType:    'Absent',
        date,
        recipients: list.map(s => ({
          studentId:         s.studentId,
          studentName:       s.studentName,
          admissionNo:       s.admissionNo,
          className:         s.className,
          mobile:            s.fatherMobile,
          outstandingAmount: 0,
        })),
      })
      const r = res.data?.data || {}
      message.success(`SMS sent: ${r.sent || 0}, failed: ${r.failed || 0}.`)
    } catch (e) {
      message.error(e?.response?.data?.message || e.message || 'Failed to send SMS.')
    }
  }

  const loadSummary = async () => {
    if (!summaryClass || !summarySection || !summaryMonth) {
      message.warning('Select class, section and month.')
      return
    }
    setSummaryLoad(true)
    try {
      const m = summaryMonth.month() + 1
      const y = summaryMonth.year()
      const r = await api.get(
        `/school/attendance/summary?classId=${summaryClass}&sectionId=${summarySection}&month=${m}&year=${y}`
      )
      setSummary(r.data.data || [])
    } finally {
      setSummaryLoad(false)
    }
  }

  // ── Delete attendance ─────────────────────────────────────────────────────
  const loadDelGrid = async () => {
    if (!delClass || !delDate) {
      message.warning('Select class and date first.')
      return
    }
    setDelLoading(true)
    try {
      const date = delDate.format('YYYY-MM-DD')
      // sectionId=0 = "all sections" (server maps 0 → whole class). Always send it so the
      // query param is present — an absent optional param 405s in Web API action selection.
      const r = await api.get(`/school/attendance?classId=${delClass}&sectionId=${delSection || 0}&date=${date}`)
      setDelGrid(r.data.data)
    } catch {
      message.error('Failed to load attendance.')
    } finally {
      setDelLoading(false)
    }
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      const date = delDate.format('YYYY-MM-DD')
      // sectionId=0 = "all sections" (server maps 0 → whole class); always sent so the param is present.
      const r = await api.delete(`/school/attendance?classId=${delClass}&sectionId=${delSection || 0}&date=${date}`)
      message.success(r.data?.message || 'Attendance deleted.')
      loadDelGrid()  // refresh — should now show "Not marked"
    } catch (e) {
      message.error(e.message || 'Failed to delete attendance.')
    } finally {
      setDeleting(false)
    }
  }

  const delMarkedCount = (delGrid?.students || []).filter(s => s.status).length

  // ── Mark attendance columns ───────────────────────────────────────────────
  const markColumns = [
    {
      title: '#',
      key: 'sno',
      width: 60,
      render: (_, __, index) => index + 1,
    },
    {
      title: 'Student',
      key: 'student',
      render: (_, s) => (
        <div>
          <div style={{ fontWeight: 500 }}>{s.studentName}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>{s.admissionNo}</Text>
        </div>
      ),
    },
    {
      title: 'Status',
      key: 'status',
      width: 320,
      render: (_, s) => (
        <Radio.Group
          value={entries[s.studentId]?.status || 'Present'}
          onChange={e => setStatus(s.studentId, e.target.value)}
          size="small"
        >
          {STATUS_OPTIONS.map(st => (
            <Radio.Button key={st} value={st}>
              <Tag color={STATUS_COLOR[st]} style={{ margin: 0, cursor: 'pointer' }}>{STATUS_LABEL[st] || st}</Tag>
            </Radio.Button>
          ))}
        </Radio.Group>
      ),
    },
    {
      title: 'Remarks',
      key: 'remarks',
      render: (_, s) => (
        <Input
          size="small"
          placeholder="Optional"
          value={entries[s.studentId]?.remarks || ''}
          onChange={e => setRemarks(s.studentId, e.target.value)}
          style={{ width: 180 }}
        />
      ),
    },
  ]

  // ── Monthly summary columns ───────────────────────────────────────────────
  const summaryColumns = [
    { title: 'Student',     dataIndex: 'studentName', key: 'studentName' },
    { title: 'Adm. No',     dataIndex: 'admissionNo', key: 'admissionNo', width: 100 },
    { title: 'Present',     dataIndex: 'presentDays', key: 'presentDays', width: 80,
      render: d => <Tag color="success">{d}</Tag> },
    { title: 'Absent',      dataIndex: 'absentDays',  key: 'absentDays',  width: 80,
      render: d => <Tag color="error">{d}</Tag> },
    { title: 'Late',        dataIndex: 'lateDays',    key: 'lateDays',    width: 80,
      render: d => d > 0 ? <Tag color="warning">{d}</Tag> : <Tag>{d}</Tag> },
    { title: 'Half Day',    dataIndex: 'halfDayDays', key: 'halfDayDays', width: 90,
      render: d => d > 0 ? <Tag color="blue">{d}</Tag> : <Tag>{d || 0}</Tag> },
    { title: 'Days Marked', dataIndex: 'totalMarked', key: 'totalMarked', width: 100 },
    {
      title: 'Attendance %',
      key: 'pct',
      width: 120,
      render: (_, r) => {
        const pct = r.totalMarked > 0
          ? Math.round(((r.presentDays + r.lateDays + 0.5 * (r.halfDayDays || 0)) / r.totalMarked) * 100)
          : 0
        const color = pct >= 85 ? 'success' : pct >= 75 ? 'warning' : 'error'
        return <Tag color={color}>{pct}%</Tag>
      },
    },
  ]

  const presentCount = Object.values(entries).filter(e => e.status === 'Present').length
  const absentCount  = Object.values(entries).filter(e => e.status === 'Absent').length
  const lateCount    = Object.values(entries).filter(e => e.status === 'Late').length
  const halfDayCount = Object.values(entries).filter(e => e.status === 'HalfDay').length

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Attendance</Title>

      <Tabs
        items={[
          {
            key:   'mark',
            label: 'Mark Attendance',
            children: (
              <>
                <Card style={{ marginBottom: 16 }}>
                  <Row gutter={16} align="middle" wrap>
                    <Col>
                      <Text strong>Class</Text>
                      <Select
                        style={{ display: 'block', width: 160, marginTop: 4 }}
                        placeholder="Select class"
                        value={selectedClass}
                        onChange={v => {
                          setSelectedClass(v)
                          setSelectedSection(null)
                          setGrid(null)
                          loadSections(v, setSections)
                        }}
                        options={classes.map(c => ({ label: c.className, value: c.classId }))}
                      />
                    </Col>
                    <Col>
                      <Text strong>Section</Text>
                      <Select
                        style={{ display: 'block', width: 130, marginTop: 4 }}
                        placeholder="Select section"
                        value={selectedSection}
                        disabled={sections.length === 0}
                        onChange={v => { setSelectedSection(v); setGrid(null) }}
                        options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
                      />
                    </Col>
                    <Col>
                      <Text strong>Date</Text>
                      <DatePicker
                        style={{ display: 'block', marginTop: 4 }}
                        value={selectedDate}
                        onChange={v => { setSelectedDate(v); setGrid(null) }}
                        format="DD/MM/YYYY"
                        disabledDate={d => d && d.isAfter(dayjs(), 'day')}
                      />
                    </Col>
                    <Col style={{ marginTop: 20 }}>
                      <Button
                        type="primary"
                        icon={<SearchOutlined />}
                        loading={loading}
                        onClick={loadGrid}
                      >
                        Load
                      </Button>
                    </Col>
                  </Row>
                </Card>

                {grid && (
                  <Card
                    title={
                      <Space>
                        <span>{grid.className} — {dayjs(grid.date).format('DD MMM YYYY')}</span>
                        {grid.isMarked && <Tag color="blue">Already marked</Tag>}
                        {grid.students.length > 0 && (
                          <Space size={4}>
                            <Tag color="success">P: {presentCount}</Tag>
                            <Tag color="error">A: {absentCount}</Tag>
                            {lateCount > 0 && <Tag color="warning">L: {lateCount}</Tag>}
                            {halfDayCount > 0 && <Tag color="blue">HD: {halfDayCount}</Tag>}
                          </Space>
                        )}
                      </Space>
                    }
                    extra={
                      <Space>
                        <Button size="small" onClick={() => markAll('Present')} icon={<CheckCircleOutlined />}>All Present</Button>
                        <Button size="small" onClick={() => markAll('Absent')} danger>All Absent</Button>
                        <Button
                          type="primary"
                          icon={<SaveOutlined />}
                          loading={saving}
                          onClick={handleSave}
                        >
                          Save
                        </Button>
                      </Space>
                    }
                  >
                    <Table
                      dataSource={grid.students}
                      columns={markColumns}
                      rowKey="studentId"
                      pagination={false}
                      size="middle"
                      locale={{ emptyText: 'No active students in this class.' }}
                    />
                  </Card>
                )}
              </>
            ),
          },
          {
            key:   'summary',
            label: 'Monthly Summary',
            children: (
              <>
                <Card style={{ marginBottom: 16 }}>
                  <Row gutter={16} align="middle" wrap>
                    <Col>
                      <Text strong>Class</Text>
                      <Select
                        style={{ display: 'block', width: 160, marginTop: 4 }}
                        placeholder="Select class"
                        value={summaryClass}
                        onChange={v => {
                          setSummaryClass(v)
                          setSummarySection(null)
                          loadSections(v, setSummarySections)
                        }}
                        options={classes.map(c => ({ label: c.className, value: c.classId }))}
                      />
                    </Col>
                    <Col>
                      <Text strong>Section</Text>
                      <Select
                        style={{ display: 'block', width: 130, marginTop: 4 }}
                        placeholder="Select section"
                        value={summarySection}
                        disabled={summarySections.length === 0}
                        onChange={setSummarySection}
                        options={summarySections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
                      />
                    </Col>
                    <Col>
                      <Text strong>Month</Text>
                      <DatePicker.MonthPicker
                        style={{ display: 'block', marginTop: 4 }}
                        value={summaryMonth}
                        onChange={setSummaryMonth}
                        format="MMM YYYY"
                      />
                    </Col>
                    <Col style={{ marginTop: 20 }}>
                      <Button
                        type="primary"
                        icon={<SearchOutlined />}
                        loading={summaryLoad}
                        onClick={loadSummary}
                      >
                        View
                      </Button>
                    </Col>
                  </Row>
                </Card>

                <Card>
                  <Table
                    dataSource={summary}
                    columns={summaryColumns}
                    rowKey="studentName"
                    loading={summaryLoad}
                    pagination={false}
                    size="middle"
                  />
                </Card>
              </>
            ),
          },
          {
            key:   'delete',
            label: 'Delete Attendance',
            children: (
              <>
                <Card style={{ marginBottom: 16 }}>
                  <Row gutter={16} align="middle" wrap>
                    <Col>
                      <Text strong>Class</Text>
                      <Select
                        style={{ display: 'block', width: 160, marginTop: 4 }}
                        placeholder="Select class"
                        value={delClass}
                        onChange={v => {
                          setDelClass(v)
                          setDelSection(null)
                          setDelGrid(null)
                          loadSections(v, setDelSections)
                        }}
                        options={classes.map(c => ({ label: c.className, value: c.classId }))}
                      />
                    </Col>
                    <Col>
                      <Text strong>Section</Text>
                      <Select
                        style={{ display: 'block', width: 200, marginTop: 4 }}
                        placeholder="All Sections (whole class)"
                        value={delSection}
                        allowClear
                        disabled={!delClass}
                        onChange={v => { setDelSection(v ?? null); setDelGrid(null) }}
                        options={[
                          { label: 'All Sections (whole class)', value: null },
                          ...delSections.map(s => ({ label: s.sectionName, value: s.sectionId })),
                        ]}
                      />
                    </Col>
                    <Col>
                      <Text strong>Date</Text>
                      <DatePicker
                        style={{ display: 'block', marginTop: 4 }}
                        value={delDate}
                        onChange={v => { setDelDate(v); setDelGrid(null) }}
                        format="DD/MM/YYYY"
                        disabledDate={d => d && d.isAfter(dayjs(), 'day')}
                      />
                    </Col>
                    <Col style={{ marginTop: 20 }}>
                      <Button
                        type="primary"
                        icon={<SearchOutlined />}
                        loading={delLoading}
                        onClick={loadDelGrid}
                      >
                        Load
                      </Button>
                    </Col>
                  </Row>
                </Card>

                {delGrid && (
                  <Card
                    title={
                      <Space>
                        <span>{delGrid.className} — {dayjs(delGrid.date).format('DD MMM YYYY')}</span>
                        {delGrid.isMarked
                          ? <Tag color="blue">{delMarkedCount} marked</Tag>
                          : <Tag>Not marked</Tag>}
                      </Space>
                    }
                    extra={
                      <Popconfirm
                        title="Delete attendance?"
                        description={`This permanently removes attendance for ${delMarkedCount} student(s) — ${delSection ? 'this section' : 'the whole class (all sections)'} — on ${dayjs(delGrid.date).format('DD MMM YYYY')}.`}
                        okText="Yes, delete"
                        okButtonProps={{ danger: true }}
                        cancelText="Cancel"
                        disabled={!delGrid.isMarked}
                        onConfirm={handleDelete}
                      >
                        <Button danger icon={<DeleteOutlined />} loading={deleting} disabled={!delGrid.isMarked}>
                          Delete Attendance
                        </Button>
                      </Popconfirm>
                    }
                  >
                    {delGrid.isMarked ? (
                      <Table
                        dataSource={delGrid.students.filter(s => s.status)}
                        rowKey="studentId"
                        pagination={false}
                        size="small"
                        columns={[
                          { title: '#', key: 'sno', width: 60, render: (_, __, i) => i + 1 },
                          {
                            title: 'Student', key: 'student',
                            render: (_, s) => (
                              <div>
                                <div style={{ fontWeight: 500 }}>{s.studentName}</div>
                                <Text type="secondary" style={{ fontSize: 12 }}>{s.admissionNo}</Text>
                              </div>
                            ),
                          },
                          { title: 'Section', dataIndex: 'sectionName', key: 'sectionName', width: 100,
                            render: s => s || '—' },
                          {
                            title: 'Status', dataIndex: 'status', key: 'status', width: 140,
                            render: st => <Tag color={STATUS_COLOR[st] || 'default'}>{STATUS_LABEL[st] || st}</Tag>,
                          },
                          { title: 'Remarks', dataIndex: 'remarks', key: 'remarks', render: r => r || '—' },
                        ]}
                      />
                    ) : (
                      <Empty description="No attendance marked for this class, section and date." />
                    )}
                  </Card>
                )}
              </>
            ),
          },
        ]}
      />
    </div>
  )
}
