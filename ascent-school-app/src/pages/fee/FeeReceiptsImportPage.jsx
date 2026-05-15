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

const COLUMNS_META = [
  { key: 'AdmissionNo',     required: true,  note: 'Student admission number' },
  { key: 'AcademicYear',    required: true,  note: 'Must match master data, e.g. 2024-25' },
  { key: 'FeeCategory',     required: true,  note: 'School / Transport / Hostel / Admission / Other' },
  { key: 'FeeTypeName',     required: false, note: 'For School/Admission/Other: must match fee type name' },
  { key: 'RouteName',       required: false, note: 'For Transport rows: must match bus route name' },
  { key: 'HostelName',      required: false, note: 'For Hostel rows: must match hostel name' },
  { key: 'TermName',        required: false, note: 'Term or fee period label, e.g. Term 1 or April 2024' },
  { key: 'PaidAmount',      required: true,  note: 'Numeric, e.g. 1500 or 1500.50' },
  { key: 'PaymentDate',     required: true,  note: 'dd/MM/yyyy or yyyy-MM-dd' },
  { key: 'PaymentMode',     required: true,  note: 'Must match payment mode name, e.g. Cash' },
  { key: 'ChequeNo',        required: false, note: 'Cheque number (for Cheque mode)' },
  { key: 'BankName',        required: false, note: 'Bank name (for Cheque mode)' },
  { key: 'LegacyReceiptNo', required: false, note: 'Original receipt number from legacy system; rows sharing the same value are grouped into one receipt' },
  { key: 'Remarks',         required: false, note: 'Additional notes' },
]

const REQUIRED_HEADERS = ['AdmissionNo', 'AcademicYear', 'FeeCategory', 'PaidAmount', 'PaymentDate', 'PaymentMode']
const CSV_HEADERS      = COLUMNS_META.map((c) => c.key)

