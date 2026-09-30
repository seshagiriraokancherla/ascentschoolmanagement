import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import Papa from 'papaparse'
import api from '../../api/axiosInstance'
import { API_BASE } from '../../store/brandingStore'

/**
 * Generate and download a PDF with a standard school header + autoTable.
 * @param {object} opts
 * @param {string}   opts.schoolName
 * @param {string}   opts.title        - Report title line
 * @param {string[]} opts.columns      - Column header labels
 * @param {any[][]}  [opts.head]       - Multi-row header (autoTable cell objects with colSpan/rowSpan);
 *                                       used instead of `columns` when given
 * @param {any[][]}  opts.rows         - 2-D array of cell values
 * @param {string}   opts.fileName     - e.g. 'class_students.pdf'
 * @param {object}   [opts.tableOptions] - jspdf-autotable overrides (fontSize, columnStyles…)
 */
export function exportPdf({ schoolName, title, columns, head, rows, fileName, tableOptions = {} }) {
  const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' })

  const pageW = doc.internal.pageSize.getWidth()
  const now   = new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })

  // School name
  doc.setFontSize(14)
  doc.setFont('helvetica', 'bold')
  doc.text(schoolName, pageW / 2, 14, { align: 'center' })

  // Report title
  doc.setFontSize(11)
  doc.setFont('helvetica', 'normal')
  doc.text(title, pageW / 2, 21, { align: 'center' })

  // Date generated (right-aligned)
  doc.setFontSize(8)
  doc.text(`Generated: ${now}`, pageW - 10, 10, { align: 'right' })

  // Divider
  doc.setDrawColor(180)
  doc.line(10, 25, pageW - 10, 25)

  autoTable(doc, {
    head:       head || [columns],
    body:       rows,
    startY:     28,
    styles:     { fontSize: 8, cellPadding: 2 },
    headStyles: { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [245, 248, 255] },
    ...tableOptions,
  })

  doc.save(fileName)
}

/**
 * Generate and download a Study Certificate PDF (portrait A4, formal layout).
 * @param {object} opts
 * @param {string}  opts.schoolName
 * @param {string}  opts.studentName
 * @param {string}  opts.fatherName
 * @param {string}  opts.motherName
 * @param {string}  opts.className
 * @param {string}  opts.sectionName
 * @param {string}  opts.academicYear
 * @param {string}  opts.dateOfBirth   - formatted string e.g. "15 Aug 2010"
 * @param {string}  opts.gender        - "Male" | "Female" | other
 * @param {string}  opts.issuedFor     - purpose e.g. "general"
 * @param {string}  opts.admissionNo
 */
export function generateStudyCertificate({
  schoolName, studentName, fatherName, motherName,
  className, sectionName, academicYear,
  dateOfBirth, gender, issuedFor, admissionNo,
}) {
  const doc  = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
  const pageW = doc.internal.pageSize.getWidth()   // 210
  const mL = 20, mR = 20
  const contentW = pageW - mL - mR
  const now = new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' })

  // ── Border ────────────────────────────────────────────────────────────────
  doc.setDrawColor(22, 119, 255)
  doc.setLineWidth(0.8)
  doc.rect(10, 10, pageW - 20, 277)
  doc.setLineWidth(0.3)
  doc.rect(12, 12, pageW - 24, 273)

  // ── School name ───────────────────────────────────────────────────────────
  doc.setFont('helvetica', 'bold')
  doc.setFontSize(18)
  doc.setTextColor(22, 119, 255)
  doc.text(schoolName, pageW / 2, 30, { align: 'center' })

  // ── Divider ───────────────────────────────────────────────────────────────
  doc.setDrawColor(22, 119, 255)
  doc.setLineWidth(0.5)
  doc.line(mL, 35, pageW - mR, 35)

  // ── Certificate title ─────────────────────────────────────────────────────
  doc.setFont('helvetica', 'bold')
  doc.setFontSize(14)
  doc.setTextColor(40, 40, 40)
  doc.text('STUDY CERTIFICATE', pageW / 2, 47, { align: 'center' })

  // Underline the title
  const titleW = doc.getTextWidth('STUDY CERTIFICATE')
  doc.setLineWidth(0.4)
  doc.line((pageW - titleW) / 2, 49, (pageW + titleW) / 2, 49)

  // ── Salutation ────────────────────────────────────────────────────────────
  doc.setFont('helvetica', 'italic')
  doc.setFontSize(10)
  doc.setTextColor(80, 80, 80)
  doc.text('To Whom It May Concern', mL, 62)

  // ── Body text ─────────────────────────────────────────────────────────────
  const sonDaughter = gender?.toLowerCase() === 'female' ? 'daughter' : 'son'
  const hisHer      = gender?.toLowerCase() === 'female' ? 'Her' : 'His'
  const parentLine  = [fatherName, motherName].filter(Boolean).join(' and ')

  const body = [
    `This is to certify that ${studentName} (Admission No: ${admissionNo}), ${sonDaughter} of`,
    `${parentLine || '—'}, is a bonafide student of this institution. ${hisHer}/Her`,
    `particulars are as follows:`,
  ].join(' ')

  doc.setFont('helvetica', 'normal')
  doc.setFontSize(11)
  doc.setTextColor(30, 30, 30)
  const bodyLines = doc.splitTextToSize(body, contentW)
  doc.text(bodyLines, mL, 74)

  // ── Details table ─────────────────────────────────────────────────────────
  const details = [
    ['Class',           `${className}${sectionName && sectionName !== '-' ? ' — ' + sectionName : ''}`],
    ['Academic Year',   academicYear || '—'],
    ['Date of Birth',   dateOfBirth  || '—'],
  ]

  let y = 95
  doc.setFontSize(11)
  details.forEach(([label, value]) => {
    doc.setFont('helvetica', 'bold')
    doc.text(`${label}`, mL + 5, y)
    doc.setFont('helvetica', 'normal')
    doc.text(`:   ${value}`, mL + 45, y)
    y += 10
  })

  // ── Closing ───────────────────────────────────────────────────────────────
  y += 6
  const closing = `This certificate is issued on request for ${issuedFor || 'general'} purposes.`
  doc.setFont('helvetica', 'normal')
  doc.setFontSize(11)
  doc.text(doc.splitTextToSize(closing, contentW), mL, y)

  // ── Date + Signature ──────────────────────────────────────────────────────
  y += 28
  doc.setFontSize(10)
  doc.text(`Date: ${now}`, mL, y)
  doc.text('Principal / Head of Institution', pageW - mR, y, { align: 'right' })

  // Signature line
  doc.setDrawColor(100)
  doc.setLineWidth(0.3)
  doc.line(pageW - mR - 60, y + 1, pageW - mR, y + 1)

  doc.save(`study_certificate_${admissionNo}.pdf`)
}

