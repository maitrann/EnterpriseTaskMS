import { CommonModule } from '@angular/common';
import { DOCUMENT } from '@angular/common';
import { Component, DestroyRef, ElementRef, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface CustomSelectOption<T = string | number | null> {
  value: T;
  label: string;
  description?: string;
  groupLabel?: string;
}

type CustomSelectGroup = {
  label: string;
  options: CustomSelectOption[];
};

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './custom-select.component.html',
  styleUrl: './custom-select.component.scss'
})
export class CustomSelectComponent {
  private readonly documentRef = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly elementRef = inject(ElementRef<HTMLElement>);

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

  readonly isOpen = signal(false);
  readonly searchTerm = signal('');

  constructor() {
    const handleDocumentClick = (event: Event) => {
      const target = event.target as Node | null;

      if (!target || !this.elementRef.nativeElement.contains(target)) {
        this.closePanel();
      }
    };

    this.documentRef.addEventListener('click', handleDocumentClick, true);
    this.destroyRef.onDestroy(() => {
      this.documentRef.removeEventListener('click', handleDocumentClick, true);
    });
  }

  toggleOpen() {
    if (this.disabled) {
      return;
    }

    this.isOpen.update((value) => {
      this.searchTerm.set('');
      return !value;
    });
  }

  selectOption(option: CustomSelectOption) {
    if (this.disabled) {
      return;
    }

    if (!this.multiple) {
      this.valueChange.emit(option.value as string | number | null);
      this.isOpen.set(false);
      return;
    }

    const current = Array.isArray(this.value) ? this.value : [];
    const normalizedValue = option.value as string | number;
    const next = current.includes(normalizedValue)
      ? current.filter((item) => item !== normalizedValue)
      : [...current, normalizedValue];

    this.valueChange.emit(next);
  }

  isSelected(option: CustomSelectOption) {
    if (this.multiple) {
      const current = Array.isArray(this.value) ? this.value : [];
      return current.includes(option.value as string | number);
    }

    return option.value === this.value;
  }

  closePanel() {
    this.isOpen.set(false);
  }

  getFilteredOptions() {
    const keyword = this.searchTerm().trim().toLowerCase();

    if (!keyword) {
      return this.options;
    }

    return this.options.filter(
      (option) =>
        option.label.toLowerCase().includes(keyword) ||
        (option.description ?? '').toLowerCase().includes(keyword)
    );
  }

  getSelectedOptions() {
    if (this.multiple) {
      const selectedValues = Array.isArray(this.value) ? this.value : [];
      return this.options.filter((option) => selectedValues.includes(option.value as string | number));
    }

    return this.options.filter((option) => option.value === this.value);
  }

  getDisplayValue() {
    const selected = this.getSelectedOptions();

    if (!selected.length) {
      return this.placeholder;
    }

    if (!this.multiple || selected.length === 1) {
      return selected[0].label;
    }

    return `${selected[0].label} +${selected.length - 1}`;
  }

  getGroupedOptions(): CustomSelectGroup[] {
    const filtered = this.getFilteredOptions();
    const groups = new Map<string, CustomSelectOption[]>();

    for (const option of filtered) {
      const groupLabel = option.groupLabel ?? '';
      const existing = groups.get(groupLabel) ?? [];
      existing.push(option);
      groups.set(groupLabel, existing);
    }

    return Array.from(groups.entries()).map(([label, options]) => ({ label, options }));
  }
}
