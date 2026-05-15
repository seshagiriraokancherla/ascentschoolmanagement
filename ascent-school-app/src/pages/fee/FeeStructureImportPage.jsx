import { useState } from 'react'
import {
  Card, Button, Table, Typography, Upload, App as AntApp,
  Alert, Progress, Space, Tag, Divider,
} from 'antd'
import {
  UploadOutlined, DownloadOutlined, CheckCircleOutlined, CloseCircleOutlined,
} from '@ant-design/icons'
import Papa from 'papaparse'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography

// ── Column reference ──────────────────────────────────────────────────────────
const COLUMNS_META = [
  { key: 'AcademicYear',  required: true,  note: 'Must match master data, e.g. 2024-25' },
  { key: 'ClassName',     required: true,  note: 'Must match master data, e.g. Class 6' },
  { key: 'FeeCategory',   required: true,  note: 'Must match master data, e.g. General' },
  { key: 'FeeType',       required: true,  note: 'Must match master data, e.g. Tuition Fee' },
  { key: 'PaymentType',   required: true,  note: 'Term or Monthly' },
  { key: 'AdmissionType', required: false, note: 'New, Old, or blank (applies to all)' },
  { key: 'Term',          required: false, note: 'For PaymentType=Term: must match master terms, e.g. Term 1. Leave blank for Monthly.' },
  { key: 'FeePeriod',     required: false, note: 'For PaymentType=Monthly: must match Fee Periods master, e.g. April 2024. Leave blank for Term.' },
  { key: 'Amount',        required: true,  note: 'Numeric, e.g. 1500 or 1500.50' },
]

// Required columns for CSV validation (Term/FeePeriod validated contextually by server)
const REQUIRED_HEADERS = ['AcademicYear', 'ClassName', 'FeeCategory', 'FeeType', 'PaymentType', 'Amount']
const CSV_HEADERS      = COLUMNS_META.map((c) => c.key)

const EXAMPLE_ROWS = [
  { AcademicYear: '2024-25', ClassName: 'Class 6', FeeCategory: 'General', FeeType: 'Tuition Fee', PaymentType: 'Monthly', AdmissionType: 'Old', Term: '',       FeePeriod: 'April 2024',   Amount: '1500' },
  { AcademicYear: '2024-25', ClassName: 'Class 6', FeeCategory: 'General', FeeType: 'Tuition Fee', PaymentType: 'Monthly', AdmissionType: 'Old', Term: '',       FeePeriod: 'May 2024',     Amount: '1500' },
  { AcademicYear: '2024-25', ClassName: 'Class 6', FeeCategory: 'General', FeeType: 'Exam Fee',    PaymentType: 'Term',    AdmissionType: '',    Term: 'Term 1', FeePeriod: '',             Amount: '500'  },
  { AcademicYear: '2024-25', ClassName: 'Class 7', FeeCategory: 'General', FeeType: 'Tuition Fee', PaymentType: 'Monthly', AdmissionType: 'New', Term: '',       FeePeriod: 'April 2024',   Amount: '750'  },
]

const exampleColumns = COLUMNS_META.map((c) => ({
  title: (
    <span>
      {c.key}
      {c.required && <span style={{ color: 'red' }}> *</span>}
    </span>
  ),
  dataIndex: c.key,
  key:       c.key,
  width:     140,
  render:    (v) => <Text style={{ fontSize: 12 }}>{v}</Text>,
}))

const errorColumns = [
  { title: 'Row',        dataIndex: 'row',        key: 'row',        width: 70 },
  { title: 'Entry',      dataIndex: 'identifier', key: 'identifier', width: 260 },
  { title: 'Reason',     dataIndex: 'reason',     key: 'reason' },
]

function downloadTemplate() {
  const header = CSV_HEADERS.join(',')
  const rows   = EXAMPLE_ROWS.map((r) => CSV_HEADERS.map((h) => r[h] ?? '').join(',')).join('\n')
  const blob   = new Blob([header + '\n' + rows], { type: 'text/csv' })
  const url    = URL.createObjectURL(blob)
  const a      = document.createElement('a')
  a.href       = url
  a.download   = 'fee_structure_import_template.csv'
  a.click()
  URL.revokeObjectURL(url)
}