/**
 * Generate Exam Toppers PDF — landscape A4, one autoTable per class.
 * @param {object}   opts
 * @param {string}   opts.schoolName
 * @param {string}   opts.examName
 * @param {string}   opts.academicYear
 * @param {string[]} opts.subjects      - ordered subject name list
 * @param {Array}    opts.classes       - [{ className, sectionName, rows: [{rank, admissionNo, studentName, subjectMarks, totalObtained, totalMax, percentage}] }]
 */
export function exportToppersPdf({ schoolName, examName, academicYear, subjects, classes, subtitle }) {
  const doc  = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' })
  const pageW = doc.internal.pageSize.getWidth()
  const now   = new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })

  // Page header (drawn once; subsequent pages via didDrawPage hook)
  const drawHeader = () => {
    doc.setFontSize(14)
    doc.setFont('helvetica', 'bold')
    doc.setTextColor(30, 30, 30)
    doc.text(schoolName, pageW / 2, 14, { align: 'center' })
    doc.setFontSize(10)
    doc.setFont('helvetica', 'normal')
    doc.text(subtitle || `Exam Toppers — ${examName} (${academicYear})`, pageW / 2, 21, { align: 'center' })
    doc.setFontSize(8)
    doc.text(`Generated: ${now}`, pageW - 10, 10, { align: 'right' })
    doc.setDrawColor(180)
    doc.line(10, 25, pageW - 10, 25)
  }

  drawHeader()
  let startY = 28
  let firstClass = true

  classes.forEach(cls => {
    if (!cls.rows.length) return
    if (!firstClass) {
      // Start new page for each class group
      doc.addPage()
      drawHeader()
      startY = 28
    }
    firstClass = false

    // Class section label
    doc.setFontSize(10)
    doc.setFont('helvetica', 'bold')
    doc.setTextColor(22, 119, 255)
    doc.text(`${cls.className} — ${cls.sectionName}`, 10, startY + 4)
    doc.setTextColor(30, 30, 30)

    const columns = ['Rank', 'Adm No', 'Name', ...subjects, 'Total', '%']
    const rows = cls.rows.map(r => [
      r.rank,
      r.admissionNo,
      r.studentName,
      ...subjects.map(s => r.subjectMarks?.[s] ?? '—'),
      `${r.totalObtained}/${r.totalMax}`,
      `${r.percentage}%`,
    ])

    autoTable(doc, {
      head:       [columns],
      body:       rows,
      startY:     startY + 7,
      styles:     { fontSize: 7, cellPadding: 1.5 },
      headStyles: { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold' },
      alternateRowStyles: { fillColor: [245, 248, 255] },
      columnStyles: {
        0: { cellWidth: 12, halign: 'center' },
        1: { cellWidth: 18 },
        2: { cellWidth: 40 },
        [3 + subjects.length]:     { cellWidth: 22, halign: 'center' },
        [3 + subjects.length + 1]: { cellWidth: 14, halign: 'center' },
      },
      didDrawPage: (data) => {
        if (data.pageNumber > 1) { drawHeader(); }
      },
    })

    startY = doc.lastAutoTable.finalY + 6
  })

  doc.save('exam_toppers.pdf')
}

// Draws a bounded schedule table without autoTable (prevents overflow into adjacent ticket slots).
function _drawTicketTable(doc, x, y, w, maxH, headers, rows, colWidths, fontSize, padding) {
  const lineH = fontSize / 2.835 + padding * 2 + 0.5
  const maxDataRows = Math.max(0, Math.floor((maxH - lineH) / lineH))
  const visRows = rows.slice(0, maxDataRows)
  let cy = y

  doc.setFillColor(22, 119, 255)
  doc.rect(x, cy, w, lineH, 'F')
  doc.setFont('helvetica', 'bold')
  doc.setFontSize(fontSize)
  doc.setTextColor(255, 255, 255)
  let cx = x
  headers.forEach((h, i) => {
    const tx = i === 0 ? cx + padding + 0.5 : cx + colWidths[i] / 2
    doc.text(h, tx, cy + lineH - padding - 0.3, i > 0 ? { align: 'center' } : {})
    cx += colWidths[i]
  })
  cy += lineH

  visRows.forEach((row, ri) => {
    if (ri % 2 === 0) { doc.setFillColor(245, 248, 255); doc.rect(x, cy, w, lineH, 'F') }
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(fontSize)
    doc.setTextColor(30, 30, 30)
    cx = x
    row.forEach((cell, i) => {
      const tx = i === 0 ? cx + padding + 0.5 : cx + colWidths[i] / 2
      doc.text(String(cell ?? '—'), tx, cy + lineH - padding - 0.3, i > 0 ? { align: 'center' } : {})
      cx += colWidths[i]
    })
    doc.setDrawColor(220); doc.setLineWidth(0.1)
    doc.line(x, cy + lineH, x + w, cy + lineH)
    cy += lineH
  })

  doc.setDrawColor(100); doc.setLineWidth(0.3)
  doc.rect(x, y, w, cy - y)
  cx = x
  for (let i = 0; i < colWidths.length - 1; i++) {
    cx += colWidths[i]; doc.line(cx, y, cx, cy)
  }
  if (rows.length > maxDataRows) {
    doc.setFont('helvetica', 'italic'); doc.setFontSize(5.5); doc.setTextColor(140)
    doc.text(`+${rows.length - maxDataRows} more subject(s) — use fewer tickets per page`, x + 1, cy + 3.5)
  }
}

