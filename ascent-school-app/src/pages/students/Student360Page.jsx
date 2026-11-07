import { useCallback, useEffect, useRef, useState } from 'react'
import {
  Card, Select, Avatar, Tag, Row, Col, Statistic, Tabs, Table, Button, Space, Empty,
  Spin, Typography, Descriptions, Progress, Alert, App as AntApp,
} from 'antd'
import {
  UserOutlined, EditOutlined, DollarOutlined, PrinterOutlined, MobileOutlined, CommentOutlined,
} from '@ant-design/icons'
import { useNavigate, useSearchParams } from 'react-router-dom'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'
import { useAuthStore } from '../../store/authStore'
import { useBrandingStore } from '../../store/brandingStore'
import { P } from '../../config/permissions'
import { ReceiptPrintArea } from '../fee/ReceiptsPage'

const { Text } = Typography
const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

// Same categories as the five fee collection screens.
const FEE_CATEGORIES = ['Admission', 'School', 'Transport', 'Hostel', 'Other']
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

const money = (n) => Number(n || 0).toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
// Late = full day, Half Day = 0.5 (same weighting as the attendance pages)
const presentDays = (r) => (r.present || 0) + (r.late || 0) + 0.5 * (r.halfDay || 0)
const pct = (num, den) => (den > 0 ? Math.round((num / den) * 1000) / 10 : null)

