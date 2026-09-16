import { Component, EventEmitter, Input, Output } from '@angular/core';

// Paginador reutilizable para los listados del panel de administración.
// No carga datos: solo avisa qué página se pidió y el padre se encarga del resto.
@Component({
  selector: 'app-paginator',
  standalone: true,
  templateUrl: './paginator.html',
  styleUrl: './paginator.css'
})
export class PaginatorComponent {
  @Input() page = 1;
  @Input() pageSize = 10;
  @Input() totalCount = 0;
  @Input() disabled = false;

  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get rangeStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  // Primera, última y las vecinas de la actual; null marca un salto ("…")
  get pages(): (number | null)[] {
    const total = this.totalPages;
    const visible = new Set([1, total, this.page - 1, this.page, this.page + 1]);
    const sorted = [...visible].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);

    const result: (number | null)[] = [];
    sorted.forEach((p, i) => {
      if (i > 0 && p - sorted[i - 1] > 1) result.push(null);
      result.push(p);
    });
    return result;
  }

  goTo(page: number): void {
    if (this.disabled || page < 1 || page > this.totalPages || page === this.page) return;
    this.pageChange.emit(page);
  }
}
