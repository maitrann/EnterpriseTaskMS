import { TaskFormOptions } from '../models/task-form.model';
import { Task } from '../models/task.model';
import { TASK_STATUS_DEFINITIONS } from '../constants/task-status.constants';

const statuses = TASK_STATUS_DEFINITIONS.map((status) => status.id);
const priorities = [1, 2, 3, 4];
const taskTypes = ['Vận hành', 'Báo cáo', 'Phê duyệt', 'Liên phòng'];
const urgencyLevels = ['Thông thường', 'Gấp', 'Khẩn'];
const securityLevels = ['Nội bộ', 'Hạn chế', 'Bảo mật'];
const sources = ['Kế hoạch tháng', 'Chỉ đạo lãnh đạo', 'Văn bản đến', 'Yêu cầu liên phòng'];
const departmentIds = [1, 2, 3, 4, 5];
const tags = ['Báo cáo', 'Hợp đồng', 'Nhân sự', 'Thông báo', 'Phê duyệt', 'Liên phòng'];

function randomItem<T>(items: T[]): T {
  return items[Math.floor(Math.random() * items.length)];
}

function randomDate(start: Date, end: Date) {
  return new Date(start.getTime() + Math.random() * (end.getTime() - start.getTime()));
}

function randomSubset<T>(items: T[], min = 0, max = 2) {
  const count = Math.min(items.length, Math.floor(Math.random() * (max - min + 1)) + min);
  const shuffled = [...items].sort(() => Math.random() - 0.5);
  return shuffled.slice(0, count);
}

const now = new Date();

export const TASK_FORM_OPTIONS_MOCK: TaskFormOptions = {
  taskTypes: [
    { value: 'van-hanh', label: 'Vận hành' },
    { value: 'bao-cao', label: 'Báo cáo' },
    { value: 'phe-duyet', label: 'Phê duyệt' },
    { value: 'lien-phong', label: 'Liên phòng' }
  ],
  departments: [
    { id: 1, label: 'Ban điều hành' },
    { id: 2, label: 'Nhân sự' },
    { id: 3, label: 'Kế toán' },
    { id: 4, label: 'Hành chính' },
    { id: 5, label: 'CNTT' }
  ],
  users: [
    { id: 1, label: 'Trần Minh Quân', role: 'Quản trị viên', departmentId: 1 },
    { id: 2, label: 'Lê Thu Hà', role: 'Trưởng phòng Nhân sự', departmentId: 2 },
    { id: 3, label: 'Nguyễn Hoàng Mai', role: 'Chuyên viên Kế toán', departmentId: 3 },
    { id: 4, label: 'Phạm Gia Linh', role: 'Điều phối Hành chính', departmentId: 4 },
    { id: 5, label: 'Đỗ Anh Tuấn', role: 'Kỹ sư CNTT', departmentId: 5 },
    { id: 6, label: 'Vũ Ngọc Anh', role: 'Thư ký dự án', departmentId: 1 }
  ],
  priorities: [
    { value: 1, label: 'Thấp' },
    { value: 2, label: 'Trung bình' },
    { value: 3, label: 'Cao' },
    { value: 4, label: 'Khẩn cấp' }
  ],
  urgencyLevels: [
    { value: 'thong-thuong', label: 'Thông thường' },
    { value: 'gap', label: 'Gấp' },
    { value: 'khan', label: 'Khẩn' }
  ],
  securityLevels: [
    { value: 'noi-bo', label: 'Nội bộ' },
    { value: 'han-che', label: 'Hạn chế' },
    { value: 'bao-mat', label: 'Bảo mật' }
  ],
  sources: [
    { value: 'ke-hoach-thang', label: 'Kế hoạch tháng' },
    { value: 'chi-dao-lanh-dao', label: 'Chỉ đạo lãnh đạo' },
    { value: 'van-ban-den', label: 'Văn bản đến' },
    { value: 'yeu-cau-lien-phong', label: 'Yêu cầu liên phòng' }
  ]
};

export const TASK_MOCK: Task[] = Array.from({ length: 50 }).map((_, index) => {
  const start = randomDate(new Date(2026, 2, 1), new Date(2026, 2, 20));
  const statusId = randomItem(statuses);
  const due =
    statusId === 10
      ? randomDate(new Date(2026, 1, 20), new Date(2026, 2, 10))
      : randomDate(new Date(2026, 2, 20), new Date(2026, 3, 5));
  const departmentId = randomItem(departmentIds);
  const departmentUsers = TASK_FORM_OPTIONS_MOCK.users
    .filter((user) => user.departmentId === departmentId)
    .map((user) => user.id);
  const fallbackUsers = TASK_FORM_OPTIONS_MOCK.users.map((user) => user.id);
  const assigneePool = departmentUsers.length ? departmentUsers : fallbackUsers;
  const assigneeId = randomItem(assigneePool);
  const collaboratorIds = randomSubset(
    fallbackUsers.filter((userId) => userId !== assigneeId),
    1,
    2
  );
  const watcherIds = randomSubset(
    fallbackUsers.filter((userId) => userId !== assigneeId && !collaboratorIds.includes(userId)),
    1,
    2
  );
  const progress =
    statusId === 1 ? 0 :
    statusId === 2 ? randomItem([0, 5, 10]) :
    statusId === 3 ? Math.floor(Math.random() * 45) + 25 :
    statusId === 4 ? Math.floor(Math.random() * 35) + 15 :
    statusId === 5 ? Math.floor(Math.random() * 20) + 55 :
    statusId === 6 ? Math.floor(Math.random() * 10) + 80 :
    statusId === 7 ? 100 :
    statusId === 8 ? 100 :
    statusId === 9 ? Math.floor(Math.random() * 30) :
    Math.floor(Math.random() * 35) + 50;

  return {
    id: index + 1,
    code: `CV-${String(index + 1).padStart(4, '0')}`,
    projectId: 1,
    parentTaskId: undefined,
    title: `Công việc #${index + 1}`,
    description: `Đây là công việc mock số ${index + 1}`,
    taskType: randomItem(taskTypes),
    departmentId,
    statusId,
    priorityId: randomItem(priorities),
    urgencyLevel: randomItem(urgencyLevels),
    securityLevel: randomItem(securityLevels),
    reporterId: 1,
    assigneeId,
    collaboratorIds,
    watcherIds,
    startDate: start,
    dueDate: due,
    progress,
    source: randomItem(sources),
    attachmentNames: Math.random() > 0.5 ? [`tai-lieu-${index + 1}.docx`] : [],
    tags: randomSubset(tags, 1, 3),
    processingNotes: [`Đã tiếp nhận và đang theo dõi tiến độ công việc #${index + 1}.`],
    estimatedHours: Math.floor(Math.random() * 40) + 4,
    actualHours: Math.floor(Math.random() * 40),
    createdAt: now,
    updatedAt: now
  };
});