export default function Student360Page() {
  const { message } = AntApp.useApp()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const { branding } = useBrandingStore()

  const canFees     = hasPermission(P.FEE_VIEW)
  const canAttend   = hasPermission(P.ATTENDANCE_VIEW)
  const canMarks    = hasPermission(P.MARKS_VIEW)
  const canSms      = hasPermission(P.SMS_SEND)
  const canMessages = hasPermission(P.MESSAGES_VIEW)
  const canEdit     = hasPermission(P.STUDENT_EDIT)
  const canCollect  = hasPermission(P.FEE_COLLECT)

  // ── Search ──────────────────────────────────────────────────────────
  const [options,   setOptions]   = useState([])
  const [searching, setSearching] = useState(false)
  const searchTimer = useRef(null)
  const searchSeq   = useRef(0)

  const onSearch = (text) => {
    clearTimeout(searchTimer.current)
    if (!text || text.trim().length < 2) { setOptions([]); return }
    searchTimer.current = setTimeout(async () => {
      const seq = ++searchSeq.current
      setSearching(true)
      try {
        const q = new URLSearchParams({ search: text.trim(), latestOnly: 'true' })
        const res = await api.get(`/school/students?${q}`)
        if (seq !== searchSeq.current) return
        setOptions((res.data?.data || []).slice(0, 50).map((s) => ({
          value: s.studentId,
          label: `${s.studentName} · ${s.admissionNo || '—'} · ${s.sectionName ? `${s.className} - ${s.sectionName}` : (s.className || '')}`,
        })))
      } catch (e) {
        message.error(apiError(e, 'Search failed.'))
      } finally {
        if (seq === searchSeq.current) setSearching(false)
      }
    }, 350)
  }

  // ── Selected student ────────────────────────────────────────────────
  const studentId = params.get('id') ? Number(params.get('id')) : null
  const [student,  setStudent]  = useState(null)   // GET /school/students/{id}
  const [overview, setOverview] = useState(null)   // GET /school/students/{id}/overview
  const [fees,     setFees]     = useState([])     // rows: category × year
  const [history,  setHistory]  = useState(null)
  const [loading,  setLoading]  = useState(false)
  const [yearId,   setYearId]   = useState(null)
  const [printReceipt, setPrintReceipt] = useState(null)
  const loadSeq = useRef(0)

  const loadFees = useCallback(async (uniqueId) => {
    if (!canFees || !uniqueId) { setFees([]); return }
    const results = await Promise.all(FEE_CATEGORIES.map((cat) =>
      api.get(`/school/fees/student-unique/${uniqueId}?feeTypeCategory=${cat}`)
        .then((r) => ({ cat, data: r.data?.data }))
        .catch(() => ({ cat, data: null }))))
    const rows = []
    results.forEach(({ cat, data }) => {
      (data?.years || []).forEach((y) => {
        if (!y.lineItems?.length) return
        rows.push({
          key: `${cat}_${y.academicYearId}`, category: cat,
          academicYear: y.academicYear, className: y.className,
          structure: y.totalStructure, paid: y.totalPaid, outstanding: y.totalOutstanding,
          concession: (y.lineItems || []).reduce((a, li) => a + (li.concessionAmount || 0), 0),
          lineItems: y.lineItems,
        })
      })
    })
    setFees(rows)
  }, [canFees])

  const load = useCallback(async (id, ayId) => {
    const seq = ++loadSeq.current
    setLoading(true)
    setHistory(null)
    try {
      const q = ayId ? `?academicYearId=${ayId}` : ''
      const ov = (await api.get(`/school/students/${id}/overview${q}`)).data?.data
      if (seq !== loadSeq.current) return
      const st = (await api.get(`/school/students/${ov.studentId}`)).data?.data
      if (seq !== loadSeq.current) return
      setOverview(ov)
      setStudent(st)
      setYearId(ov.academicYearId)
      loadFees(ov.studentUniqueId)
    } catch (e) {
      if (seq === loadSeq.current) {
        message.error(apiError(e, 'Failed to load student.'))
        setOverview(null); setStudent(null)
      }
    } finally {
      if (seq === loadSeq.current) setLoading(false)
    }
  }, [loadFees, message])

  useEffect(() => {
    if (studentId) load(studentId, null)
    else { setOverview(null); setStudent(null); setFees([]) }
  }, [studentId, load])

  const selectStudent = (id) => setParams(id ? { id: String(id) } : {})

  const changeYear = (ayId) => {
    setYearId(ayId)
    load(overview.studentId, ayId)
  }

  const loadHistory = async () => {
    if (history || !overview) return
    try {
      const res = await api.get(`/school/students/${overview.studentId}/history`)
      setHistory(res.data?.data || [])
    } catch (e) {
      message.error(apiError(e, 'Failed to load change history.'))
    }
  }

  const handlePrint = async (receiptId) => {
    try {
      const res = await api.get(`/school/fees/receipts/${receiptId}`)
      setPrintReceipt(res.data?.data)
      setTimeout(() => window.print(), 150)
    } catch (e) {
      message.error(apiError(e, 'Failed to load receipt for printing.'))
    }
  }

  // ── Derived figures ─────────────────────────────────────────────────
  const att        = overview?.attendance || []
  const attWorking = att.reduce((a, r) => a + r.workingDays, 0)
  const attPresent = att.reduce((a, r) => a + presentDays(r), 0)
  const attPct     = pct(attPresent, attWorking)
  const totalDue   = fees.reduce((a, r) => a + (r.outstanding || 0), 0)
  const lastExam   = (overview?.exams || []).slice(-1)[0]

  // ── Render ──────────────────────────────────────────────────────────
  const search = (
    <Select
      showSearch
      allowClear
      value={studentId && student ? studentId : undefined}
      placeholder="Search by student name or admission no (min 2 characters)"
      style={{ width: '100%', maxWidth: 560 }}
      filterOption={false}
      onSearch={onSearch}
      onChange={selectStudent}
      notFoundContent={searching ? <Spin size="small" /> : null}
      options={options.length || !student ? options : [{ value: studentId, label: `${student.studentName} · ${student.admissionNo || '—'}` }]}
    />
  )

  return (
    <>
      <Card title="Student 360" extra={search} styles={{ body: { padding: overview ? 16 : 24 } }}>
        {loading && !overview && <div style={{ textAlign: 'center', padding: 48 }}><Spin /></div>}
        {!loading && !overview && (
          <Empty description="Search and select a student to see their fees, attendance, marks and history in one place." />
        )}

        {overview && student && (
          <Spin spinning={loading}>
            {/* Header */}
            <Row gutter={[16, 16]} align="middle">
              <Col flex="none">
                {student.photoPath
                  ? <Avatar size={88} src={/^https?:\/\//.test(student.photoPath) ? student.photoPath : `${API_BASE}${student.photoPath}`} />
                  : <Avatar size={88} icon={<UserOutlined />} />}
              </Col>
              <Col flex="auto">
                <Space size={8} wrap>
                  <Text strong style={{ fontSize: 20 }}>{student.studentName}</Text>
                  <StatusTag status={student.status} />
                  {overview.isDetained && <Tag color="volcano">Detained</Tag>}
                  {overview.hasMobileApp
                    ? <Tag color="green" icon={<MobileOutlined />}>Parent app since {dayjs(overview.appLinkedOn).format('DD-MM-YYYY')}</Tag>
                    : <Tag icon={<MobileOutlined />}>Parent app not installed</Tag>}
                </Space>
                <div style={{ marginTop: 4, color: '#555' }}>
                  Adm No <b>{student.admissionNo || '—'}</b> · {student.className}{student.sectionName ? ` - ${student.sectionName}` : ''}
                  {student.rollNo ? ` · Roll ${student.rollNo}` : ''}
                </div>
                <div style={{ marginTop: 2, color: '#555' }}>
                  Father: {student.fatherName || '—'} {student.fatherMobile ? `(${student.fatherMobile})` : ''}
                  {' · '}Mother: {student.motherName || '—'} {student.motherMobile ? `(${student.motherMobile})` : ''}
                </div>
                {overview.blockedReason && student.status === 'Blocked' && (
                  <div style={{ color: '#cf1322', marginTop: 2 }}>Blocked: {overview.blockedReason}</div>
                )}
                {overview.isDetained && overview.detainedReason && (
                  <div style={{ color: '#ad4e00', marginTop: 2 }}>Detained: {overview.detainedReason}</div>
                )}
              </Col>
              <Col flex="none">
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: 8 }}>
                  <Select
                    style={{ width: 200 }}
                    value={yearId}
                    onChange={changeYear}
                    options={(overview.years || []).map((y) => ({
                      value: y.academicYearId,
                      label: `${y.academicYear || '—'} · ${y.className || ''}${y.sectionName ? ` - ${y.sectionName}` : ''}`,
                    }))}
                  />
                  <Space wrap>
                    {canEdit && <Button icon={<EditOutlined />} onClick={() => navigate(`/students/${overview.studentId}`)}>Edit</Button>}
                    {canCollect && <Button type="primary" icon={<DollarOutlined />} onClick={() => navigate('/fees/collect/school')}>Collect Fee</Button>}
                  </Space>
                </div>
              </Col>
            </Row>

            {/* Key figures */}
            <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
              {canFees && (
                <Col xs={12} md={6}>
                  <Card size="small"><Statistic title="Fee outstanding (all years)" value={money(totalDue)} prefix="₹"
                    valueStyle={{ color: totalDue > 0 ? '#cf1322' : '#389e0d' }} /></Card>
                </Col>
              )}
              {canAttend && (
                <Col xs={12} md={6}>
                  <Card size="small"><Statistic title={`Attendance ${overview.academicYear || ''}`}
                    value={attPct ?? '—'} suffix={attPct != null ? '%' : ''}
                    valueStyle={{ color: attPct == null ? undefined : attPct < 75 ? '#cf1322' : '#389e0d' }} /></Card>
                </Col>
              )}
              {canMarks && (
                <Col xs={12} md={6}>
                  <Card size="small"><Statistic title={lastExam ? `Latest exam: ${lastExam.examTypeName}` : 'Latest exam'}
                    value={lastExam ? pct(lastExam.total, lastExam.maxTotal) ?? '—' : '—'}
                    suffix={lastExam && lastExam.maxTotal > 0 ? `%  ·  Rank ${lastExam.rank}/${lastExam.rankOutOf}` : ''} /></Card>
                </Col>
              )}
              <Col xs={12} md={6}>
                <Card size="small"><Statistic title="Transport / Hostel"
                  value={overview.routeName || student.hostelName || 'None'}
                  valueStyle={{ fontSize: 18 }} /></Card>
              </Col>
            </Row>

            <Tabs
              style={{ marginTop: 16 }}
              onChange={(k) => { if (k === 'history') loadHistory() }}
              items={[
                canFees     && { key: 'fees',       label: 'Fees',          children: <FeesTab rows={fees} receipts={overview.receipts || []} onPrint={handlePrint} /> },
                canAttend   && { key: 'attendance', label: 'Attendance',    children: <AttendanceTab overview={overview} /> },
                canMarks    && { key: 'marks',      label: 'Marks',         children: <MarksTab exams={overview.exams || []} /> },
                { key: 'profile', label: 'Profile', children: <ProfileTab student={student} overview={overview} /> },
                (canSms || canMessages) && {
                  key: 'comm', label: 'Communication',
                  children: <CommTab sms={canSms ? overview.sms || [] : null}
                                     onConversations={canMessages ? () => navigate('/messages/conversations') : null} />,
                },
                { key: 'history', label: 'Academic History', children: <HistoryTab years={overview.years || []} history={history} /> },
              ].filter(Boolean)}
            />
          </Spin>
        )}
      </Card>

      {/* Hidden print area for receipt reprints — visible only on print */}
      <ReceiptPrintArea receipt={printReceipt} branding={branding} />
      <style>{`
        .receipt-print-area { display: none; }
        @media print {
          body * { visibility: hidden; }
          .receipt-print-area { display: block; }
          .receipt-print-area, .receipt-print-area * { visibility: visible; }
          .receipt-print-area { position: absolute; left: 0; top: 0; width: 100%; }
        }
      `}</style>
    </>
  )
}

