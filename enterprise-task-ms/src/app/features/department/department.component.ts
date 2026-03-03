import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-department',
  imports: [CommonModule],
  templateUrl: './department.component.html',
  styleUrls: ['./department.component.scss']
})
export class DepartmentComponent {

  departments = [
    {
      name: 'Engineering',
      description: 'Product development and system architecture',
      members: 24
    },
    {
      name: 'Marketing',
      description: 'Brand, growth and customer acquisition',
      members: 12
    },
    {
      name: 'Human Resources',
      description: 'People operations and recruitment',
      members: 6
    },
    {
      name: 'Finance',
      description: 'Financial planning and analysis',
      members: 8
    }
  ];
}