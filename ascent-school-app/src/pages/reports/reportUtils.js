import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'
import Papa from 'papaparse'

/**
 * Generate and download a PDF with a standard school header + autoTable.
 * @param {object} opts
 * @param {string}   opts.schoolName
 * @param {string}   opts.title        - Report title line
 * @param {string[]} opts.columns      - Column header labels
 * @param {any[][]}  opts.rows         - 2-D array of cell values
 * @param {string}   opts.fileName     - e.g. 'class_students.pdf'
 * @param {object}   [opts.tableOptions] - jspdf-autotable overrides (fontSize, columnStyles…)
 */
export function exportPdf({ schoolName, title, columns, rows, fileName, tableOptions = {} }) {
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
    head:       [columns],
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
 */
export function generateHallTickets({
  schoolName, className, sectionName, academicYear, examName,
  schedule, students, ticketsPerPage = 1,
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

  const scheduleRows = schedule.map(s => [s.subjectName, s.examDate || '—', s.time || '—', s.maxMarks || '—'])
  const tblHeaders   = ['Subject', 'Date', 'Time', 'Max Marks']
  const unit         = contentW / 10
  const colWidths    = [unit * 4, unit * 2.2, unit * 2.5, unit * 1.3]

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
    const sigH   = vCompact ? 10 : compact ? 13 : 14
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
        columnStyles: { 0: { cellWidth: 65 }, 1: { cellWidth: 32, halign: 'center' }, 2: { cellWidth: 48, halign: 'center' }, 3: { halign: 'center' } },
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
    doc.setFontSize(vCompact ? 7.5 : 8.5)
    doc.setFont('helvetica', 'normal'); doc.setTextColor(40, 40, 40)
    doc.text('Signature of Candidate',       mL + 8,          sigY + 4)
    doc.text("Principal's Signature & Seal", pageW - mR - 8,  sigY + 4, { align: 'right' })
    doc.setDrawColor(100); doc.setLineWidth(0.3)
    doc.line(mL + 3,              sigY + 5.5, mL + 3 + slLen,              sigY + 5.5)
    doc.line(pageW - mR - 3 - slLen, sigY + 5.5, pageW - mR - 3, sigY + 5.5)
  })

  doc.save('hall_tickets.pdf')
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