function StatusTag({ status }) {
  const color = { Active: 'green', Y: 'green', Blocked: 'red', Inactive: 'default', TC: 'purple' }[status] || 'default'
  return <Tag color={color}>{status === 'Y' ? 'Active' : status === 'TC' ? 'TC Issued' : (status || '—')}</Tag>
}

// ── Fees ───────────────────────────────────────────────────────────────
function FeesTab({ rows, receipts, onPrint }) {
  const feeCols = [
    { title: 'Category',      dataIndex: 'category' },
    { title: 'Academic Year', dataIndex: 'academicYear' },
    { title: 'Class',         dataIndex: 'className' },
    { title: 'Fee (₹)',       dataIndex: 'structure',   align: 'right', render: money },
    { title: 'Concession (₹)', dataIndex: 'concession', align: 'right', render: (v) => (v > 0 ? money(v) : '—') },
    { title: 'Paid (₹)',      dataIndex: 'paid',        align: 'right', render: money },
    { title: 'Outstanding (₹)', dataIndex: 'outstanding', align: 'right',
      render: (v) => <Text strong type={v > 0 ? 'danger' : 'success'}>{money(v)}</Text> },
  ]
  const lineCols = [
    { title: 'Fee Type',  render: (_, li) => li.feeTypeName || '—' },
    { title: 'Term / Period', render: (_, li) => li.termName || li.periodLabel || '—' },
    { title: 'Fee (₹)',        dataIndex: 'structureAmount',  align: 'right', render: money },
    { title: 'Concession (₹)', dataIndex: 'concessionAmount', align: 'right', render: (v) => (v > 0 ? money(v) : '—') },
    { title: 'Paid (₹)',       dataIndex: 'paidAmount',       align: 'right', render: money },
    { title: 'Outstanding (₹)', dataIndex: 'outstanding',     align: 'right', render: money },
  ]
  const receiptCols = [
    { title: 'Receipt No', dataIndex: 'receiptNo' },
    { title: 'Date',       dataIndex: 'paymentDate', render: (d) => dayjs(d).format('DD-MM-YYYY') },
    { title: 'Year',       dataIndex: 'academicYear' },
    { title: 'Mode',       dataIndex: 'paymentMode', render: (v) => v || '—' },
    { title: 'Amount (₹)', dataIndex: 'totalAmount', align: 'right', render: money },
    { title: 'Status',     dataIndex: 'status', render: (s) => <Tag color={s === 'Cancelled' ? 'red' : 'green'}>{s}</Tag> },
    { title: 'Collected by', dataIndex: 'createdBy', render: (v) => v || '—' },
    { title: '', key: 'print', width: 90,
      render: (_, r) => <Button size="small" icon={<PrinterOutlined />} onClick={() => onPrint(r.receiptId)}>Print</Button> },
  ]
  const activeTotal = receipts.filter((r) => r.status !== 'Cancelled').reduce((a, r) => a + r.totalAmount, 0)

  return (
    <>
      <Table size="small" rowKey="key" columns={feeCols} dataSource={rows} pagination={false}
        locale={{ emptyText: 'No fee structure found for this student.' }}
        expandable={{
          expandedRowRender: (r) => (
            <Table size="small" rowKey={(li, i) => i} columns={lineCols} dataSource={r.lineItems} pagination={false} />
          ),
        }} />
      <div style={{ margin: '20px 0 8px', display: 'flex', justifyContent: 'space-between' }}>
        <Text strong>Receipts (all years)</Text>
        <Text type="secondary">{receipts.length} receipt(s) · collected ₹{money(activeTotal)}</Text>
      </div>
      <Table size="small" rowKey="receiptId" columns={receiptCols} dataSource={receipts}
        pagination={{ pageSize: 10, hideOnSinglePage: true }} />
    </>
  )
}

