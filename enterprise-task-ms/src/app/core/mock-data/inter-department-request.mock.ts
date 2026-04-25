import {
  InterDepartmentRequest,
  RequestDepartmentRef,
  RequestOwnerRef,
  RequestSlaPolicy
} from '../models/inter-department-request.model';

export const INTER_DEPARTMENT_SLA_POLICY_MOCK: RequestSlaPolicy[] = [
  { key: 'procurement', label: 'Mua sam', targetHours: 48, warnHours: 8 },
  { key: 'asset', label: 'Cap phat thiet bi', targetHours: 24, warnHours: 4 },
  { key: 'it-support', label: 'Ho tro IT', targetHours: 8, warnHours: 2 },
  { key: 'payment', label: 'Thanh toan / tam ung', targetHours: 36, warnHours: 6 },
  { key: 'recruitment', label: 'Tuyen dung', targetHours: 72, warnHours: 12 },
  { key: 'communication-design', label: 'Truyen thong / thiet ke', targetHours: 60, warnHours: 10 },
  { key: 'legal', label: 'Phap ly / hop dong', targetHours: 96, warnHours: 16 }
];

export const INTER_DEPARTMENT_DEPARTMENT_MOCK: RequestDepartmentRef[] = [
  { id: 'hr-admin', name: 'Hanh chinh - Nhan su' },
  { id: 'it', name: 'IT noi bo' },
  { id: 'finance', name: 'Ke toan - Tai chinh' },
  { id: 'marketing', name: 'Marketing' },
  { id: 'legal', name: 'Phap che' }
];

export const INTER_DEPARTMENT_OWNER_MOCK: RequestOwnerRef[] = [
  { id: 'owner-001', name: 'Pham Duc Long', departmentId: 'it', departmentName: 'IT noi bo' },
  { id: 'owner-002', name: 'Tran Thu Ha', departmentId: 'finance', departmentName: 'Ke toan - Tai chinh' },
  { id: 'owner-003', name: 'Le Hoang Phuc', departmentId: 'marketing', departmentName: 'Marketing' },
  { id: 'owner-004', name: 'Vu Thu Trang', departmentId: 'hr-admin', departmentName: 'Hanh chinh - Nhan su' },
  { id: 'owner-005', name: 'Nguyen Hai Dang', departmentId: 'legal', departmentName: 'Phap che' }
];