/**
 * Generate Hall Ticket PDFs — portrait A4, configurable tickets per page (1, 2 or 3).
 * @param {object}   opts
 * @param {string}   opts.schoolName
 * @param {string}   opts.className
 * @param {string}   opts.sectionName
 * @param {string}   opts.academicYear
 * @param {string}   opts.examName
 * @param {Array}    opts.schedule        - [{ subjectName, examDate, time, maxMarks }]
 * @param {Array}    opts.students        - [{ studentName, admissionNo, dateOfBirth, gender }]
 * @param {number}   [opts.ticketsPerPage=1] - 1 | 2 | 3
 * @param {string}   [opts.signatureUrl]  - Principal's signature image (School Settings); best-effort, skipped if it can't be loaded
 */
export async function generateHallTickets({
  schoolName, className, sectionName, academicYear, examName,
  schedule, students, ticketsPerPage = 1, signatureUrl,
}) {
  const doc  = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
  const pageW = doc.internal.pageSize.getWidth()
  const pageH = doc.internal.pageSize.getHeight()
  const mL = 13, mR = 13
  const contentW = pageW - mL - mR

  const pagePad   = 5
  const cutGap    = ticketsPerPage > 1 ? 4 : 0
  const ticketH   = (pageH - pagePad * 2 - cutGap * (ticketsPerPage - 1)) / ticketsPerPage
  const compact   = ticketsPerPage >= 2
  const vCompact  = ticketsPerPage >= 3

  // Loaded once, reused on every ticket. Adds a small fixed allowance to the
  // signature strip only when an image is actually available — tickets with no
  // signature configured render with byte-identical geometry to before this.
  const signatureImg = await loadImageDataUrl(signatureUrl)
  // 1-per-page has ~280mm of near-empty ticket height to spare, so the signature
  // can be printed at a legible size there; 2/3-per-page stay modest since this
  // allowance eats directly into the schedule table's available height.
  const sigImgAllowance = signatureImg ? (vCompact ? 18 : compact ? 22 : 35) : 0

  const scheduleRows = schedule.map(s => [
    s.subjectName,
    [s.examDate, s.time].filter(Boolean).join(', ') || '—',
    s.maxMarks || '—',
    '',
  ])
  const tblHeaders   = ['Subject', 'Date & Time', 'Max Marks', 'Invigilator Sign']
  const unit         = contentW / 10
  const colWidths    = [unit * 3.3, unit * 3, unit * 1.2, unit * 2.5]

  students.forEach((student, idx) => {
    const slot = idx % ticketsPerPage
    if (idx > 0 && slot === 0) doc.addPage()
    const y0 = pagePad + slot * (ticketH + cutGap)

    // ── Dashed cut line above slot ────────────────────────────────────────────
    if (slot > 0) {
      doc.setLineDashPattern([2, 2], 0)
      doc.setDrawColor(170); doc.setLineWidth(0.3)
      doc.line(6, y0 - cutGap / 2, pageW - 6, y0 - cutGap / 2)
      doc.setLineDashPattern([], 0)
      doc.setFontSize(7); doc.setTextColor(170)
      doc.text('✂', 3, y0 - cutGap / 2 + 1.5)
    }

    // ── Border ────────────────────────────────────────────────────────────────
    doc.setDrawColor(22, 119, 255)
    doc.setLineWidth(vCompact ? 0.5 : 0.7)
    doc.rect(6, y0 + 1, pageW - 12, ticketH - 1.5)
    if (!vCompact) {
      doc.setLineWidth(0.2)
      doc.rect(7.5, y0 + 2, pageW - 15, ticketH - 3.5)
    }

    // ── School name ───────────────────────────────────────────────────────────
    const sFz = vCompact ? 10 : compact ? 13 : 16
    doc.setFont('helvetica', 'bold'); doc.setFontSize(sFz); doc.setTextColor(22, 119, 255)
    const sY = y0 + (vCompact ? 7 : compact ? 9 : 11)
    doc.text(schoolName, pageW / 2, sY, { align: 'center' })

    // ── Divider ───────────────────────────────────────────────────────────────
    const d1Y = sY + (vCompact ? 2.5 : 3)
    doc.setDrawColor(22, 119, 255); doc.setLineWidth(0.4)
    doc.line(mL, d1Y, pageW - mR, d1Y)

    // ── Title ─────────────────────────────────────────────────────────────────
    const tFz = vCompact ? 8 : compact ? 10 : 13
    doc.setFont('helvetica', 'bold'); doc.setFontSize(tFz); doc.setTextColor(40, 40, 40)
    const tY = d1Y + (vCompact ? 5.5 : compact ? 6.5 : 8)
    doc.text('HALL TICKET', pageW / 2, tY, { align: 'center' })
    const tW = doc.getTextWidth('HALL TICKET')
    doc.setLineWidth(0.3); doc.setDrawColor(40, 40, 40)
    doc.line((pageW - tW) / 2, tY + 1, (pageW + tW) / 2, tY + 1)

    // ── Exam info ─────────────────────────────────────────────────────────────
    const iFz = vCompact ? 7 : 8.5
    doc.setFont('helvetica', 'normal'); doc.setFontSize(iFz); doc.setTextColor(60, 60, 60)
    const iY = tY + (vCompact ? 5 : 6)
    doc.text(`Year: ${academicYear}`, mL + 2, iY)
    doc.text(`Exam: ${examName}`, pageW / 2 + 2, iY)

    // ── Photo box (not for 3-per-page) ────────────────────────────────────────
    if (!vCompact) {
      const phW = compact ? 26 : 33, phH = compact ? 33 : 42
      const phX = pageW - mR - phW, phY = iY + 4
      doc.setDrawColor(120); doc.setLineWidth(0.3)
      doc.rect(phX, phY, phW, phH)
      doc.setFontSize(7); doc.setTextColor(160)
      doc.text('Affix Photo', phX + phW / 2, phY + phH * 0.6, { align: 'center' })
    }

    // ── Student details ───────────────────────────────────────────────────────
    const dFz = vCompact ? 7.5 : compact ? 8.5 : 9.5
    const dSp = vCompact ? 5 : compact ? 6.5 : 7.5
    const dY  = iY + (vCompact ? 6 : 7)
    const lbW = vCompact ? 26 : 38
    const fields = [
      ['Name',     student.studentName || '—'],
      ['Adm No.',  student.admissionNo  || '—'],
      ['Class',    `${className}${sectionName ? ' — ' + sectionName : ''}`],
      ['DOB',      student.dateOfBirth  || '—'],
      ...(vCompact ? [] : [['Gender', student.gender || '—']]),
    ]
    fields.forEach(([label, value], i) => {
      const fy = dY + i * dSp
      doc.setFont('helvetica', 'bold'); doc.setFontSize(dFz); doc.setTextColor(60, 60, 60)
      doc.text(label, mL + 2, fy)
      doc.setFont('helvetica', 'normal'); doc.setTextColor(30, 30, 30)
      doc.text(`:  ${value}`, mL + lbW, fy)
    })

    // ── Divider before table ──────────────────────────────────────────────────
    const afterDY = dY + fields.length * dSp + (vCompact ? 2 : 3)
    doc.setDrawColor(200); doc.setLineWidth(0.2)
    doc.line(mL, afterDY, pageW - mR, afterDY)

    // ── Schedule table ────────────────────────────────────────────────────────
    const tblY   = afterDY + 2
    const sigH   = (vCompact ? 10 : compact ? 13 : 14) + sigImgAllowance
    const sigY   = y0 + ticketH - sigH
    const maxTH  = sigY - tblY - 2

    if (ticketsPerPage === 1) {
      // Full autoTable for 1-per-page
      autoTable(doc, {
        head: [tblHeaders], body: scheduleRows,
        startY: tblY, margin: { left: mL, right: mR },
        styles:     { fontSize: 9, cellPadding: 2.5 },
        headStyles: { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [245, 248, 255] },
        columnStyles: { 0: { cellWidth: 58 }, 1: { cellWidth: 50, halign: 'center' }, 2: { cellWidth: 26, halign: 'center' }, 3: { halign: 'center' } },
      })
      const iY2 = doc.lastAutoTable.finalY + 8
      doc.setFont('helvetica', 'bold'); doc.setFontSize(8.5); doc.setTextColor(40, 40, 40)
      doc.text('Instructions:', mL + 3, iY2)
      doc.setFont('helvetica', 'normal'); doc.setFontSize(8); doc.setTextColor(60, 60, 60)
      ;[
        '1. Candidates must carry this Hall Ticket to the examination hall.',
        '2. Report to the exam hall at least 15 minutes before the scheduled time.',
        '3. Electronic devices, mobile phones and calculators are strictly not allowed.',
        '4. This Hall Ticket is not transferable and must be shown on demand.',
      ].forEach((line, i) => doc.text(line, mL + 3, iY2 + 6 + i * 5.5))
    } else {
      // Manual bounded table — prevents overflow into adjacent ticket slot
      _drawTicketTable(doc, mL, tblY, contentW, maxTH, tblHeaders, scheduleRows,
        colWidths, vCompact ? 6 : 7, vCompact ? 1 : 1.5)
    }

    // ── Signatures ────────────────────────────────────────────────────────────
    const slLen = vCompact ? 42 : 52
    if (signatureImg) {
      // Sized to fit the reserved allowance, right-aligned above the
      // "Principal's Signature & Seal" line so it reads as signed on it.
      const maxImgH = sigImgAllowance - 1.5
      const maxImgW = vCompact ? 47 : compact ? 58 : 99
      const aspect  = signatureImg.width / Math.max(1, signatureImg.height)
      let drawW = maxImgW, drawH = drawW / aspect
      if (drawH > maxImgH) { drawH = maxImgH; drawW = drawH * aspect }
      try {
        doc.addImage(signatureImg.dataUrl, pageW - mR - 8 - drawW, sigY + 0.5, drawW, drawH)
      } catch { /* a corrupt/unsupported image must never block the ticket */ }
    }
    doc.setFontSize(vCompact ? 7.5 : 8.5)
    doc.setFont('helvetica', 'normal'); doc.setTextColor(40, 40, 40)
    doc.text('Signature of Candidate',       mL + 8,          sigY + sigImgAllowance + 4)
    doc.text("Principal's Signature & Seal", pageW - mR - 8,  sigY + sigImgAllowance + 4, { align: 'right' })
    doc.setDrawColor(100); doc.setLineWidth(0.3)
    doc.line(mL + 3,              sigY + sigImgAllowance + 5.5, mL + 3 + slLen,              sigY + sigImgAllowance + 5.5)
    doc.line(pageW - mR - 3 - slLen, sigY + sigImgAllowance + 5.5, pageW - mR - 3, sigY + sigImgAllowance + 5.5)
  })

  doc.save('hall_tickets.pdf')
}