// ── Attendance ─────────────────────────────────────────────────────────
function AttendanceTab({ overview }) {
  const rows = overview.attendance || []
  if (!rows.length) return <Empty description={`No attendance marked for ${overview.academicYear || 'this year'}.`} />
  const total = rows.reduce((a, r) => ({
    workingDays: a.workingDays + r.workingDays, present: a.present + r.present, absent: a.absent + r.absent,
    late: a.late + r.late, halfDay: a.halfDay + r.halfDay,
  }), { workingDays: 0, present: 0, absent: 0, late: 0, halfDay: 0 })
  const data = [...rows.map((r) => ({ ...r, key: `${r.year}-${r.month}` })), { ...total, key: 'total', isTotal: true }]

  const cols = [
    { title: 'Month', render: (_, r) => (r.isTotal ? <b>Total</b> : `${MONTHS[r.month - 1]} ${r.year}`) },
    { title: 'Working Days', dataIndex: 'workingDays', align: 'right' },
    { title: 'Present',  dataIndex: 'present', align: 'right' },
    { title: 'Absent',   dataIndex: 'absent',  align: 'right', render: (v) => (v > 0 ? <Text type="danger">{v}</Text> : v) },
    { title: 'Late',     dataIndex: 'late',    align: 'right' },
    { title: 'Half Day', dataIndex: 'halfDay', align: 'right' },
    { title: 'Attendance %', key: 'pct', width: 200,
      render: (_, r) => {
        const p = pct(presentDays(r), r.workingDays)
        return p == null ? '—' : <Progress percent={p} size="small" status={p < 75 ? 'exception' : 'normal'} format={(v) => `${v}%`} />
      } },
  ]
  return (
    <Row gutter={[16, 16]}>
      <Col xs={24} lg={16}>
        <Table size="small" columns={cols} dataSource={data} pagination={false}
          rowClassName={(r) => (r.isTotal ? 'ant-table-row-selected' : '')} />
        <Text type="secondary" style={{ fontSize: 12 }}>Late counts as present; Half Day counts as half a day. Holidays are excluded.</Text>
      </Col>
      <Col xs={24} lg={8}>
        <Card size="small" title="Recent absences">
          {(overview.recentAbsences || []).length === 0
            ? <Text type="secondary">None</Text>
            : (overview.recentAbsences || []).map((a, i) => (
              <div key={i} style={{ padding: '3px 0', borderBottom: '1px solid #f0f0f0' }}>
                {dayjs(a.attendanceDate).format('ddd, DD MMM YYYY')}{' '}
                <Tag color={a.status === 'Absent' ? 'red' : 'blue'}>{a.status === 'HalfDay' ? 'Half Day' : a.status}</Tag>
                {a.remarks && <Text type="secondary">{a.remarks}</Text>}
              </div>
            ))}
        </Card>
      </Col>
    </Row>
  )
}