export const INTER_DEPARTMENT_REQUEST_MOCK: InterDepartmentRequest[] = [
  {
    id: '1',
    code: 'IR-001',
    type: 'asset',
    title: 'Cap phat laptop va the gui xe cho nhan vien thu viec',
    description:
      'Nhan vien moi bat dau lam vao sang thu Hai. Can cap laptop, tai khoan wifi va the gui xe truoc 08:30.',
    requesterDepartment: 'Hanh chinh - Nhan su',
    requesterDepartmentId: 'hr-admin',
    requesterName: 'Nguyen Minh Chau',
    requesterUserId: 2,
    targetDepartment: 'IT noi bo',
    targetDepartmentId: 'it',
    owner: null,
    ownerId: null,
    priority: 'Critical',
    status: 'new',
    createdAt: '2026-04-22T08:15:00+07:00',
    updatedAt: '2026-04-24T09:10:00+07:00',
    receivedAt: null,
    closedAt: null,
    dueDate: '24/04/2026',
    sla: {
      policyKey: 'asset',
      policyLabel: 'Cap phat thiet bi',
      targetHours: 24,
      warnHours: 4,
      startedAt: '2026-04-22T08:15:00+07:00',
      dueAt: '2026-04-24T08:15:00+07:00',
      remainingHours: -1,
      breached: true
    },
    formValues: {
      'Loai thiet bi': 'Laptop + phu kien onboarding',
      'Muc dich': 'Onboarding nhan vien thu viec',
      'Dia diem giao': 'Van phong tang 5'
    },
    latestMessage: 'Ben gui da tao phieu, dang cho IT tiep nhan.',
    note: 'Can uu tien vi nhan su moi tham gia du an ngay trong tuan nay.',
    messages: [
      {
        id: 'msg-001',
        authorName: 'Nguyen Minh Chau',
        authorRole: 'requester',
        authorDepartment: 'Hanh chinh - Nhan su',
        createdAt: '22/04/2026 08:15',
        body: 'Gui thong tin onboarding va de nghi cap laptop truoc ngay nhan viec.'
      }
    ]
  },
  {
    id: '2',
    code: 'IR-002',
    type: 'payment',
    title: 'Tam ung chi phi workshop noi bo quy 2',
    description:
      'Team van hanh can tam ung ngan sach de dat dia diem va in tai lieu cho workshop noi bo thang 5.',
    requesterDepartment: 'Marketing',
    requesterDepartmentId: 'marketing',
    requesterName: 'Do Khanh Linh',
    requesterUserId: 3,
    targetDepartment: 'Ke toan - Tai chinh',
    targetDepartmentId: 'finance',
    owner: 'Tran Thu Ha',
    ownerId: 'owner-002',
    priority: 'High',
    status: 'processing',
    createdAt: '2026-04-21T10:30:00+07:00',
    updatedAt: '2026-04-24T11:45:00+07:00',
    receivedAt: '2026-04-21T11:10:00+07:00',
    closedAt: null,
    dueDate: '25/04/2026',
    sla: {
      policyKey: 'payment',
      policyLabel: 'Thanh toan / tam ung',
      targetHours: 36,
      warnHours: 6,
      startedAt: '2026-04-21T10:30:00+07:00',
      dueAt: '2026-04-25T10:30:00+07:00',
      remainingHours: 23,
      breached: false
    },
    formValues: {
      'So tien de nghi': '35.000.000 VND',
      'Hinh thuc': 'Tam ung',
      'Ma tham chieu': 'MK-WORKSHOP-Q2'
    },
    latestMessage: 'Ke toan dang xu ly chung tu thanh toan.',
    note: 'Ho so da co ke hoach workshop va danh sach hang muc chi.',
    messages: [
      {
        id: 'msg-003',
        authorName: 'Do Khanh Linh',
        authorRole: 'requester',
        authorDepartment: 'Marketing',
        createdAt: '21/04/2026 10:30',
        body: 'Gui de nghi tam ung cho workshop noi bo va file du tru chi phi.'
      },
      {
        id: 'msg-004',
        authorName: 'Tran Thu Ha',
        authorRole: 'processor',
        authorDepartment: 'Ke toan - Tai chinh',
        createdAt: '24/04/2026 11:45',
        body: 'Ke toan da tiep nhan va dang doi chieu bo chung tu di kem.'
      }
    ]
  },
  {
    id: '5',
    code: 'IR-005',
    type: 'payment',
    title: 'Đối soát tạm ứng công tác phí tháng 04',
    description:
      'Phòng Marketing cần Kế toán - Tài chính tiếp nhận hồ sơ đối soát tạm ứng công tác phí và phân công người xử lý.',
    requesterDepartment: 'Marketing',
    requesterDepartmentId: 'marketing',
    requesterName: 'Đỗ Khánh Linh',
    requesterUserId: 3,
    targetDepartment: 'Kế toán - Tài chính',
    targetDepartmentId: 'finance',
    owner: null,
    ownerId: null,
    priority: 'Medium',
    status: 'received',
    createdAt: '2026-04-24T09:20:00+07:00',
    updatedAt: '2026-04-24T10:05:00+07:00',
    receivedAt: '2026-04-24T10:05:00+07:00',
    closedAt: null,
    dueDate: '26/04/2026',
    sla: {
      policyKey: 'payment',
      policyLabel: 'Thanh toán / tạm ứng',
      targetHours: 36,
      warnHours: 6,
      startedAt: '2026-04-24T09:20:00+07:00',
      dueAt: '2026-04-26T09:20:00+07:00',
      remainingHours: 20,
      breached: false
    },
    formValues: {
      'Số tiền đề nghị': '8.500.000 VND',
      'Hình thức': 'Đối soát tạm ứng',
      'Mã tham chiếu': 'CTP-0426'
    },
    latestMessage: 'Kế toán - Tài chính đã tiếp nhận phiếu và đang chờ phân công người xử lý.',
    note: 'Hồ sơ đã đủ bảng kê và chứng từ đính kèm.',
    messages: [
      {
        id: 'msg-009',
        authorName: 'Đỗ Khánh Linh',
        authorRole: 'requester',
        authorDepartment: 'Marketing',
        createdAt: '24/04/2026 09:20',
        body: 'Gửi hồ sơ đối soát tạm ứng công tác phí tháng 04 để bộ phận tài chính kiểm tra.'
      },
      {
        id: 'msg-010',
        authorName: 'Trần Thu Hà',
        authorRole: 'processor',
        authorDepartment: 'Kế toán - Tài chính',
        createdAt: '24/04/2026 10:05',
        body: 'Bộ phận tài chính đã tiếp nhận và sẽ phân công người xử lý chi tiết.'
      }
    ]
  },
  {
    id: '3',
    code: 'IR-003',
    type: 'communication-design',
    title: 'Thiet ke poster truyen thong cho chuong trinh noi bo',
    description:
      'Can poster doc 2 phien ban A3 va social square de truyen thong cho chuong trinh dao tao noi bo.',
    requesterDepartment: 'Hanh chinh - Nhan su',
    requesterDepartmentId: 'hr-admin',
    requesterName: 'Vu Thu Trang',
    requesterUserId: 2,
    targetDepartment: 'Marketing',
    targetDepartmentId: 'marketing',
    owner: 'Le Hoang Phuc',
    ownerId: 'owner-003',
    priority: 'Medium',
    status: 'waiting-requester',
    createdAt: '2026-04-23T09:00:00+07:00',
    updatedAt: '2026-04-24T08:40:00+07:00',
    receivedAt: '2026-04-23T09:20:00+07:00',
    closedAt: null,
    dueDate: '28/04/2026',
    sla: {
      policyKey: 'communication-design',
      policyLabel: 'Truyen thong / thiet ke',
      targetHours: 60,
      warnHours: 10,
      startedAt: '2026-04-23T09:00:00+07:00',
      dueAt: '2026-04-25T21:00:00+07:00',
      remainingHours: 36,
      breached: false
    },
    formValues: {
      'Dinh dang': 'Poster A3 + social square',
      'Key message': 'Dao tao ky nang quan ly cong viec',
      'Moc phat hanh': '29/04/2026'
    },
    latestMessage: 'Marketing da gui ban nhap, dang cho ben gui xac nhan noi dung.',
    note: 'Can tong mau dong bo voi chuong trinh training quy 2.',
    messages: [
      {
        id: 'msg-005',
        authorName: 'Vu Thu Trang',
        authorRole: 'requester',
        authorDepartment: 'Hanh chinh - Nhan su',
        createdAt: '23/04/2026 09:00',
        body: 'Gui brief poster va timeline phat hanh noi bo truoc ngay 29/04.'
      },
      {
        id: 'msg-006',
        authorName: 'Le Hoang Phuc',
        authorRole: 'processor',
        authorDepartment: 'Marketing',
        createdAt: '24/04/2026 08:40',
        body: 'Da gui layout nhap 1, vui long xac nhan key visual va thong diep.'
      }
    ]
  },
  {
    id: '4',
    code: 'IR-004',
    type: 'legal',
    title: 'Ra soat phu luc hop dong thue dich vu van chuyen',
    description:
      'Can bo phan phap che kiem tra dieu khoan bo sung ve SLA giao nhan va muc phat vi pham.',
    requesterDepartment: 'Ke toan - Tai chinh',
    requesterDepartmentId: 'finance',
    requesterName: 'Pham Tuan Kiet',
    requesterUserId: 8,
    targetDepartment: 'Phap che',
    targetDepartmentId: 'legal',
    owner: 'Nguyen Hai Dang',
    ownerId: 'owner-005',
    priority: 'High',
    status: 'done',
    createdAt: '2026-04-19T14:10:00+07:00',
    updatedAt: '2026-04-22T16:05:00+07:00',
    receivedAt: '2026-04-19T14:30:00+07:00',
    closedAt: null,
    dueDate: '22/04/2026',
    sla: {
      policyKey: 'legal',
      policyLabel: 'Phap ly / hop dong',
      targetHours: 96,
      warnHours: 16,
      startedAt: '2026-04-19T14:10:00+07:00',
      dueAt: '2026-04-23T14:10:00+07:00',
      remainingHours: 0,
      breached: false
    },
    formValues: {
      'Loai ho so': 'Phu luc hop dong',
      'Doi tac / don vi': 'VietMove Logistics',
      'Moc ap dung': '01/05/2026'
    },
    latestMessage: 'Bo phan phap che da hoan tat, dang cho ben yeu cau dong phieu.',
    note: 'Tai lieu da duoc thong nhat va cho xac nhan ket thuc.',
    messages: [
      {
        id: 'msg-007',
        authorName: 'Pham Tuan Kiet',
        authorRole: 'requester',
        authorDepartment: 'Ke toan - Tai chinh',
        createdAt: '19/04/2026 14:10',
        body: 'Gui phu luc can ra soat truoc khi ky voi doi tac van chuyen.'
      },
      {
        id: 'msg-008',
        authorName: 'Nguyen Hai Dang',
        authorRole: 'processor',
        authorDepartment: 'Phap che',
        createdAt: '22/04/2026 16:05',
        body: 'Da cap nhat dieu khoan SLA, muc phat va de nghi ben yeu cau xac nhan dong phieu.'
      }
    ]
  }
];
