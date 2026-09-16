import { PaginatorComponent } from './paginator';

describe('PaginatorComponent', () => {
  function create(page: number, totalCount: number, pageSize = 10): PaginatorComponent {
    const paginator = new PaginatorComponent();
    paginator.page = page;
    paginator.totalCount = totalCount;
    paginator.pageSize = pageSize;
    return paginator;
  }

  it('calcula el total de páginas y el rango mostrado', () => {
    const paginator = create(2, 23);
    expect(paginator.totalPages).toBe(3);
    expect(paginator.rangeStart).toBe(11);
    expect(paginator.rangeEnd).toBe(20);
  });

  it('muestra 0–0 cuando no hay registros', () => {
    const paginator = create(1, 0);
    expect(paginator.totalPages).toBe(1);
    expect(paginator.rangeStart).toBe(0);
    expect(paginator.rangeEnd).toBe(0);
  });

  it('muestra todas las páginas cuando son pocas', () => {
    expect(create(2, 30).pages).toEqual([1, 2, 3]);
  });

  it('agrega saltos alrededor de la página actual cuando son muchas', () => {
    expect(create(5, 100).pages).toEqual([1, null, 4, 5, 6, null, 10]);
    expect(create(1, 100).pages).toEqual([1, 2, null, 10]);
    expect(create(10, 100).pages).toEqual([1, null, 9, 10]);
  });

  it('solo emite páginas válidas y distintas de la actual', () => {
    const paginator = create(1, 25);
    const emitted: number[] = [];
    paginator.pageChange.subscribe((p) => emitted.push(p));

    paginator.goTo(0);
    paginator.goTo(1);
    paginator.goTo(4);
    paginator.goTo(2);

    paginator.disabled = true;
    paginator.goTo(3);

    expect(emitted).toEqual([2]);
  });
});