// ── Marks ──────────────────────────────────────────────────────────────
function MarksTab({ exams }) {
  if (!exams.length) return <Empty description="No marks entered for this year." />
  const cols = [
    { title: 'Exam',  dataIndex: 'examTypeName' },
    { title: 'Total', render: (_, r) => `${Number(r.total)} / ${Number(r.maxTotal)}`, align: 'right' },
    { title: '%', key: 'pct', width: 220,
      render: (_, r) => {
        const p = pct(r.total, r.maxTotal)
        return p == null ? '—' : <Progress percent={p} size="small" format={(v) => `${v}%`} />
      } },
    { title: 'Rank in section', render: (_, r) => <b>{r.rank} <Text type="secondary">/ {r.rankOutOf}</Text></b>, align: 'center' },
    { title: 'Subjects', dataIndex: 'subjectCount', align: 'right' },
    { title: 'Absent in', dataIndex: 'absentSubjects', align: 'right',
      render: (v) => (v > 0 ? <Tag color="orange">{v} subject(s)</Tag> : '—') },
  ]
  return (
    <>
      <Table size="small" rowKey="examTypeId" columns={cols} dataSource={exams} pagination={false} />
      <Text type="secondary" style={{ fontSize: 12 }}>
        Totals include the activity component; grade-only subjects are excluded (same as the marks card).
        Full report: Exams → Marks Card.
      </Text>
    </>
  )
}