/**
 * Images hosted by the API server (branding logo etc. under /Uploads) are cross-origin
 * to the school app and IIS serves them WITHOUT CORS headers, so the canvas below can't
 * read them. Fetch those through GET /school/uploads/image-data (a managed route that
 * does carry CORS headers) as a data URL instead. Other URLs (R2/CDN, bundled assets)
 * are returned unchanged.
 */
async function toReadableSrc(url) {
  const prefix = `${API_BASE}/Uploads/`
  if (!url || !url.toLowerCase().startsWith(prefix.toLowerCase())) return url
  try {
    const r = await api.get(`/school/uploads/image-data?path=${encodeURIComponent(url.slice(API_BASE.length))}`)
    return r.data?.data?.dataUrl || null
  } catch {
    return null
  }
}

/**
 * Load an image URL as a data URL so jsPDF can embed it.
 * Best-effort: resolves null on any failure (missing file, CORS-blocked canvas,
 * slow host) so a logo problem can never stop a report from downloading.
 * @returns {Promise<{dataUrl: string, width: number, height: number} | null>}
 */
async function loadImageDataUrl(url, timeoutMs = 4000) {
  const src = await toReadableSrc(url)
  return new Promise((resolve) => {
    if (!src) { resolve(null); return }
    let settled = false
    const done = (v) => { if (!settled) { settled = true; resolve(v) } }
    const timer = setTimeout(() => done(null), timeoutMs)

    const img = new Image()
    img.crossOrigin = 'anonymous'          // R2/CDN must send CORS headers, else canvas taints
    img.onload = () => {
      clearTimeout(timer)
      try {
        const canvas = document.createElement('canvas')
        canvas.width  = img.naturalWidth
        canvas.height = img.naturalHeight
        canvas.getContext('2d').drawImage(img, 0, 0)
        done({ dataUrl: canvas.toDataURL('image/png'), width: img.naturalWidth, height: img.naturalHeight })
      } catch { done(null) }               // tainted canvas — skip the logo
    }
    img.onerror = () => { clearTimeout(timer); done(null) }
    img.src = src
  })
}

