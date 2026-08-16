import { useEffect, useState } from 'react'
import {
  Card, Table, Tag, Select, DatePicker, Button, Space, Drawer,
  Typography, Empty, Spin, Descriptions,
} from 'antd'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { RangePicker } = DatePicker
const { Text, Paragraph } = Typography

const STATUS_COLOR = { Active: 'green', Blocked: 'red' }

export default function ConversationsPage() {
  // ── reference data ──
  const [years,    setYears]    = useState([])
  const [classes,  setClasses]  = useState([])
  const [sections, setSections] = useState([])
  const [students, setStudents] = useState([])
  const [teachers, setTeachers] = useState([])

  // ── filters ──
  const [yearId,    setYearId]    = useState(null)
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)
  const [uniqueId,  setUniqueId]  = useState(null)   // student_unique_id
  const [teacherId, setTeacherId] = useState(null)
  const [range,     setRange]     = useState([dayjs().subtract(5, 'day'), dayjs()])

  // ── results ──
  const [rows,    setRows]    = useState([])
  const [loading, setLoading] = useState(false)

  // ── conversation drawer ──
  const [open,       setOpen]       = useState(false)
  const [detail,     setDetail]     = useState(null)
  const [detailBusy, setDetailBusy] = useState(false)

  useEffect(() => {
    (async () => {
      const [y, c, t] = await Promise.all([
        api.get('/school/master/academic-years?activeOnly=true'),
        api.get('/school/master/classes'),
        api.get('/school/class-teachers/teachers'),
      ])
      const yl = y.data.data || []
      setYears(yl)
      setClasses(c.data.data || [])
      setTeachers(t.data.data || [])
      const current = yl.find((x) => x.isCurrent) || yl[0]
      if (current) {
        setYearId(current.academicYearId)
        loadThreads({ academicYearId: current.academicYearId })
      }
    })()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function onClassChange(val) {
    setClassId(val); setSectionId(null); setUniqueId(null); setSections([]); setStudents([])
    if (val) {
      const { data } = await api.get(`/school/master/sections?classId=${val}`)
      setSections(data.data || [])
    }
  }

  async function loadStudents(nextSection) {
    setUniqueId(null)
    if (!classId) { setStudents([]); return }
    const params = new URLSearchParams()
    if (yearId)               params.append('academicYearId', yearId)
    params.append('classId', classId)
    if (nextSection ?? sectionId) params.append('sectionId', nextSection ?? sectionId)
    const { data } = await api.get(`/school/students?${params.toString()}`)
    setStudents((data.data || []).filter((s) => s.studentUniqueId))
  }

  function onSectionChange(val) {
    setSectionId(val)
    loadStudents(val)
  }

  async function loadThreads(overrides = {}) {
    setLoading(true)
    try {
      const p = new URLSearchParams()
      const yr = overrides.academicYearId ?? yearId
      if (yr)        p.append('academicYearId', yr)
      if (classId)   p.append('classId', classId)
      if (sectionId) p.append('sectionId', sectionId)
      if (uniqueId)  p.append('studentUniqueId', uniqueId)
      if (teacherId) p.append('teacherUserId', teacherId)
      if (range?.[0]) p.append('dateFrom', range[0].format('YYYY-MM-DD'))
      if (range?.[1]) p.append('dateTo',   range[1].format('YYYY-MM-DD'))
      const { data } = await api.get(`/school/messages/threads?${p.toString()}`)
      setRows(data.data || [])
    } finally {
      setLoading(false)
    }
  }

  async function openThread(record) {
    setOpen(true); setDetail(null); setDetailBusy(true)
    try {
      const { data } = await api.get(`/school/messages/threads/${record.threadId}`)
      setDetail(data.data)
    } finally {
      setDetailBusy(false)
    }
  }

  const columns = [
    { title: 'S.No', key: 'sno', width: 60, render: (_, __, i) => i + 1 },
    {
      title: 'Student', key: 'student',
      render: (_, r) => (
        <div>
          <div>{r.studentName || '—'}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>{r.admissionNo}</Text>
        </div>
      ),
    },
    {
      title: 'Class / Section', key: 'cls', width: 150,
      render: (_, r) => `${r.className || '—'}${r.sectionName ? ' · ' + r.sectionName : ''}`,
    },
    { title: 'Teacher(s)', dataIndex: 'teacherNames', key: 'teacherNames', width: 180,
      render: (v) => v || <Text type="secondary">—</Text> },
    {
      title: 'Last message', dataIndex: 'lastMessageBody', key: 'lastMessageBody',
      render: (v) => <Paragraph style={{ marginBottom: 0 }} ellipsis={{ rows: 2 }}>{v || ''}</Paragraph>,
    },
    {
      title: 'Last activity', dataIndex: 'lastMessageAt', key: 'lastMessageAt', width: 150,
      render: (v) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : ''),
    },
    { title: 'Msgs', dataIndex: 'messageCount', key: 'messageCount', width: 70, align: 'center' },
    {
      title: 'Status', dataIndex: 'status', key: 'status', width: 90,
      render: (v) => <Tag color={STATUS_COLOR[v] || 'default'}>{v}</Tag>,
    },
  ]

  return (
    <Card title="Conversations" bodyStyle={{ paddingTop: 12 }}>
      <Space wrap style={{ marginBottom: 16 }}>
        <Select
          style={{ width: 150 }} placeholder="Academic Year" value={yearId} onChange={setYearId}
          options={years.map((y) => ({ value: y.academicYearId, label: y.academicYear }))}
        />
        <Select
          style={{ width: 160 }} placeholder="Class" value={classId} onChange={onClassChange} allowClear
          options={classes.map((c) => ({ value: c.classId, label: c.className }))}
        />
        <Select
          style={{ width: 150 }} placeholder="Section" value={sectionId} onChange={onSectionChange}
          allowClear disabled={!classId}
          options={sections.map((s) => ({ value: s.sectionId, label: s.sectionName }))}
        />
        <Select
          style={{ width: 200 }} placeholder="Student" value={uniqueId} onChange={setUniqueId}
          allowClear showSearch optionFilterProp="label" disabled={!classId}
          options={students.map((s) => ({
            value: s.studentUniqueId, label: `${s.studentName} (${s.admissionNo})`,
          }))}
        />
        <Select
          style={{ width: 180 }} placeholder="Teacher" value={teacherId} onChange={setTeacherId}
          allowClear showSearch optionFilterProp="label"
          options={teachers.map((t) => ({ value: t.userId, label: t.fullName }))}
        />
        <RangePicker value={range} onChange={setRange} allowClear={false} format="YYYY-MM-DD" />
        <Button type="primary" onClick={() => loadThreads()} loading={loading}>Search</Button>
      </Space>

      <Table
        rowKey="threadId"
        dataSource={rows}
        columns={columns}
        loading={loading}
        onRow={(r) => ({ onClick: () => openThread(r), style: { cursor: 'pointer' } })}
        pagination={{ pageSize: 20, showSizeChanger: true, showTotal: (t) => `${t} conversations` }}
        locale={{ emptyText: 'No conversations in this range.' }}
      />

      <Drawer
        title={detail?.thread
          ? `${detail.thread.studentName || 'Conversation'} · ${detail.thread.className || ''}${detail.thread.sectionName ? ' ' + detail.thread.sectionName : ''}`
          : 'Conversation'}
        width={520}
        open={open}
        onClose={() => setOpen(false)}
      >
        {detailBusy ? (
          <div style={{ textAlign: 'center', padding: 40 }}><Spin /></div>
        ) : !detail ? (
          <Empty />
        ) : (
          <>
            <Descriptions size="small" column={1} style={{ marginBottom: 16 }}>
              <Descriptions.Item label="Admission No">{detail.thread.admissionNo || '—'}</Descriptions.Item>
              <Descriptions.Item label="Status">
                <Tag color={STATUS_COLOR[detail.thread.status] || 'default'}>{detail.thread.status}</Tag>
              </Descriptions.Item>
            </Descriptions>

            {(detail.messages || []).length === 0 ? (
              <Empty description="No messages" />
            ) : (
              (detail.messages || []).map((m) => {
                const isTeacher = m.senderType === 'teacher'
                const removed = m.status === 'Removed'
                return (
                  <div key={m.messageId}
                    style={{ display: 'flex', justifyContent: isTeacher ? 'flex-end' : 'flex-start', marginBottom: 10 }}>
                    <div style={{
                      maxWidth: '78%',
                      background: isTeacher ? '#e6f4ff' : '#f5f5f5',
                      border: '1px solid #f0f0f0',
                      borderRadius: 10, padding: '8px 12px',
                    }}>
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        {m.senderName || (isTeacher ? 'Teacher' : 'Parent')} · {isTeacher ? 'Teacher' : 'Parent'}
                      </Text>
                      <div style={{ margin: '2px 0', color: removed ? '#999' : undefined, fontStyle: removed ? 'italic' : undefined }}>
                        {removed ? 'This message was removed.' : m.body}
                      </div>
                      <Text type="secondary" style={{ fontSize: 11 }}>
                        {m.createdAt ? dayjs(m.createdAt).format('YYYY-MM-DD HH:mm') : ''}
                      </Text>
                    </div>
                  </div>
                )
              })
            )}
          </>
        )}
      </Drawer>
    </Card>
  )
}