// ── Profile ────────────────────────────────────────────────────────────
function ProfileTab({ student: s, overview }) {
  const d = (v) => (v ? dayjs(v).format('DD-MM-YYYY') : '—')
  const address = [s.doorNo, s.addressArea, s.addressCity, s.addressState].filter(Boolean).join(', ')
  return (
    <Descriptions size="small" bordered column={{ xs: 1, md: 2, xl: 3 }}>
      <Descriptions.Item label="Gender">{s.gender || '—'}</Descriptions.Item>
      <Descriptions.Item label="Date of Birth">{d(s.dateOfBirth)}</Descriptions.Item>
      <Descriptions.Item label="Blood Group">{s.bloodGroup || '—'}</Descriptions.Item>
      <Descriptions.Item label="Admission Date">{d(s.admissionDate)}</Descriptions.Item>
      <Descriptions.Item label="Join Type">{s.joinType || '—'}</Descriptions.Item>
      <Descriptions.Item label="Joining Class">{s.joiningClass || '—'}</Descriptions.Item>
      <Descriptions.Item label="Father">{s.fatherName || '—'}{s.fatherOccupation ? ` (${s.fatherOccupation})` : ''}</Descriptions.Item>
      <Descriptions.Item label="Father Mobile">{s.fatherMobile || '—'}</Descriptions.Item>
      <Descriptions.Item label="Mother Mobile">{s.motherMobile || '—'}</Descriptions.Item>
      <Descriptions.Item label="Mother">{s.motherName || '—'}{s.motherOccupation ? ` (${s.motherOccupation})` : ''}</Descriptions.Item>
      <Descriptions.Item label="Email">{s.email || '—'}</Descriptions.Item>
      <Descriptions.Item label="Aadhar No">{s.aadharNo || '—'}</Descriptions.Item>
      <Descriptions.Item label="Religion / Caste">{[s.religion, s.caste].filter(Boolean).join(' / ') || '—'}</Descriptions.Item>
      <Descriptions.Item label="Mother Tongue">{s.motherTongue || '—'}</Descriptions.Item>
      <Descriptions.Item label="Languages">{[s.firstLanguage, s.secondLanguage, s.thirdLanguage].filter(Boolean).join(', ') || '—'}</Descriptions.Item>
      <Descriptions.Item label="Transport">
        {s.transportType || '—'}{overview.routeName ? ` · ${overview.routeName}` : ''}{overview.busName ? ` · Bus ${overview.busName}` : ''}
      </Descriptions.Item>
      <Descriptions.Item label="Hostel">{s.hostelName || '—'}</Descriptions.Item>
      <Descriptions.Item label="UDISE No">{s.udiseNo || '—'}</Descriptions.Item>
      <Descriptions.Item label="Address" span={3}>{address || s.permanentAddress || '—'}</Descriptions.Item>
    </Descriptions>
  )
}