/** Indian-grouped money, 2dp. No currency symbol — jsPDF's built-in helvetica is
 *  WinAnsi-encoded and cannot render the rupee sign (U+20B9); it comes out blank. */
const inr = (n) => Number(n || 0).toLocaleString('en-IN', {
  minimumFractionDigits: 2, maximumFractionDigits: 2,
})

/**
 * Generate and download a fee-collection listing PDF: letterhead block (logo, school
 * name, address, contact) repeated on every page, the table, then a totals block and
 * a "Page X of Y" footer.
 *
 * @param {object}   opts
 * @param {string}   opts.schoolName
 * @param {string[]} [opts.addressLines]  - pre-formatted lines under the school name
 * @param {string}   [opts.logoUrl]       - best-effort; skipped if it can't be loaded
 * @param {string}   opts.title           - e.g. 'Fee Collection Details'
 * @param {string}   [opts.rangeLabel]    - e.g. 'From 01-08-2026 To 31-08-2026'
 * @param {string[]} opts.columns
 * @param {any[][]}  opts.rows            - pre-formatted cell values
 * @param {object}   [opts.totals]        - { collected, byMode: [{mode, amount}], cancelledAmount, cancelledCount }
 * @param {object}   [opts.tableOptions]  - jspdf-autotable overrides
 * @param {string}   opts.fileName
 */
