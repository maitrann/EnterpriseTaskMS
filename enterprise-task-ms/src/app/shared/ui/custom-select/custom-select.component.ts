import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MultiSelectModule } from 'primeng/multiselect';
import { SelectModule } from 'primeng/select';

export interface CustomSelectOption<T = string | number | null> {
  value: T;
  label: string;
  description?: string;
  groupLabel?: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule, FormsModule, SelectModule, MultiSelectModule],
  templateUrl: './custom-select.component.html',
  styleUrl: './custom-select.component.scss'
})
export class CustomSelectComponent {
  @Input() options: Array<CustomSelectOption> = [];
  @Input() value: string | number | null | Array<string | number> = null;
  @Input() multiple = false;
  @Input() placeholder = 'Chưa chọn';
  @Input() searchPlaceholder = 'Tìm kiếm...';
  @Input() emptyLabel = 'Không có lựa chọn';
  @Input() helperLabel = '';
  @Input() disabled = false;
  @Input() size: 'default' | 'compact' = 'default';

  @Output() valueChange = new EventEmitter<string | number | null | Array<string | number>>();

  get normalizedValue() {
    return this.multiple ? (Array.isArray(this.value) ? this.value : []) : this.value;
  }

  emitValue(value: string | number | null | Array<string | number>) {
    this.valueChange.emit(value);
  }
}