const EXAMPLE_ROWS = [
  { AdmissionNo: '2024001', AcademicYear: '2024-25', FeeCategory: 'School',    FeeTypeName: 'Tuition Fee', RouteName: '',       HostelName: '', TermName: 'Term 1', PaidAmount: '3000', PaymentDate: '05/06/2024', PaymentMode: 'Cash',   ChequeNo: '', BankName: '', LegacyReceiptNo: 'R001', Remarks: '' },
  { AdmissionNo: '2024001', AcademicYear: '2024-25', FeeCategory: 'School',    FeeTypeName: 'Exam Fee',    RouteName: '',       HostelName: '', TermName: 'Term 1', PaidAmount: '500',  PaymentDate: '05/06/2024', PaymentMode: 'Cash',   ChequeNo: '', BankName: '', LegacyReceiptNo: 'R001', Remarks: '' },
  { AdmissionNo: '2024002', AcademicYear: '2024-25', FeeCategory: 'Transport', FeeTypeName: '',            RouteName: 'Route A', HostelName: '', TermName: 'Term 1', PaidAmount: '1200', PaymentDate: '10/06/2024', PaymentMode: 'Cheque', ChequeNo: '123456', BankName: 'SBI', LegacyReceiptNo: 'R002', Remarks: '' },
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

const colRefColumns = [
  { title: 'Column',   dataIndex: 'key',      key: 'key',      width: 160, render: (v) => <Text code>{v}</Text> },
  { title: 'Required', dataIndex: 'required', key: 'required', width: 80,  render: (v) => v ? <Tag color="red">Yes</Tag> : <Tag>No</Tag> },
  { title: 'Notes',    dataIndex: 'note',     key: 'note' },
]

function downloadTemplate() {
  const csv = Papa.unparse({ fields: CSV_HEADERS, data: EXAMPLE_ROWS })
  const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  const a   = Object.assign(document.createElement('a'), { href: url, download: 'legacy_receipts_template.csv' })
  a.click(); URL.revokeObjectURL(url)
}

function downloadErrors(errors) {
  const rows = errors.map((e) => ({ Row: e.row, AdmissionNo: e.admissionNo || '', Reason: e.reason }))
  const csv  = Papa.unparse(rows)
  const url  = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  const a    = Object.assign(document.createElement('a'), { href: url, download: 'import_errors.csv' })
  a.click(); URL.revokeObjectURL(url)
}

const previewColumns = CSV_HEADERS.map((h) => ({
  title: h, dataIndex: h, key: h, width: 130,
  ellipsis: true,
  render: (v) => <Text style={{ fontSize: 11 }}>{v}</Text>,
}))

const errorColumns = [
  { title: 'Row',         dataIndex: 'row',         key: 'row',         width: 60 },
  { title: 'AdmissionNo', dataIndex: 'admissionNo',  key: 'admissionNo', width: 120 },
  { title: 'Reason',      dataIndex: 'reason',       key: 'reason' },
]

export default function FeeReceiptsImportPage() {
  const { message }       = AntApp.useApp()
  const [preview,  setPreview]  = useState([])
  const [result,   setResult]   = useState(null)
  const [loading,  setLoading]  = useState(false)
  const [progress, setProgress] = useState(0)

  const handleFile = (file) => {
    Papa.parse(file, {
      header:        true,
      skipEmptyLines: true,
      complete: ({ data, meta }) => {
        const missing = REQUIRED_HEADERS.filter((h) => !meta.fields?.includes(h))
        if (missing.length > 0) {
          message.error(`Missing required columns: ${missing.join(', ')}`)
          return
        }
        setPreview(data.slice(0, 10))
        setResult(null)
        setProgress(0)
        message.success(`Parsed ${data.length} rows. Review the preview below.`)
        // Store full data for upload
        handleFile._fullData = data
      },
      error: () => message.error('Failed to parse CSV file.'),
    })
    return false
  }

  const handleUpload = (file) => {
    Papa.parse(file, {
      header:        true,
      skipEmptyLines: true,
      complete: async ({ data, meta }) => {
        const missing = REQUIRED_HEADERS.filter((h) => !meta.fields?.includes(h))
        if (missing.length > 0) { message.error(`Missing required columns: ${missing.join(', ')}`); return }
        if (data.length === 0)  { message.error('No data rows found.'); return }
        if (data.length > 1000) { message.error('Maximum 1000 rows per upload.'); return }

        setPreview(data.slice(0, 10))
        setResult(null)
        setLoading(true)
        setProgress(10)

        try {
          const rows = data.map((r, i) => ({ ...r, RowNumber: i + 2 }))
          setProgress(30)
          const res = await api.post('/school/fees/receipts/bulk', { Rows: rows })
          setProgress(100)
          setResult(res.data?.data)
        } catch (e) {
          message.error(e.message || 'Upload failed.')
        } finally {
          setLoading(false)
        }
      },
      error: () => message.error('Failed to parse CSV file.'),
    })
    return false
  }

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Legacy Receipt Import</Title>

      <Card title="Column Reference" style={{ marginBottom: 16 }}>
        <Table
          dataSource={COLUMNS_META} columns={colRefColumns} rowKey="key"
          size="small" pagination={false} style={{ marginBottom: 16 }}
        />
        <Button icon={<DownloadOutlined />} onClick={downloadTemplate}>
          Download Template CSV
        </Button>
      </Card>

      <Card title="Example Data" style={{ marginBottom: 16 }}>
        <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
          Rows with the same <Text code>LegacyReceiptNo</Text> for the same student + year are grouped into a single receipt.
        </Text>
        <Table
          dataSource={EXAMPLE_ROWS} columns={exampleColumns} rowKey={(_, i) => i}
          size="small" pagination={false} scroll={{ x: 'max-content' }}
        />
      </Card>

      <Card title="Upload CSV" style={{ marginBottom: 16 }}>
        <Alert
          type="info" showIcon style={{ marginBottom: 16 }}
          message="Each row is one fee line item. Rows sharing the same LegacyReceiptNo (for the same student + year) are combined into one receipt. If LegacyReceiptNo is blank, rows are grouped by AdmissionNo + AcademicYear + PaymentDate + PaymentMode."
        />
        <Space direction="vertical" style={{ width: '100%' }}>
          <Upload
            accept=".csv"
            showUploadList={false}
            beforeUpload={handleUpload}
          >
            <Button icon={<UploadOutlined />} loading={loading} type="primary">
              {loading ? 'Uploading…' : 'Select CSV and Upload'}
            </Button>
          </Upload>

          {loading && (
            <Progress percent={progress} status="active" style={{ maxWidth: 400 }} />
          )}
        </Space>
      </Card>

      {preview.length > 0 && (
        <Card title={`Preview (first ${preview.length} rows)`} style={{ marginBottom: 16 }}>
          <Table
            dataSource={preview} columns={previewColumns} rowKey={(_, i) => i}
            size="small" pagination={false} scroll={{ x: 'max-content' }}
          />
        </Card>
      )}

      {result && (
        <Card title="Import Result">
          <Space size="large" style={{ marginBottom: 16 }}>
            <Tag icon={<CheckCircleOutlined />} color="default">Total: {result.total}</Tag>
            <Tag icon={<CheckCircleOutlined />} color="success">Imported: {result.imported}</Tag>
            {result.failed > 0 && (
              <Tag icon={<CloseCircleOutlined />} color="error">Failed: {result.failed}</Tag>
            )}
          </Space>

          {result.failed === 0 ? (
            <Alert type="success" showIcon message={`All ${result.imported} rows imported successfully.`} />
          ) : (
            <>
              <Alert
                type="warning" showIcon style={{ marginBottom: 12 }}
                message={`${result.imported} rows imported. ${result.failed} rows failed — see errors below.`}
              />
              <div style={{ textAlign: 'right', marginBottom: 8 }}>
                <Button size="small" icon={<DownloadOutlined />} onClick={() => downloadErrors(result.errors)}>
                  Download Errors CSV
                </Button>
              </div>
              <Table
                dataSource={result.errors} columns={errorColumns} rowKey={(_, i) => i}
                size="small" pagination={{ pageSize: 20 }}
              />
            </>
          )}
        </Card>
      )}
    </div>
  )
}