export async function exportListingPdf({
  schoolName, addressLines = [], logoUrl, title, rangeLabel,
  columns, rows, totals, tableOptions = {}, fileName,
}) {
  const doc   = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
  const pageW = doc.internal.pageSize.getWidth()
  const pageH = doc.internal.pageSize.getHeight()
  const mL = 10, mR = 10
  const stamp = new Date().toLocaleString('en-IN', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit', hour12: true,
  })

  const logo = await loadImageDataUrl(logoUrl)

  // Letterhead — drawn on every page. Returns the y to start content at.
  const drawHeader = () => {
    let y = 13

    if (logo) {
      const h = 16
      const w = Math.min(28, (logo.width / logo.height) * h)   // preserve aspect, cap width
      try { doc.addImage(logo.dataUrl, 'PNG', mL, 8, w, h) } catch { /* ignore */ }
    }

    doc.setFont('helvetica', 'bold')
    doc.setFontSize(15)
    doc.setTextColor(20, 20, 20)
    doc.text(schoolName || 'School', pageW / 2, y, { align: 'center' })

    doc.setFont('helvetica', 'normal')
    doc.setFontSize(8.5)
    doc.setTextColor(70, 70, 70)
    addressLines.filter(Boolean).forEach((line) => {
      y += 4.2
      doc.text(line, pageW / 2, y, { align: 'center' })
    })

    y += 6.5
    doc.setFont('helvetica', 'bold')
    doc.setFontSize(10.5)
    doc.setTextColor(30, 30, 30)
    doc.text([title, rangeLabel].filter(Boolean).join('  '), pageW / 2, y, { align: 'center' })

    y += 2.5
    doc.setDrawColor(150)
    doc.setLineWidth(0.3)
    doc.line(mL, y, pageW - mR, y)
    return y + 3
  }

  const headerBottom = drawHeader()

  autoTable(doc, {
    head:   [columns],
    body:   rows,
    startY: headerBottom,
    margin: { left: mL, right: mR, top: headerBottom },
    styles: { fontSize: 8, cellPadding: 1.8, overflow: 'linebreak' },
    headStyles: { fillColor: [22, 119, 255], textColor: 255, fontStyle: 'bold' },
    alternateRowStyles: { fillColor: [245, 248, 255] },
    // Page 1's header is already drawn; repeat it on every page after a break.
    didDrawPage: (data) => { if (data.pageNumber > 1) drawHeader() },
    ...tableOptions,
  })

  // ── Totals block (right-aligned, under the table) ─────────────────────────
  if (totals) {
    const lines = [
      ['Total Collected', inr(totals.collected)],
      ...(totals.byMode || []).map((m) => [`    ${m.mode}`, inr(m.amount)]),
      ...(totals.cancelledCount
        ? [[`Cancelled (excluded, ${totals.cancelledCount})`, inr(totals.cancelledAmount)]]
        : []),
    ]
    const blockH = lines.length * 5 + 4
    let y = doc.lastAutoTable.finalY + 6
    if (y + blockH > pageH - 14) { doc.addPage(); y = drawHeader() + 4 }

    const boxW = 78
    const boxX = pageW - mR - boxW
    doc.setDrawColor(150); doc.setLineWidth(0.3)
    doc.rect(boxX, y, boxW, blockH)

    y += 5.5
    lines.forEach(([label, value], i) => {
      const isTotal = i === 0
      doc.setFont('helvetica', isTotal ? 'bold' : 'normal')
      doc.setFontSize(isTotal ? 9.5 : 8.5)
      doc.setTextColor(isTotal ? 20 : 80)
      doc.text(label, boxX + 3, y)
      doc.text(value, boxX + boxW - 3, y, { align: 'right' })
      y += 5
    })
  }

  // ── Page footer: generated stamp + Page X of Y (needs the final page count) ─
  const total = doc.getNumberOfPages()
  for (let p = 1; p <= total; p++) {
    doc.setPage(p)
    doc.setFont('helvetica', 'normal')
    doc.setFontSize(7.5)
    doc.setTextColor(120)
    doc.text(`Generated: ${stamp}`, mL, pageH - 7)
    doc.text(`Page ${p} of ${total}`, pageW - mR, pageH - 7, { align: 'right' })
  }

  doc.save(fileName)
}

/**
 * Build the marks cards (progress reports) PDF — one A4 portrait page per student:
 * logo (left) + optional right image, school name, address, exam title, a student box
 * (name / admission no / father name · class / section / rank), then the subject table
 * (Subject | Internal | Written | Total | Grade) with a TOTAL row, the month-wise
 * attendance particulars (Jun → Mar), a Remarks box for handwriting, and Parent /
 * Class Teacher / Principal signature lines at the foot of the page.
 * Returns the jsPDF doc — the caller decides whether to open it for printing or save it.
 *
 * @param {object}   opts
 * @param {string}   opts.schoolName
 * @param {string}   [opts.address]        - single address line under the name
 * @param {string}   [opts.logoUrl]        - best-effort; skipped if it can't be loaded
 * @param {string}   [opts.rightImageUrl]  - best-effort; e.g. a founder photo (school-specific)
 * @param {string}   opts.examTitle        - e.g. 'PROGRESS REPORT - FA 1 (2026-27)'
 * @param {string}   opts.className
 * @param {string}   opts.sectionName
 * @param {object[]} opts.subjects         - { subjectId, subjectName, maxMarks, activityMaxMarks, hasActivity }
 * @param {object[]} opts.students         - MarksCardStudentDto (lines, total, totalGrade, rank, presentDays, …)
 * @param {object[]} [opts.attendanceMonths] - { label, workingDays } × 10 (Jun → Mar)
 * @param {number}   [opts.totalWorkingDays]
 */
