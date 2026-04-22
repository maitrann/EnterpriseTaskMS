import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';

import { DepartmentService } from '../../core/services/department.service';

@Component({
  standalone: true,
  selector: 'app-department',
  imports: [CommonModule],
  templateUrl: './department.component.html',
  styleUrls: ['./department.component.scss']
})
export class DepartmentComponent {
  private readonly departmentService = inject(DepartmentService);

  readonly departments = this.departmentService.departmentCards;
  readonly summaryCards = this.departmentService.summaryCards;
}
