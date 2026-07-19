import { useEffect, useState } from 'react'
import {
  Card, Select, DatePicker, Button, Input, Typography, App as AntApp,
  Row, Col, Space, Spin, Empty,
} from 'antd'
import { SaveOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography
const { TextArea } = Input

// Treat both new ('Active') and legacy ('Y') as active subjects.
const isActive = (s) => ['Active', 'Y', null, undefined, ''].includes(s.status)

export default function DailyHomeworkPage() {
  const { message } = AntApp.useApp()

  const [date,      setDate]      = useState(dayjs())
  const [classes,   setClasses]   = useState([])
  const [sections,  setSections]  = useState([])
  const [subjects,  setSubjects]  = useState([])
  const [classId,   setClassId]   = useState(null)
  const [sectionId, setSectionId] = useState(null)
  const [texts,     setTexts]     = useState({})   // { [subjectId]: description }
  const [loading,   setLoading]   = useState(false)
  const [saving,    setSaving]    = useState(false)

  // Lookups on mount
  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
    api.get('/school/master/subjects').then(r =>
      setSubjects((r.data?.data || []).filter(isActive)))
  }, [])

  const loadSections = async (cid) => {
    if (!cid) { setSections([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${cid}`)
      setSections(r.data?.data || [])
    } catch { setSections([]) }
  }

  // Pre-fill the textboxes from any homework already saved for this Class+Section+Date.
  const loadExisting = async (cid, secId, d) => {
    if (!cid || !secId || !d) { setTexts({}); return }
    setLoading(true)
    try {
      const params = new URLSearchParams({
        classId:      cid,
        sectionId:    secId,
        assignedDate: dayjs(d).format('YYYY-MM-DD'),
        pageSize:     200,
      })
      const r = await api.get(`/school/homework?${params}`)
      const rows = r.data?.data?.items || []
      const map = {}
      rows.forEach(h => { if (h.subjectId) map[h.subjectId] = h.description || '' })
      setTexts(map)
    } catch {
      setTexts({})
    } finally {
      setLoading(false)
    }
  }

  const onClassChange = (val) => {
    setClassId(val)
    setSectionId(null)
    setTexts({})
    loadSections(val)
  }

  const onSectionChange = (val) => {
    setSectionId(val)
    loadExisting(classId, val, date)
  }

  const onDateChange = (val) => {
    setDate(val)
    loadExisting(classId, sectionId, val)
  }

  const setText = (subjectId, value) =>
    setTexts(prev => ({ ...prev, [subjectId]: value }))

  const handleSave = async () => {
    if (!classId)   { message.warning('Select a class.');   return }
    if (!sectionId) { message.warning('Select a section.'); return }
    if (!date)      { message.warning('Select a date.');    return }

    const items = subjects
      .filter(s => (texts[s.subjectId] || '').trim())
      .map(s => ({ subjectId: s.subjectId, description: texts[s.subjectId].trim() }))

    if (items.length === 0) { message.warning('Enter homework for at least one subject.'); return }

    setSaving(true)
    try {
      const r = await api.post('/school/homework/batch', {
        classId,
        sectionId,
        assignedDate: date.format('YYYY-MM-DD'),
        items,
      })
      message.success(r.data?.message || 'Homework saved.')
      loadExisting(classId, sectionId, date)
    } catch (e) {
      message.error(e.message || 'Failed to save homework.')
    } finally {
      setSaving(false)
    }
  }

  const ready = classId && sectionId && date

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Daily Homework</Title>

      <Card size="small" style={{ marginBottom: 16 }}>
        <Space size="large" wrap>
          <Space>
            <Text strong>Date</Text>
            <DatePicker value={date} onChange={onDateChange} format="DD-MMM-YYYY" allowClear={false} />
          </Space>
          <Space>
            <Text strong>Class</Text>
            <Select
              style={{ width: 180 }}
              placeholder="Select class"
              value={classId}
              onChange={onClassChange}
              options={classes.map(c => ({ label: c.className, value: c.classId }))}
            />
          </Space>
          <Space>
            <Text strong>Section</Text>
            <Select
              style={{ width: 180 }}
              placeholder="Select section"
              value={sectionId}
              onChange={onSectionChange}
              disabled={!classId || sections.length === 0}
              options={sections.map(s => ({ label: s.sectionName, value: s.sectionId }))}
            />
          </Space>
        </Space>
      </Card>

      <Card
        title="Subjects"
        extra={
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={saving}
            disabled={!ready}
            onClick={handleSave}
          >
            Save
          </Button>
        }
      >
        {!ready ? (
          <Empty description="Select date, class and section to enter homework" />
        ) : subjects.length === 0 ? (
          <Empty description="No active subjects found. Add subjects in Master Data." />
        ) : (
          <Spin spinning={loading}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {subjects.map(s => (
                <Row key={s.subjectId} gutter={12} align="middle">
                  <Col xs={24} sm={6} md={5} lg={4}>
                    <Text strong>{s.subjectName}</Text>
                  </Col>
                  <Col xs={24} sm={18} md={19} lg={20}>
                    <TextArea
                      autoSize={{ minRows: 1, maxRows: 4 }}
                      value={texts[s.subjectId] || ''}
                      onChange={e => setText(s.subjectId, e.target.value)}
                      placeholder={`Homework for ${s.subjectName}`}
                    />
                  </Col>
                </Row>
              ))}
            </div>
          </Spin>
        )}
      </Card>
    </div>
  )
}
