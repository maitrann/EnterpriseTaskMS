# Enterprise Task MS Frontend

Angular frontend for the Enterprise Task Management System. The app focuses on internal task operations: dashboard monitoring, Kanban-style task management, task details, subtasks, project progress, departments, and inter-department requests.

## Stack

- Angular
- Angular Router
- HttpClient with JWT interceptor
- Angular signals for local reactive state
- Route guards for authenticated pages
- Mock data sources for UI development without the backend

Note: NgRx packages are present and a small reducer exists, but the main application state currently uses Angular services and signals. Do not emphasize NgRx in portfolio descriptions unless the store/effects are expanded later.

## Demo Flow

1. Sign in with the demo account shown on the login screen.
2. Review dashboard cards for workload, deadline warnings, and task distribution.
3. Open the task board and filter by workflow status groups.
4. Open a task detail drawer to update assignee, add feedback, manage subtasks, request extensions, or complete/cancel a task.
5. Open Projects to see task progress grouped by project.
6. Open Departments and Inter-department Requests to review operational views.

## Demo Account

- Email: `admin@etms.local`
- Password: `Admin@123`

Additional mock users use `Mock@123`.

## Backend Integration

The frontend calls the API base URL from:

```ts
src/app/core/constants/app.constants.ts
```

JWT tokens are stored locally and attached to API requests through `src/app/core/interceptors/auth.interceptor.ts`.

When the backend is unavailable, selected services keep mock data visible so UI review remains possible.

## Run

```powershell
npm install
npm start
```

Open `http://localhost:4200`.

## Build

```powershell
npm run build
```

## Screenshots

Screenshots are stored in the root `docs/screenshots/` folder.

Current screenshot:

- `docs/screenshots/login.png`

Recommended screenshots before publishing the portfolio repository:

- `docs/screenshots/dashboard.png`
- `docs/screenshots/task-board.png`
- `docs/screenshots/task-detail.png`
- `docs/screenshots/projects.png`
- `docs/screenshots/inter-department-requests.png`
