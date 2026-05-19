using EnterpriseTask.Application.Development;
using EnterpriseTask.Infrastructure.Auth;
using EnterpriseTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EnterpriseTask.Infrastructure.Development;

public sealed class DatabaseSeeder(ApplicationDbContext dbContext, IConfiguration configuration) : IDatabaseSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var adminPassword = configuration["DevelopmentSeed:AdminPassword"];
        var userPassword = configuration["DevelopmentSeed:UserPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(userPassword))
        {
            throw new InvalidOperationException("Missing development seed passwords. Configure DevelopmentSeed:AdminPassword and DevelopmentSeed:UserPassword with user-secrets.");
        }

        var adminHash = PasswordHasher.Hash(adminPassword);
        var userHash = PasswordHasher.Hash(userPassword);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO companies (id, name, code) OVERRIDING SYSTEM VALUE VALUES
              (1, 'Enterprise Task Co.', 'ETMS')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, code = EXCLUDED.code;

            INSERT INTO departments (id, company_id, name, description) OVERRIDING SYSTEM VALUE VALUES
              (1, 1, 'Ban điều hành', 'Điều phối và giám sát vận hành toàn công ty'),
              (101, 1, 'Hành chính - Nhân sự', 'Quản lý nhân sự, hành chính và onboarding'),
              (102, 1, 'IT nội bộ', 'Hỗ trợ hệ thống, tài khoản và thiết bị'),
              (103, 1, 'Kế toán - Tài chính', 'Thanh toán, ngân sách và chứng từ'),
              (104, 1, 'Marketing', 'Truyền thông, thiết kế và nội dung'),
              (105, 1, 'Pháp chế', 'Hợp đồng, tuân thủ và pháp lý')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, description = EXCLUDED.description;

            INSERT INTO roles (id, code, name, description) OVERRIDING SYSTEM VALUE VALUES
              (1, 'admin', 'System Admin', 'Quản trị hệ thống'),
              (2, 'manager', 'Department Manager', 'Quản lý bộ phận'),
              (3, 'staff', 'Staff', 'Nhân sự nghiệp vụ')
            ON CONFLICT (id) DO UPDATE SET code = EXCLUDED.code, name = EXCLUDED.name;
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO users (id, username, email, password_hash, full_name, role_label, department_id, is_active) OVERRIDING SYSTEM VALUE VALUES
              (1, 'admin', 'admin@etms.local', {adminHash}, 'Truong Tran', 'System Admin', 1, TRUE),
              (2, 'chau.hr', 'chau.hr@etms.local', {userHash}, 'Nguyen Minh Chau', 'HR Executive', 101, TRUE),
              (3, 'linh.mk', 'linh.mk@etms.local', {userHash}, 'Do Khanh Linh', 'Marketing Executive', 104, TRUE),
              (4, 'long.it', 'long.it@etms.local', {userHash}, 'Pham Duc Long', 'IT Support Lead', 102, TRUE),
              (5, 'ha.finance', 'ha.finance@etms.local', {userHash}, 'Tran Thu Ha', 'Finance Lead', 103, TRUE),
              (6, 'phuc.mk', 'phuc.mk@etms.local', {userHash}, 'Le Hoang Phuc', 'Marketing Lead', 104, TRUE),
              (7, 'dang.legal', 'dang.legal@etms.local', {userHash}, 'Nguyen Hai Dang', 'Legal Specialist', 105, TRUE),
              (8, 'kiet.finance', 'kiet.finance@etms.local', {userHash}, 'Pham Tuan Kiet', 'Finance Executive', 103, TRUE),
              (9, 'tran.dev', 'tran.dev@etms.local', {userHash}, 'Truong Tran', 'Developer', 102, TRUE)
            ON CONFLICT (id) DO UPDATE SET
              email = EXCLUDED.email,
              password_hash = EXCLUDED.password_hash,
              full_name = EXCLUDED.full_name,
              role_label = EXCLUDED.role_label,
              department_id = EXCLUDED.department_id,
              is_active = EXCLUDED.is_active;
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO user_roles (user_id, role_id) VALUES
              (1, 1), (2, 3), (3, 3), (4, 2), (5, 2), (6, 2), (7, 3), (8, 3), (9, 3)
            ON CONFLICT DO NOTHING;

            INSERT INTO projects (id, code, name, description, department_id, owner_id, start_date, end_date, status, created_by)
            OVERRIDING SYSTEM VALUE VALUES
              (1, 'DA-001', 'Kế hoạch vận hành tháng 5', 'Điều phối các đầu việc vận hành và báo cáo nội bộ.', 1, 1, CURRENT_DATE - 7, CURRENT_DATE + 14, 'active', 1),
              (2, 'DA-002', 'Onboarding nhân sự mới', 'Chuẩn hóa quy trình tiếp nhận nhân sự và tài khoản.', 101, 2, CURRENT_DATE - 3, CURRENT_DATE + 21, 'active', 1),
              (3, 'DA-003', 'Sẵn sàng hạ tầng nội bộ', 'Theo dõi tài khoản, thiết bị, backup và hỗ trợ CNTT.', 102, 4, CURRENT_DATE - 5, CURRENT_DATE + 10, 'active', 1)
            ON CONFLICT (id) DO UPDATE SET
              name = EXCLUDED.name,
              description = EXCLUDED.description,
              status = EXCLUDED.status,
              updated_at = now();

            INSERT INTO tasks (id, code, project_id, title, description, task_type, department_id, status_id, priority_id,
                               reporter_id, assignee_id, start_date, due_date, progress, source, estimated_hours, actual_hours)
            OVERRIDING SYSTEM VALUE VALUES
              (1, 'CV-0001', 1, 'Rà soát kế hoạch vận hành tuần', 'Tổng hợp đầu việc trọng tâm và cập nhật tiến độ.', 'operations', 1, 3, 2, 1, 2, CURRENT_DATE - 2, CURRENT_DATE + 5, 35, 'manual', 6, 2),
              (2, 'CV-0002', 2, 'Chuẩn bị tài khoản nhân sự mới', 'Phối hợp IT tạo tài khoản, email và quyền truy cập.', 'support', 101, 2, 3, 2, 4, CURRENT_DATE, CURRENT_DATE + 4, 10, 'project', 4, 0),
              (3, 'CV-0003', 3, 'Kiểm tra backup hệ thống', 'Rà soát lịch backup và xác nhận log phục hồi.', 'support', 102, 4, 4, 1, 4, CURRENT_DATE - 4, CURRENT_DATE + 2, 60, 'manual', 8, 5)
            ON CONFLICT (id) DO UPDATE SET
              title = EXCLUDED.title,
              description = EXCLUDED.description,
              status_id = EXCLUDED.status_id,
              priority_id = EXCLUDED.priority_id,
              progress = EXCLUDED.progress,
              updated_at = now();

            INSERT INTO subtasks (id, task_id, title, assignee_id, due_date, progress, done, sort_order)
            OVERRIDING SYSTEM VALUE VALUES
              (1, 1, 'Tổng hợp task mở', 2, CURRENT_DATE + 2, 100, TRUE, 1),
              (2, 1, 'Cập nhật dashboard tiến độ', 2, CURRENT_DATE + 4, 30, FALSE, 2),
              (3, 2, 'Tạo email công ty', 4, CURRENT_DATE + 1, 0, FALSE, 1)
            ON CONFLICT (id) DO UPDATE SET
              title = EXCLUDED.title,
              progress = EXCLUDED.progress,
              done = EXCLUDED.done,
              updated_at = now();

            INSERT INTO task_comments (task_id, user_id, content) VALUES
              (1, 1, 'Seed: task dùng để kiểm thử luồng dashboard và task board.'),
              (2, 2, 'Seed: cần IT phối hợp trong ngày.')
            ON CONFLICT DO NOTHING;
            """,
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO inter_department_requests
              (id, code, type, title, description, requester_department_id, requester_user_id,
               target_department_id, owner_id, priority, status, due_date, sla_policy_key,
               sla_started_at, sla_due_at, form_values, latest_message, note)
            VALUES
              ('00000000-0000-0000-0000-000000000001', 'IR-001', 'it-support', 'Cấp quyền VPN cho nhân sự mới',
               'Yêu cầu IT cấp quyền VPN và kiểm tra truy cập nội bộ.', 101, 2, 102, 4, 'High', 'processing',
               CURRENT_DATE + 3, 'it-support', now(), now() + interval '24 hours',
               '{{"employee":"Nguyen Van A","system":"VPN"}}'::jsonb,
               'IT đã tiếp nhận và đang xử lý.', 'Phiếu seed để kiểm thử yêu cầu liên phòng ban.'),
              ('00000000-0000-0000-0000-000000000002', 'IR-002', 'payment', 'Thanh toán chi phí truyền thông',
               'Đề nghị phòng tài chính kiểm tra và thanh toán chi phí chiến dịch.', 104, 3, 103, 5, 'Medium', 'new',
               CURRENT_DATE + 5, 'payment', now(), now() + interval '72 hours',
               '{{"amount":"15000000","vendor":"Agency A"}}'::jsonb,
               'Phiếu mới chờ tiếp nhận.', 'Chứng từ sẽ bổ sung trong ngày.')
            ON CONFLICT (id) DO UPDATE SET
              title = EXCLUDED.title,
              status = EXCLUDED.status,
              latest_message = EXCLUDED.latest_message,
              updated_at = now();

            INSERT INTO inter_request_messages
              (id, request_id, author_user_id, author_name, author_role, author_department, body)
            VALUES
              ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 2, 'Nguyen Minh Chau', 'requester', 'Hành chính - Nhân sự', 'Nhờ IT hỗ trợ cấp quyền VPN cho nhân sự mới.'),
              ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 4, 'Pham Duc Long', 'processor', 'IT nội bộ', 'IT đã tiếp nhận và đang xử lý.')
            ON CONFLICT (id) DO NOTHING;
            """,
            cancellationToken);
    }
}
