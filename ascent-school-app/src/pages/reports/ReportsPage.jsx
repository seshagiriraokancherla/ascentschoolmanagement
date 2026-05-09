import { useState } from 'react'
import { Layout, Menu, Typography, Card } from 'antd'
import { BarChartOutlined, TeamOutlined, FieldNumberOutlined, StopOutlined, TableOutlined, CarOutlined, FileDoneOutlined, CalendarOutlined, ScheduleOutlined, BookOutlined, IdcardOutlined, TrophyOutlined, CloseCircleOutlined, ReadOutlined, BankOutlined, AlertOutlined, PauseCircleOutlined } from '@ant-design/icons'
import ClassStudentsReport          from './ClassStudentsReport'
import TotalStrengthReport          from './TotalStrengthReport'
import AbsentsReport                from './AbsentsReport'
import AttendanceSheetReport        from './AttendanceSheetReport'
import TransportStudentsReport      from './TransportStudentsReport'
import StudyCertificateReport       from './StudyCertificateReport'
import DailyAttendanceSummaryReport from './DailyAttendanceSummaryReport'
import MonthlyAttendanceSheetReport from './MonthlyAttendanceSheetReport'
import AttendanceRegisterReport     from './AttendanceRegisterReport'
import ExamHallTicketReport         from './ExamHallTicketReport'
import ExamToppersReport            from './ExamToppersReport'
import ClassToppersReport           from './ClassToppersReport'
import FailedStudentsReport         from './FailedStudentsReport'
import AcademicYearToppersReport    from './AcademicYearToppersReport'
import HomeworkStatementReport      from './HomeworkStatementReport'
import SubjectHomeworkReport        from './SubjectHomeworkReport'
import StaffSalaryStatementReport   from './StaffSalaryStatementReport'
import RegularAbsenteesReport       from './RegularAbsenteesReport'
import DetainedStudentsReport       from './DetainedStudentsReport'

const { Sider, Content } = Layout
const { Text } = Typography