// ── Communication ──────────────────────────────────────────────────────
function CommTab({ sms, onConversations }) {
  return (
    <>
      {onConversations && (
        <Alert type="info" showIcon style={{ marginBottom: 12 }}
          message="Parent ↔ teacher messages"
          description="Open Conversations and filter by this student to read their chats."
          action={<Button size="small" icon={<CommentOutlined />} onClick={onConversations}>Conversations</Button>} />
      )}
      {sms && (
        <Table size="small" rowKey={(r, i) => i} dataSource={sms} pagination={{ pageSize: 10, hideOnSinglePage: true }}
          locale={{ emptyText: 'No SMS sent to this student\'s parents.' }}
          columns={[
            { title: 'Sent', dataIndex: 'sentAt', width: 150, render: (v) => dayjs(v).format('DD-MM-YYYY HH:mm') },
            { title: 'Type', dataIndex: 'smsType', width: 90 },
            { title: 'Mobile', dataIndex: 'mobile', width: 110 },
            { title: 'Message', dataIndex: 'message' },
            { title: 'Status', dataIndex: 'status', width: 80, render: (s) => <Tag color={s === 'Sent' ? 'green' : 'red'}>{s}</Tag> },
            { title: 'By', dataIndex: 'sentBy', width: 120 },
          ]} />
      )}
    </>
  )
}

// ── Academic history + change log ──────────────────────────────────────
function HistoryTab({ years, history }) {
  return (
    <>
      <Table size="small" rowKey="studentId" dataSource={years} pagination={false}
        columns={[
          { title: 'Academic Year', dataIndex: 'academicYear' },
          { title: 'Class', render: (_, y) => `${y.className || '—'}${y.sectionName ? ` - ${y.sectionName}` : ''}` },
          { title: 'Roll No', dataIndex: 'rollNo', render: (v) => v || '—' },
          { title: 'Status', render: (_, y) => <Space><StatusTag status={y.status} />{y.isDetained && <Tag color="volcano">Detained</Tag>}</Space> },
        ]} />
      <div style={{ margin: '20px 0 8px' }}><Text strong>Change history (selected year)</Text></div>
      {history == null
        ? <Spin size="small" />
        : <Table size="small" rowKey={(r, i) => i} dataSource={history} pagination={{ pageSize: 10, hideOnSinglePage: true }}
            locale={{ emptyText: 'No changes recorded.' }}
            columns={[
              { title: 'Saved at (UTC)', dataIndex: 'validFrom', width: 150, render: (v) => dayjs(v).format('DD-MM-YYYY HH:mm') },
              { title: 'By', render: (_, r) => r.updatedBy || r.createdBy || '—' },
              { title: 'Name', dataIndex: 'studentName' },
              { title: 'Class', render: (_, r) => `${r.className || '—'}${r.sectionName ? ` - ${r.sectionName}` : ''}` },
              { title: 'Status', dataIndex: 'status' },
              { title: 'Father Mobile', dataIndex: 'fatherMobile' },
            ]} />}
    </>
  )
}
