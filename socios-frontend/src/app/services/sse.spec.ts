import { parseSseEvents } from './sse';

describe('parseSseEvents', () => {
  it('separa eventos completos y deja el resto para el próximo pedazo', () => {
    const { events, rest } = parseSseEvents(
      'event: delta\ndata: {"text":"Hola"}\n\nevent: delta\ndata: {"text":" mun'
    );

    expect(events).toEqual([{ event: 'delta', data: '{"text":"Hola"}' }]);
    expect(rest).toBe('event: delta\ndata: {"text":" mun');
  });

  it('arma el evento cuando el resto se completa', () => {
    const first = parseSseEvents('event: delta\ndata: {"text":"a"}');
    const second = parseSseEvents(first.rest + '\n\nevent: done\ndata: {}\n\n');

    expect(first.events).toEqual([]);
    expect(second.events).toEqual([
      { event: 'delta', data: '{"text":"a"}' },
      { event: 'done', data: '{}' }
    ]);
    expect(second.rest).toBe('');
  });

  it('acepta saltos de línea \\r\\n', () => {
    const { events } = parseSseEvents('event: done\r\ndata: {}\r\n\r\n');
    expect(events).toEqual([{ event: 'done', data: '{}' }]);
  });
});