const REPORTS = [
  {
    key:         'class-students',
    label:       'Class-wise Students',
    icon:        <TeamOutlined />,
    component:   <ClassStudentsReport />,
    description: 'Active students listed by class and section',
  },
  {
    key:         'total-strength',
    label:       'Total Strength',
    icon:        <FieldNumberOutlined />,
    component:   <TotalStrengthReport />,
    description: 'Class-wise student count with boys / girls breakdown',
  },
  {
    key:         'absents',
    label:       'Absents Statement',
    icon:        <StopOutlined />,
    component:   <AbsentsReport />,
    description: 'Students absent within a date range',
  },
  {
    key:         'attendance-sheet',
    label:       'Attendance Sheet',
    icon:        <TableOutlined />,
    component:   <AttendanceSheetReport />,
    description: 'Monthly attendance grid — students × days',
  },
  {
    key:         'transport-students',
    label:       'Transport Students',
    icon:        <CarOutlined />,
    component:   <TransportStudentsReport />,
    description: 'Students using school bus, filterable by route',
  },
  {
    key:         'study-certificate',
    label:       'Study Certificate',
    icon:        <FileDoneOutlined />,
    component:   <StudyCertificateReport />,
    description: 'Bonafide / study certificate for a student — PDF only',
  },
  {
    key:         'daily-attendance-summary',
    label:       'Daily Attendance Summary',
    icon:        <CalendarOutlined />,
    component:   <DailyAttendanceSummaryReport />,
    description: 'Day-wise class-wise attendance count with present / absent / late breakdown',
  },
  {
    key:         'monthly-attendance-sheet',
    label:       'Monthly Attendance Sheet',
    icon:        <ScheduleOutlined />,
    component:   <MonthlyAttendanceSheetReport />,
    description: 'Student-wise monthly attendance totals (P/A/L per month) for a full academic year',
  },
  {
    key:         'attendance-register',
    label:       'Attendance Register',
    icon:        <BookOutlined />,
    component:   <AttendanceRegisterReport />,
    description: 'Formal date-range attendance register — students × working days with P/A/L',
  },
  {
    key:         'exam-hall-ticket',
    label:       'Exam Hall Ticket',
    icon:        <IdcardOutlined />,
    component:   <ExamHallTicketReport />,
    description: 'Generate per-student hall tickets with exam schedule — PDF only',
  },
  {
    key:         'exam-toppers',
    label:       'Exam wise Toppers',
    icon:        <TrophyOutlined />,
    component:   <ExamToppersReport />,
    description: 'Class-wise ranked toppers for an examination with subject-wise marks breakdown',
  },
  {
    key:         'class-toppers',
    label:       'Class wise Toppers',
    icon:        <TrophyOutlined />,
    component:   <ClassToppersReport />,
    description: 'Exam-wise toppers for a selected class — all exams of the year in one view',
  },
  {
    key:         'failed-students',
    label:       'Failed Students List',
    icon:        <CloseCircleOutlined />,
    component:   <FailedStudentsReport />,
    description: 'Students who scored below pass mark in one or more subjects for an examination',
  },
  {
    key:         'academic-year-toppers',
    label:       'Academic Year Toppers',
    icon:        <TrophyOutlined />,
    component:   <AcademicYearToppersReport />,
    description: 'Cumulative toppers ranked by grand total across all exams in the academic year',
  },
  {
    key:         'homework-statement',
    label:       'Homework Statement',
    icon:        <ReadOutlined />,
    component:   <HomeworkStatementReport />,
    description: 'Day wise homework assignments grouped by due date with class and subject details',
  },
  {
    key:         'subject-homework',
    label:       'Subject wise Homework',
    icon:        <ReadOutlined />,
    component:   <SubjectHomeworkReport />,
    description: 'Homework grouped by subject — filterable by class and subject with date range',
  },
  {
    key:         'detained-students',
    label:       'Detained Students',
    icon:        <PauseCircleOutlined />,
    component:   <DetainedStudentsReport />,
    description: 'Students marked as detained — will not be promoted to the next class',
  },
  {
    key:         'regular-absentees',
    label:       'Regular Absentees',
    icon:        <AlertOutlined />,
    component:   <RegularAbsenteesReport />,
    description: 'Students absent more than a threshold number of days within a date range',
  },
  {
    key:         'staff-salary-statement',
    label:       'Staff Salary Statement',
    icon:        <BankOutlined />,
    component:   <StaffSalaryStatementReport />,
    description: 'Month-wise salary statement for all staff with earnings and deductions breakdown',
  },
  // Add more reports here
]

export default function ReportsPage() {
  const [selected, setSelected] = useState('class-students')

  const report = REPORTS.find(r => r.key === selected)

  return (
    <Layout style={{ background: 'transparent', minHeight: 'calc(100vh - 120px)' }}>
      <Sider
        width={220}
        style={{ background: '#fff', borderRadius: 8, marginRight: 16, padding: '8px 0' }}
      >
        <div style={{ padding: '12px 16px 8px', borderBottom: '1px solid #f0f0f0', marginBottom: 4 }}>
          <Text strong style={{ fontSize: 13 }}>
            <BarChartOutlined style={{ marginRight: 6 }} />
            Reports
          </Text>
        </div>
        <Menu
          mode="inline"
          selectedKeys={[selected]}
          onClick={({ key }) => setSelected(key)}
          items={REPORTS.map(r => ({ key: r.key, icon: r.icon, label: r.label }))}
          style={{ border: 'none' }}
        />
      </Sider>

      <Content>
        <Card
          title={
            <span>
              {report?.icon}
              <span style={{ marginLeft: 8 }}>{report?.label}</span>
            </span>
          }
          extra={<Text type="secondary" style={{ fontSize: 12 }}>{report?.description}</Text>}
        >
          {report?.component}
        </Card>
      </Content>
    </Layout>
  )
}