export async function generateMarksCards({
  schoolName, address, logoUrl, rightImageUrl, examTitle,
  className, sectionName, subjects, students,
  attendanceMonths = [], totalWorkingDays = 0,
}) {
  const doc   = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' })
  const pageW = doc.internal.pageSize.getWidth()
  const pageH = doc.internal.pageSize.getHeight()
  const mL = 15, mR = 15
  const contentW = pageW - mL - mR

  const [logo, right] = await Promise.all([loadImageDataUrl(logoUrl), loadImageDataUrl(rightImageUrl)])

  const n = (v) => String(Math.round(Number(v) * 100) / 100)
  const uniq = (arr) => [...new Set(arr.map((v) => Number(v)))]

  // A subject with nothing entered for this student is left off their card; an absent
  // subject IS data and stays (printed as AB).
  const hasData = (st, sub) => {
    const l = (st.lines || []).find((x) => x.subjectId === sub.subjectId)
    return !!l && (l.entered || l.isAbsent)
  }

  // Max marks go in the column header when every PRINTED subject shares them; otherwise
  // each cell carries its own "x / max". Computed per card, because the printed subject
  // list can differ from student to student.
  const layoutFor = (subs) => {
    const actSubs      = subs.filter((s) => s.hasActivity)
    const showInternal = actSubs.length > 0
    const actMaxes     = uniq(actSubs.map((s) => s.activityMaxMarks))
    const wMaxes       = uniq(subs.map((s) => s.maxMarks))
    const tMaxes       = uniq(subs.map((s) => Number(s.maxMarks) + (s.hasActivity ? Number(s.activityMaxMarks) : 0)))
    const actUniform = actMaxes.length === 1, wUniform = wMaxes.length === 1, tUniform = tMaxes.length === 1
    const widths = showInternal ? [60, 30, 32, 28, 30] : [80, 36, 32, 32]
    return {
      showInternal, actUniform, wUniform, tUniform,
      head: [[
        'SUBJECT',
        ...(showInternal ? [`INTERNAL\nMARKS${actUniform ? `\n(${n(actMaxes[0])})` : ''}`] : []),
        `WRITTEN TEST\nMARKS${wUniform ? `\n(${n(wMaxes[0])})` : ''}`,
        `TOTAL${tUniform ? `\n(${n(tMaxes[0])})` : ''}`,
        'GRADE',
      ]],
      columnStyles: Object.fromEntries(
        widths.map((w, i) => [i, { cellWidth: w, halign: i === 0 ? 'left' : 'center' }])),
    }
  }

  const bodyFor = (st, subs, lay) => {
    const { showInternal, actUniform, wUniform, tUniform } = lay
    const bySubject = Object.fromEntries((st.lines || []).map((l) => [l.subjectId, l]))
    const rows = subs.map((sub) => {
      const l = bySubject[sub.subjectId] || {}
      const mark = (v, max, uniform) =>
        l.isAbsent ? 'AB' : v == null ? '-' : uniform ? n(v) : `${n(v)} / ${n(max)}`
      const subMax = Number(sub.maxMarks) + (sub.hasActivity ? Number(sub.activityMaxMarks) : 0)
      return [
        sub.subjectName,
        ...(showInternal ? [sub.hasActivity ? mark(l.activityMarks, sub.activityMaxMarks, actUniform) : '-'] : []),
        mark(l.marksObtained, sub.maxMarks, wUniform),
        mark(l.subjectTotal, subMax, tUniform),
        l.grade || '',
      ]
    })
    const bold = { fontStyle: 'bold' }
    rows.push([
      { content: 'TOTAL', colSpan: showInternal ? 3 : 2, styles: { ...bold, halign: 'left' } },
      { content: st.hasMarks ? n(st.total) : '-', styles: bold },
      { content: st.totalGrade || '', styles: bold },
    ])
    return rows
  }

  const drawHeader = () => {
    const top = 12, imgH = 24, pad = 4
    let logoW = 0, rightW = 0

    if (logo) {
      logoW = Math.min(28, (logo.width / logo.height) * imgH)
      try { doc.addImage(logo.dataUrl, 'PNG', mL + pad, top, logoW, imgH) } catch { logoW = 0 }
    }
    if (right) {
      rightW = Math.min(28, (right.width / right.height) * imgH)
      try { doc.addImage(right.dataUrl, 'PNG', pageW - mR - pad - rightW, top, rightW, imgH) } catch { rightW = 0 }
    }

    // Name and address must stay inside the gap BETWEEN the images — centring them on
    // the page at a fixed width runs them under the logo on long school names/addresses.
    const textL = mL + (logoW ? pad + logoW + pad : 0)
    const textR = pageW - mR - (rightW ? pad + rightW + pad : 0)
    const textW = textR - textL
    const cx    = (textL + textR) / 2

    const name = (schoolName || 'School').toUpperCase()
    doc.setFont('times', 'bold')
    let size = 20
    doc.setFontSize(size)
    while (doc.getTextWidth(name) > textW && size > 10) { size -= 0.5; doc.setFontSize(size) }
    doc.setTextColor(0, 0, 0)
    doc.text(name, cx, top + 12, { align: 'center' })

    let y = top + 12
    if (address) {
      doc.setFont('times', 'normal')
      doc.setFontSize(11.5)
      const lines = doc.splitTextToSize(address, textW).slice(0, 3)
      lines.forEach((line) => { y += 5.5; doc.text(line, cx, y, { align: 'center' }) })
    }

    // The exam title spans the full width, so keep it clear of the images.
    y = Math.max(y + 7, top + imgH + 6)
    doc.setFont('helvetica', 'bold')
    doc.setFontSize(11)
    doc.text(examTitle || '', pageW / 2, y, { align: 'center', maxWidth: pageW - mL - mR })
    return y + 5
  }

  const drawStudentBox = (st, y0) => {
    const h = 24
    doc.setDrawColor(0); doc.setLineWidth(0.3)
    doc.rect(mL, y0, contentW, h)

    const labelL = mL + 3, valueL = mL + 30
    const labelR = mL + 110, valueR = mL + 127
    const fit = (text, w) => doc.splitTextToSize(String(text ?? ''), w)[0] || ''
    const field = (label, value, lx, vx, vw, y) => {
      doc.setFont('helvetica', 'bold');   doc.setFontSize(10); doc.text(label, lx, y)
      doc.setFont('helvetica', 'normal'); doc.setFontSize(10); doc.text(fit(value, vw), vx, y)
    }
    const rows = [
      ['Student Name:', st.studentName,     'Class:',   className],
      ['Admin No:',     st.admissionNo,     'Section:', sectionName],
      ['Father Name:',  st.fatherName || '', 'Rank:',   st.rank ?? '-'],
    ]
    rows.forEach(([l1, v1, l2, v2], i) => {
      const y = y0 + 7 + i * 7
      field(l1, v1, labelL, valueL, labelR - valueL - 4, y)
      field(l2, v2, labelR, valueR, mL + contentW - valueR - 3, y)
    })
    return y0 + h
  }

  const gridStyles = {
    theme:  'grid',
    margin: { left: mL, right: mR },
    headStyles: { fillColor: [255, 255, 255], textColor: [0, 0, 0], fontStyle: 'bold',
                  lineColor: [0, 0, 0], lineWidth: 0.2 },
  }
  const cellStyles = { font: 'helvetica', textColor: [0, 0, 0], lineColor: [0, 0, 0], lineWidth: 0.2, valign: 'middle' }

  // Signature lines sit at the foot of the page; everything above must end before sigTop.
  const sigLineY = pageH - 24
  const sigTop   = sigLineY - 10

  const drawAttendance = (st, y) => {
    doc.setFont('helvetica', 'bold'); doc.setFontSize(10); doc.setTextColor(0, 0, 0)
    doc.text('Attendance Particulars :', mL, y)

    const wd  = attendanceMonths.map((m) => (m.workingDays == null ? '' : String(m.workingDays)))
    const pd  = (st.presentDays || []).map((v) => (v == null ? '' : n(v)))
    const pct = totalWorkingDays > 0
      ? `${((Number(st.totalPresent) / totalWorkingDays) * 100).toFixed(1)}%` : '-'
    const center = { halign: 'center' }
    autoTable(doc, {
      ...gridStyles,
      head: [['Month', ...attendanceMonths.map((m) => m.label), 'Total', '%']],
      body: [
        ['Working Days', ...wd, totalWorkingDays ? String(totalWorkingDays) : '',
          { content: pct, rowSpan: 2, styles: { ...center, fontStyle: 'bold' } }],
        ['Present Days', ...pd, totalWorkingDays ? n(st.totalPresent) : ''],
      ],
      startY: y + 2,
      styles: { ...cellStyles, fontSize: 9, cellPadding: 1.6 },
      headStyles: { ...gridStyles.headStyles, halign: 'center' },
      columnStyles: {
        0: { cellWidth: 28, halign: 'left', fontStyle: 'bold' },
        ...Object.fromEntries(attendanceMonths.map((_, i) => [i + 1, { cellWidth: 11.5, ...center }])),
        [attendanceMonths.length + 1]: { cellWidth: 17, ...center },
        [attendanceMonths.length + 2]: { cellWidth: 20, ...center },
      },
    })
    return doc.lastAutoTable.finalY
  }

  const drawRemarks = (y) => {
    doc.setFont('helvetica', 'bold'); doc.setFontSize(10); doc.setTextColor(0, 0, 0)
    doc.text('Remarks :', mL, y)
    const boxTop = y + 3
    const boxH   = Math.max(14, Math.min(28, sigTop - 6 - boxTop))   // blank — written by hand
    doc.setDrawColor(0); doc.setLineWidth(0.3)
    doc.rect(mL, boxTop, contentW, boxH)
  }

  const drawSignatures = () => {
    const labels = ['Parent Sign', 'Class Teacher Sign', 'Principal Sign']
    const lineW  = 50
    const gap    = (contentW - lineW * labels.length) / (labels.length - 1)
    doc.setDrawColor(0); doc.setLineWidth(0.3)
    doc.setFont('helvetica', 'normal'); doc.setFontSize(9.5); doc.setTextColor(0, 0, 0)
    labels.forEach((label, i) => {
      const x = mL + i * (lineW + gap)
      doc.line(x, sigLineY, x + lineW, sigLineY)
      doc.text(label, x + lineW / 2, sigLineY + 5, { align: 'center' })
    })
  }

  students.forEach((st, i) => {
    if (i > 0) doc.addPage()
    const headerBottom = drawHeader()
    const boxBottom    = drawStudentBox(st, headerBottom + 2)

    // Only the subjects this student has marks for. A student with nothing entered at
    // all keeps the full list, so the card shows the subjects rather than an empty table.
    const withData = subjects.filter((s) => hasData(st, s))
    const subs     = withData.length ? withData : subjects
    const lay      = layoutFor(subs)

    autoTable(doc, {
      ...gridStyles,
      head:   lay.head,
      body:   bodyFor(st, subs, lay),
      startY: boxBottom + 8,
      styles: { ...cellStyles, fontSize: 10, cellPadding: 1.8 },
      columnStyles: lay.columnStyles,
    })

    // Attendance (~26 mm) + Remarks label and a minimum box (~24 mm) must fit above the
    // signatures; a card with a very long subject list continues on a second page.
    let y = doc.lastAutoTable.finalY + 8
    if (y + 26 + 24 > sigTop) { doc.addPage(); y = 20 }
    y = drawAttendance(st, y) + 9
    drawRemarks(y)
    drawSignatures()
  })

  return doc
}

/**
 * Generate and download a CSV (data only — no header block).
 * @param {object} opts
 * @param {string}   opts.columns  - Column header labels
 * @param {any[][]}  opts.rows     - 2-D array of cell values
 * @param {string}   opts.fileName - e.g. 'class_students.csv'
 */
export function exportCsv({ columns, rows, fileName }) {
  const csv = Papa.unparse({ fields: columns, data: rows })
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
  const url  = URL.createObjectURL(blob)
  const a    = document.createElement('a')
  a.href     = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}
