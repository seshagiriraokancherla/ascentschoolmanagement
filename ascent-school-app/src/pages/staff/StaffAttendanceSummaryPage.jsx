import { useState } from 'react'
import {
  Select, Button, Table, Tag, Typography, Row, Col,
  Statistic, Card, App as AntApp,
} from 'antd'
import { FilePdfOutlined, FileTextOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportCsv } from '../reports/reportUtils'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'

const { Text } = Typography

const MONTHS = [
  'January','February','March','April','May','June',
  'July','August','September','October','November','December',
].map((label, i) => ({ label, value: i + 1 }))

const currentYear = dayjs().year()
const YEARS = Array.from({ length: 5 }, (_, i) => ({
  label: String(currentYear - i), value: currentYear - i,
}))

export default function StaffAttendanceSummaryPage() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [month, setMonth] = useState(dayjs().month() + 1)
  const [year,  setYear]  = useState(currentYear)
  const [rows,  setRows]  = useState(null)
  const [loading, setLoading] = useState(false)

  const handleLoad = async () => {
    setLoading(true)
    setRows(null)
    try {
      const r = await api.get(`/school/staff/attendance/summary?month=${month}&year=${year}`)
      setRows(r.data?.data || [])
    } catch (e) { message.error(apiError(e, 'Failed to load summary.')) }
    finally { setLoading(false) }
  }

  // ── Totals ─────────────────────────────────────────────────────────────────
  const totals = rows
    ? rows.reduce((acc, r) => ({
        present:  acc.present  + r.present,
        absent:   acc.absent   + r.absent,
        late:     acc.late     + r.late,
        halfDay:  acc.halfDay  + r.halfDay,
        onLeave:  acc.onLeave  + r.onLeave,
      }), { present: 0, absent: 0, late: 0, halfDay: 0, onLeave: 0 })
    : null

  const monthLabel = MONTHS.find(m => m.value === month)?.label

  // ── Export PDF ─────────────────────────────────────────────────────────────
  const handlePdf = () => {
    if (!rows?.length) { message.warning('No data to export.'); return }
    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' })
    const pw  = doc.internal.pageSize.getWidth()

    let y = 14
    doc.setFontSize(13).setFont(undefined, 'bold')
    doc.text(schoolName || 'School', pw / 2, y, { align: 'center' }); y += 6
    doc.setFontSize(10).setFont(undefined, 'normal')
    doc.text(`Staff Attendance Summary — ${monthLabel} ${year}`, pw / 2, y, { align: 'center' }); y += 8

    autoTable(doc, {
      startY: y,
      head: [['#', 'Emp Code', 'Name', 'Designation', 'Present', 'Absent', 'Late', 'Half Day', 'On Leave', 'Total Marked']],
      body: rows.map((r, i) => [
        i + 1, r.employeeCode || '—', r.staffName, r.designation || '—',
        r.present, r.absent, r.late, r.halfDay, r.onLeave, r.totalMarked,
      ]),
      styles:       { fontSize: 8, cellPadding: 2 },
      headStyles:   { fillColor: [41, 128, 185], textColor: 255, fontStyle: 'bold' },
      columnStyles: { 0: { cellWidth: 8 }, 1: { cellWidth: 20 }, 2: { cellWidth: 48 } },
      alternateRowStyles: { fillColor: [245, 245, 245] },
    })

    // Totals footer
    const finalY = doc.lastAutoTable.finalY + 8
    doc.setFontSize(9).setFont(undefined, 'bold')
    doc.text(
      `Totals — Present: ${totals.present}  Absent: ${totals.absent}  Late: ${totals.late}  Half Day: ${totals.halfDay}  On Leave: ${totals.onLeave}`,
      14, finalY
    )

    doc.save(`staff_attendance_${monthLabel}_${year}.pdf`)
  }

  // ── Export CSV ─────────────────────────────────────────────────────────────
  const handleCsv = () => {
    if (!rows?.length) { message.warning('No data to export.'); return }
    exportCsv({
      columns: ['Emp Code', 'Name', 'Designation', 'Present', 'Absent', 'Late', 'Half Day', 'On Leave', 'Total Marked'],
      rows:    rows.map(r => [
        r.employeeCode || '', r.staffName, r.designation || '',
        r.present, r.absent, r.late, r.halfDay, r.onLeave, r.totalMarked,
      ]),
      fileName: `staff_attendance_${monthLabel}_${year}.csv`,
    })
  }

  // ── Table columns ──────────────────────────────────────────────────────────
  const columns = [
    { title: '#', width: 45, align: 'center', render: (_, __, i) => i + 1 },
    { title: 'Emp Code',    dataIndex: 'employeeCode', width: 100,
      render: v => v || <Text type="secondary">—</Text> },
    { title: 'Name',        dataIndex: 'staffName',    width: 200 },
    { title: 'Designation', dataIndex: 'designation',  width: 130,
      render: v => v ? <Tag color="blue" style={{ fontSize: 11 }}>{v}</Tag> : null },
    { title: 'Present',  dataIndex: 'present',     width: 80, align: 'center',
      render: v => <Text style={{ color: '#52c41a', fontWeight: 600 }}>{v}</Text> },
    { title: 'Absent',   dataIndex: 'absent',      width: 80, align: 'center',
      render: v => v > 0 ? <Text style={{ color: '#ff4d4f', fontWeight: 600 }}>{v}</Text> : <Text type="secondary">0</Text> },
    { title: 'Late',     dataIndex: 'late',        width: 70, align: 'center',
      render: v => v > 0 ? <Text style={{ color: '#fa8c16', fontWeight: 600 }}>{v}</Text> : <Text type="secondary">0</Text> },
    { title: 'Half Day', dataIndex: 'halfDay',     width: 80, align: 'center',
      render: v => v > 0 ? <Text style={{ color: '#1677ff', fontWeight: 600 }}>{v}</Text> : <Text type="secondary">0</Text> },
    { title: 'On Leave', dataIndex: 'onLeave',     width: 80, align: 'center',
      render: v => v > 0 ? <Text style={{ color: '#722ed1', fontWeight: 600 }}>{v}</Text> : <Text type="secondary">0</Text> },
    { title: 'Total Marked', dataIndex: 'totalMarked', width: 100, align: 'center',
      render: v => <Text strong>{v}</Text> },
  ]

  return (
    <div>
      {/* ── Filters ─────────────────────────────────────────────────────────── */}
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Select
            style={{ width: 130 }}
            value={month}
            onChange={setMonth}
            options={MONTHS}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 90 }}
            value={year}
            onChange={setYear}
            options={YEARS}
          />
        </Col>
        <Col>
          <Button type="primary" onClick={handleLoad} loading={loading}>
            Load
          </Button>
        </Col>
        {rows?.length > 0 && (
          <Col style={{ marginLeft: 'auto' }}>
            <Button icon={<FilePdfOutlined />} onClick={handlePdf} style={{ marginRight: 8 }}>PDF</Button>
            <Button icon={<FileTextOutlined />} onClick={handleCsv}>CSV</Button>
          </Col>
        )}
      </Row>

      {/* ── Summary cards ────────────────────────────────────────────────────── */}
      {totals && (
        <Row gutter={12} style={{ marginBottom: 16 }}>
          {[
            { label: 'Present',  value: totals.present,  color: '#52c41a' },
            { label: 'Absent',   value: totals.absent,   color: '#ff4d4f' },
            { label: 'Late',     value: totals.late,     color: '#fa8c16' },
            { label: 'Half Day', value: totals.halfDay,  color: '#1677ff' },
            { label: 'On Leave', value: totals.onLeave,  color: '#722ed1' },
          ].map(({ label, value, color }) => (
            <Col key={label}>
              <Card size="small" style={{ minWidth: 100, textAlign: 'center' }}>
                <Statistic
                  title={label}
                  value={value}
                  valueStyle={{ color, fontSize: 20 }}
                />
              </Card>
            </Col>
          ))}
        </Row>
      )}

      {/* ── Table ────────────────────────────────────────────────────────────── */}
      {rows !== null && rows.length === 0 && !loading && (
        <Text type="secondary">No staff attendance data found for {monthLabel} {year}.</Text>
      )}

      {rows?.length > 0 && (
        <Table
          rowKey="staffId"
          dataSource={rows}
          columns={columns}
          size="small"
          pagination={false}
          scroll={{ x: 'max-content' }}
          summary={() => (
            <Table.Summary.Row style={{ background: '#fafafa', fontWeight: 700 }}>
              <Table.Summary.Cell colSpan={4} align="right">
                <Text strong>Total</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="center">
                <Text style={{ color: '#52c41a', fontWeight: 700 }}>{totals.present}</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="center">
                <Text style={{ color: '#ff4d4f', fontWeight: 700 }}>{totals.absent}</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="center">
                <Text style={{ color: '#fa8c16', fontWeight: 700 }}>{totals.late}</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="center">
                <Text style={{ color: '#1677ff', fontWeight: 700 }}>{totals.halfDay}</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="center">
                <Text style={{ color: '#722ed1', fontWeight: 700 }}>{totals.onLeave}</Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell align="center">
                <Text strong>{rows.reduce((s, r) => s + r.totalMarked, 0)}</Text>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          )}
        />
      )}
    </div>
  )
}