function downloadErrors(errors) {
  const header = 'Row,Entry,Reason'
  const rows   = errors.map((e) => `${e.row},"${e.identifier || ''}","${e.reason}"`).join('\n')
  const blob   = new Blob([header + '\n' + rows], { type: 'text/csv' })
  const url    = URL.createObjectURL(blob)
  const a      = document.createElement('a')
  a.href       = url
  a.download   = 'fee_structure_import_errors.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export default function FeeStructureImportPage() {
  const { message } = AntApp.useApp()

  const [preview,  setPreview]  = useState(null)
  const [fileName, setFileName] = useState(null)
  const [result,   setResult]   = useState(null)
  const [loading,  setLoading]  = useState(false)

  const handleFile = (file) => {
    setResult(null)
    Papa.parse(file, {
      header:         true,
      skipEmptyLines: true,
      complete: ({ data, meta }) => {
        const missing = REQUIRED_HEADERS.filter((c) => !meta.fields.includes(c))
        if (missing.length) {
          message.error(`CSV is missing required columns: ${missing.join(', ')}`)
          return
        }
        if (data.length > 500) {
          message.error('File has more than 500 rows. Please split and upload in batches.')
          return
        }
        setPreview(data)
        setFileName(file.name)
      },
      error: () => message.error('Failed to parse CSV file.'),
    })
    return false
  }

  const handleImport = async () => {
    if (!preview || preview.length === 0) return
    setLoading(true)
    try {
      const r = await api.post('/school/fees/structure/bulk', { rows: preview })
      setResult(r.data.data)
    } catch (e) {
      message.error(e.message || 'Import failed.')
    } finally {
      setLoading(false)
    }
  }

  const reset = () => { setPreview(null); setFileName(null); setResult(null) }

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Bulk Fee Structure Import</Title>

      {/* ── Column reference ─────────────────────────────────────────────────── */}
      <Card title="CSV Column Reference" style={{ marginBottom: 16 }}>
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Each row sets the fee amount for one Class + Category + Fee Type + Term/Period + AdmissionType combination. Use PaymentType=Term with a Term value, or PaymentType=Monthly with a FeePeriod value. Uploading a row that already exists will overwrite the existing amount."
        />

        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
          <thead>
            <tr style={{ background: '#fafafa' }}>
              <th style={th}>Column Name</th>
              <th style={th}>Required</th>
              <th style={th}>Notes</th>
            </tr>
          </thead>
          <tbody>
            {COLUMNS_META.map((c) => (
              <tr key={c.key}>
                <td style={td}><code>{c.key}</code></td>
                <td style={td}>{c.required ? <Tag color="red">Required</Tag> : <Tag>Optional</Tag>}</td>
                <td style={td}>{c.note}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <Divider style={{ margin: '12px 0' }} />
        <Text type="secondary" style={{ fontSize: 12 }}>Example data (template includes these rows):</Text>
        <div style={{ overflowX: 'auto', marginTop: 8 }}>
          <Table
            dataSource={EXAMPLE_ROWS}
            columns={exampleColumns}
            rowKey={(_, i) => i}
            pagination={false}
            size="small"
            scroll={{ x: 'max-content' }}
          />
        </div>

        <div style={{ marginTop: 12 }}>
          <Button icon={<DownloadOutlined />} onClick={downloadTemplate}>
            Download Template CSV
          </Button>
        </div>
      </Card>

      {/* ── Upload ──────────────────────────────────────────────────────────── */}
      <Card title="Upload CSV" style={{ marginBottom: 16 }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <Upload accept=".csv" showUploadList={false} beforeUpload={handleFile}>
            <Button icon={<UploadOutlined />}>Choose CSV file</Button>
          </Upload>

          {fileName && !result && (
            <Text type="secondary">Selected: <strong>{fileName}</strong> — {preview?.length} rows</Text>
          )}

          {preview && !result && (
            <Alert type="info" showIcon
              message={`${preview.length} rows ready to import. Review the preview below, then click Import.`}
            />
          )}

          {preview && !result && (
            <Space>
              <Button type="primary" loading={loading} onClick={handleImport}>
                Import {preview.length} Rows
              </Button>
              <Button onClick={reset}>Clear</Button>
            </Space>
          )}
        </Space>
      </Card>

      {/* ── Preview ─────────────────────────────────────────────────────────── */}
      {preview && !result && (
        <Card title={`Preview — ${preview.length} rows`} style={{ marginBottom: 16 }}>
          <div style={{ overflowX: 'auto' }}>
            <Table
              dataSource={preview.slice(0, 10)}
              columns={CSV_HEADERS.map((h) => ({ title: h, dataIndex: h, key: h, width: 130 }))}
              rowKey={(_, i) => i}
              pagination={false}
              size="small"
              scroll={{ x: 'max-content' }}
              footer={preview.length > 10 ? () => `… and ${preview.length - 10} more rows (not shown)` : undefined}
            />
          </div>
        </Card>
      )}

      {/* ── Result ──────────────────────────────────────────────────────────── */}
      {result && (
        <Card
          title="Import Result"
          extra={<Button onClick={reset}>Import Another File</Button>}
        >
          <Space size="large" style={{ marginBottom: 16 }}>
            <Text><strong>Total:</strong> {result.total}</Text>
            <Text style={{ color: '#52c41a' }}>
              <CheckCircleOutlined /> Imported: {result.imported}
            </Text>
            <Text style={{ color: '#ff4d4f' }}>
              <CloseCircleOutlined /> Failed: {result.failed}
            </Text>
          </Space>

          <Progress
            percent={Math.round((result.imported / result.total) * 100)}
            status={result.failed > 0 ? 'exception' : 'success'}
            style={{ maxWidth: 400 }}
          />

          {result.errors && result.errors.length > 0 && (
            <div style={{ marginTop: 16 }}>
              <Space style={{ marginBottom: 8 }}>
                <Text strong>Failed Rows</Text>
                <Button size="small" icon={<DownloadOutlined />} onClick={() => downloadErrors(result.errors)}>
                  Download Error Report
                </Button>
              </Space>
              <Table
                dataSource={result.errors}
                columns={errorColumns}
                rowKey={(_, i) => i}
                pagination={{ pageSize: 10, size: 'small' }}
                size="small"
              />
            </div>
          )}
        </Card>
      )}
    </div>
  )
}

const th = { padding: '6px 12px', border: '1px solid #f0f0f0', textAlign: 'left', fontWeight: 600 }
const td = { padding: '6px 12px', border: '1px solid #f0f0f0' }
